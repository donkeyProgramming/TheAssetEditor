using System.Text;
using Microsoft.Xna.Framework;
using Shared.GameFormats.RigidModel;
using Shared.GameFormats.RigidModel.MaterialHeaders;
using Test.TestingUtility.Shared;

namespace GameWorld.Core.Test.GameFormats;

public class RigidModelShortHeaderTests
{
    [Test]
    public void Warhammer3Decal_LoadsWithItsPathAndCube_AndSavesIdentically()
    {
        var bytes = RmvTestFiles.BuildWarhammer3Decal("rigidmodels/decals/chd/chd_coal_decal_01");

        var file = ModelFactory.Create().Load(bytes);

        var model = file.ModelList[0][0];
        var material = model.Material as DecalMaterial;
        Assert.That(material, Is.Not.Null);
        Assert.That(material!.TexturePath.Value, Is.EqualTo("rigidmodels/decals/chd/chd_coal_decal_01"));
        Assert.That(material.Values, Is.Empty);
        Assert.That(model.CommonHeader.VertexCount, Is.EqualTo(24));
        Assert.That(model.Mesh.VertexList, Is.Empty);
        Assert.That(model.Mesh.IndexList, Is.EqualTo(RmvTestFiles.CubeIndices));
        Assert.That(ModelFactory.Create().Save(file), Is.EqualTo(bytes));
    }

    [Test]
    public void Decal_KeepsItsFloats_AndTheStaleBytesAfterItsPath()
    {
        var header = new byte[256 + 9 * 4];
        Encoding.ASCII.GetBytes("rigidmodels/buildings/textures/rubble_1").CopyTo(header, 0);
        header[200] = 0x7F;                 // left over after the terminator by CA's exporter
        BitConverter.GetBytes(1.5f).CopyTo(header, 256);
        BitConverter.GetBytes(-2.0f).CopyTo(header, 256 + 8 * 4);

        Assert.That(ShortHeaderMaterials.TryCreate(ModelMaterialEnum.projected_decal_v2, header, 0, header.Length, out var material), Is.True);

        var decal = (DecalMaterial)material;
        Assert.That(decal.TexturePath.Value, Is.EqualTo("rigidmodels/buildings/textures/rubble_1"));
        Assert.That(decal.Values, Has.Length.EqualTo(9));
        Assert.That((decal.Values[0], decal.Values[8]), Is.EqualTo((1.5f, -2.0f)));
        Assert.That(decal.Save(), Is.EqualTo(header));
    }

    [Test]
    public void Decal_WithAnEditedPath_IsWrittenZeroPadded()
    {
        var header = new byte[256];
        Encoding.ASCII.GetBytes("old_path").CopyTo(header, 0);
        header[200] = 0x7F;
        ShortHeaderMaterials.TryCreate(ModelMaterialEnum.alpha_blend, header, 0, header.Length, out var material);

        var decal = (DecalMaterial)material;
        decal.TexturePath.Value = "new_path";
        var saved = decal.Save();

        Assert.That(Encoding.ASCII.GetString(saved, 0, 8), Is.EqualTo("new_path"));
        Assert.That(saved.Skip(8), Is.All.EqualTo(0));
    }

    [TestCase(ModelMaterialEnum.TerrainTiles, 6)]
    [TestCase((ModelMaterialEnum)66, 5)]
    [TestCase(ModelMaterialEnum.weighted_texture_blend, 6)]
    [TestCase(ModelMaterialEnum.projected_decal_v4, 6)]
    public void TerrainTile_IsPickedByIdAndSize(ModelMaterialEnum materialId, int wordCount)
    {
        var header = new byte[64 + wordCount * 4];
        Encoding.ASCII.GetBytes("TerrainBase0").CopyTo(header, 0);
        BitConverter.GetBytes(42u).CopyTo(header, 64);

        Assert.That(ShortHeaderMaterials.TryCreate(materialId, header, 0, header.Length, out var material), Is.True);

        var tile = (TerrainTileMaterial)material;
        Assert.That(tile.ModelName, Is.EqualTo("TerrainBase0"));
        Assert.That(tile.Values[0], Is.EqualTo(42u));
        Assert.That(tile.Save(), Is.EqualTo(header));
    }

    [Test]
    public void BannerMaterial_KeepsItsNameAndWords()
    {
        var header = new byte[256 + 8 * 4];
        Encoding.ASCII.GetBytes("army_banner_strength_fill_lod1").CopyTo(header, 0);
        BitConverter.GetBytes(7u).CopyTo(header, 256 + 7 * 4);

        Assert.That(ShortHeaderMaterials.TryCreate(ModelMaterialEnum.texture_combo, header, 0, header.Length, out var material), Is.True);

        Assert.That(material.ModelName, Is.EqualTo("army_banner_strength_fill_lod1"));
        Assert.That(((BannerMaterial)material).Values[7], Is.EqualTo(7u));
        Assert.That(material.Save(), Is.EqualTo(header));
    }

    [Test]
    public void StaticPointLight_ReadsItsPositionAsThePivot()
    {
        var header = new byte[StaticPointLightMaterial.Size];
        Encoding.ASCII.GetBytes("orange_10m_glow").CopyTo(header, 0);
        BitConverter.GetBytes(1.0f).CopyTo(header, 512);
        BitConverter.GetBytes(2.0f).CopyTo(header, 516);
        BitConverter.GetBytes(3.0f).CopyTo(header, 520);

        Assert.That(ShortHeaderMaterials.TryCreate(ModelMaterialEnum.static_point_light, header, 0, header.Length, out var material), Is.True);

        Assert.That(material.ModelName, Is.EqualTo("orange_10m_glow"));
        Assert.That(material.PivotPoint, Is.EqualTo(new Vector3(1, 2, 3)));
        Assert.That(material.Save(), Is.EqualTo(header));
    }

    [Test]
    public void SectionWithNoMaterialHeader_IsANoHeaderMaterial()
    {
        Assert.That(ShortHeaderMaterials.TryCreate(ModelMaterialEnum.bow_wave, [], 0, 0, out var material), Is.True);
        Assert.That(material, Is.TypeOf<NoHeaderMaterial>());
        Assert.That(material.Save(), Is.Empty);
    }

    // These ids also carry full weighted headers (Warhammer's id 97 on wind-swayed props), which
    // must keep going to the weighted layout.
    [TestCase(ModelMaterialEnum.projected_decal_v4)]
    [TestCase(ModelMaterialEnum.alpha_blend)]
    [TestCase(ModelMaterialEnum.texture_combo)]
    public void IdWithAWeightedSizedHeader_IsNotAShortHeader(ModelMaterialEnum materialId)
    {
        Assert.That(ShortHeaderMaterials.TryCreate(materialId, new byte[1000], 0, 1000, out _), Is.False);
    }
}
