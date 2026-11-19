using Microsoft.Xna.Framework;
using Shared.GameFormats.RigidModel;
using Shared.GameFormats.RigidModel.MaterialHeaders;
using Shared.GameFormats.RigidModel.Vertex;
using Test.TestingUtility.TestUtility;

namespace GameWorld.Core.Test.GameFormats;

public class RigidModelFormatTests
{
    static readonly string s_dataFolder = PathHelper.GetDataFolder("Data");

    static IEnumerable<string> ModelsOfVersion(params uint[] versions) => Directory
        .GetFiles(s_dataFolder, "*.rigid_model_v2", SearchOption.AllDirectories)
        .Where(path => versions.Contains(BitConverter.ToUInt32(File.ReadAllBytes(path), 4)))
        .Select(path => Path.GetRelativePath(s_dataFolder, path))
        .OrderBy(path => path);

    public static IEnumerable<string> PreV8Models => ModelsOfVersion(6, 7);
    public static IEnumerable<string> V8Models => ModelsOfVersion(8);

    // Before v8, CA writes positions as plain halves, so every field of a vanilla file can be
    // reproduced exactly: static vertex colour, the material's alignment bytes and position W.
    [TestCaseSource(nameof(PreV8Models))]
    public void PreV8Model_LoadAndSave_IsByteIdentical(string relativePath)
    {
        var original = File.ReadAllBytes(Path.Combine(s_dataFolder, relativePath));

        var saved = ModelFactory.Create().Save(ModelFactory.Create().Load(original));

        Assert.That(saved, Is.EqualTo(original));
    }

    // v8 positions use the W-scale precision trick, where more than one W can give the same
    // position, so the bytes may differ from CA's. Everything decoded from them must not.
    [TestCaseSource(nameof(V8Models))]
    public void V8Model_LoadAndSave_KeepsEveryVertex(string relativePath)
    {
        var original = ModelFactory.Create().Load(File.ReadAllBytes(Path.Combine(s_dataFolder, relativePath)));

        var reloaded = ModelFactory.Create().Load(ModelFactory.Create().Save(original));

        for (var lodIndex = 0; lodIndex < original.ModelList.Length; lodIndex++)
        {
            for (var modelIndex = 0; modelIndex < original.ModelList[lodIndex].Length; modelIndex++)
            {
                var expected = original.ModelList[lodIndex][modelIndex].Mesh.VertexList;
                var actual = reloaded.ModelList[lodIndex][modelIndex].Mesh.VertexList;
                Assert.That(actual.Select(x => x.Position), Is.EqualTo(expected.Select(x => x.Position)), $"Lod {lodIndex} mesh {modelIndex} positions");
                Assert.That(actual.Select(x => x.Colour), Is.EqualTo(expected.Select(x => x.Colour)), $"Lod {lodIndex} mesh {modelIndex} colours");
                Assert.That(actual.Select(x => x.Uv), Is.EqualTo(expected.Select(x => x.Uv)), $"Lod {lodIndex} mesh {modelIndex} uvs");
            }
        }
    }

    [TestCase(RmvVersionEnum.RMV2_V6, false, 1)]
    [TestCase(RmvVersionEnum.RMV2_V7, false, 1)]
    [TestCase(RmvVersionEnum.RMV2_V6, true, 0)]
    [TestCase(RmvVersionEnum.RMV2_V7, true, 0)]
    public void PreV8Position_IsWrittenWithCasW(RmvVersionEnum version, bool isSkinned, float expectedW)
    {
        var position = new Vector4(0.123f, 1.7f, -0.42f, 1);

        var encoded = VertexLoadHelper.CreatePositionForVersion(position, version, isSkinned);

        Assert.That((float)encoded.W, Is.EqualTo(expectedW));
        Assert.That((float)encoded.Y, Is.EqualTo((float)new SharpDX.Half(position.Y)));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void V8Position_UsesTheWScaleTrick(bool isSkinned)
    {
        var encoded = VertexLoadHelper.CreatePositionForVersion(new Vector4(0.123f, 1.7f, -0.42f, 1), RmvVersionEnum.RMV2_V8, isSkinned);

        Assert.That((float)encoded.W, Is.InRange(1.0f, 2.0f));
    }

    [Test]
    public void VertexColour_IsUnormBytes()
    {
        var colour = VertexLoadHelper.CreateColourFromBytes(255, 0, 128, 191);
        var bytes = VertexLoadHelper.CreateColourBytes(colour);

        Assert.That(colour, Is.EqualTo(new Vector4(1, 0, 128 / 255.0f, 191 / 255.0f)));
        Assert.That((bytes.X, bytes.Y, bytes.Z, bytes.W), Is.EqualTo(((byte)255, (byte)0, (byte)128, (byte)191)));
    }

    [TestCase(ModelMaterialEnum.cloth)]
    [TestCase(ModelMaterialEnum.weighted_cloth)]
    [TestCase(ModelMaterialEnum.collision_shape)]
    [TestCase(ModelMaterialEnum.rope)]
    public void PhysicsMaterial_KeepsItsTrailingBlock(ModelMaterialEnum materialId)
    {
        var factory = MaterialFactory.Create();
        var material = (WeightedMaterial)factory.CreateMaterial(materialId);
        material.BinaryVertexFormat = VertexFormat.Static;
        material.PhysicsBlock = [1, 2, 3, 4, 5, 6, 7, 8, 9];
        material.AlignmentPadding = [0xAB, 0xCD];
        var bytes = factory.Save(materialId, material);

        var loaded = (WeightedMaterial)factory.LoadMaterial(bytes, 0, RmvVersionEnum.RMV2_V8, materialId, bytes.Length);

        Assert.That(loaded.PhysicsBlock, Is.EqualTo(material.PhysicsBlock));
        Assert.That(loaded.AlignmentPadding, Is.EqualTo(material.AlignmentPadding));
        Assert.That(factory.Save(materialId, loaded), Is.EqualTo(bytes));
    }

    [Test]
    public void MaterialWithNoCreatorOfItsOwn_SavesLikeItLoads()
    {
        var factory = MaterialFactory.Create();
        var material = (WeightedMaterial)factory.CreateMaterial(ModelMaterialEnum.default_type);
        material.BinaryVertexFormat = VertexFormat.Static;
        var bytes = factory.Save(ModelMaterialEnum.default_type, material);

        var loaded = factory.LoadMaterial(bytes, 0, RmvVersionEnum.RMV2_V8, ModelMaterialEnum.ship_ambientmap, bytes.Length);

        Assert.That(factory.Save(ModelMaterialEnum.ship_ambientmap, loaded), Is.EqualTo(bytes));
    }

    [Test]
    public void ParamListClone_CopiesTheValues()
    {
        var list = new ParamList<int>();
        list.Set(WeightedParamterIds.IntParams_Alpha_index, 1);
        list.Set(WeightedParamterIds.IntParams_Dirt_index, 2);

        var clone = list.Clone();
        clone.Set(WeightedParamterIds.IntParams_Alpha_index, 5);

        Assert.That(clone.Values, Is.EqualTo(new[] { (0, 5), (2, 2) }));
        Assert.That(list.Get(WeightedParamterIds.IntParams_Alpha_index), Is.EqualTo(1));
    }
}
