using System.Drawing;
using System.Drawing.Imaging;
using System.Numerics;
using Editors.ImportExport.Exporting.Exporters.RmvToGltf;
using Editors.ImportExport.Exporting.Exporters.RmvToGltf.Helpers;
using Editors.ImportExport.Importing;
using Editors.ImportExport.Importing.Importers.GltfToRmv;
using Editors.ImportExport.Importing.Importers.GltfToRmv.Helper;
using GameWorld.Core.Services;
using Moq;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.PackFiles.Models.FileSources;
using Shared.Core.PackFiles.Models.Containers;
using Shared.Core.Services;
using Shared.Core.Settings;
using Shared.GameFormats.RigidModel;
using Shared.GameFormats.RigidModel.MaterialHeaders;
using Shared.TestUtility;
using SharpGLTF.Geometry;
using SharpGLTF.Geometry.VertexTypes;
using SharpGLTF.Materials;
using SharpGLTF.Schema2;

namespace Test.ImportExport.Importing.Importers.GltfImporterTest;

public class GltfMaterialModeImportTests
{
    [Test]
    public void Import_OpaqueMaterial_WritesOpaqueWh3AlphaParameter()
    {
        var fixtureDirectory = CreateFixtureDirectory();
        try
        {
            var glbPath = CreateModel(
                fixtureDirectory,
                "opaque.glb",
                [new MaterialBuilder("opaque_material").WithMetallicRoughness()]);
            var destination = PackFileContainer.CreatePackFile("test");

            var result = CreateImporter().Import(CreateSettings(glbPath, destination));

            Assert.That(result.Succeeded, Is.True, GetFailure(result));
            var material = GetImportedMaterial(destination, "opaque");
            Assert.That(material.IntParams.TryGet(
                WeightedParamterIds.IntParams_Alpha_index,
                out var alphaMode), Is.True);
            Assert.That(alphaMode, Is.Zero);
        }
        finally
        {
            Directory.Delete(fixtureDirectory, recursive: true);
        }
    }

    [Test]
    public void Import_MaskMaterial_UsesGltfCutoffAndMatchesRealWh3AlphaContract()
    {
        var fixtureDirectory = CreateFixtureDirectory();
        try
        {
            var baseColorPath = CreateAlphaPng(fixtureDirectory, "hair.png");
            var materialBuilder = new MaterialBuilder("hair_mask")
                .WithMetallicRoughness()
                .WithAlpha(SharpGLTF.Materials.AlphaMode.MASK, 0.7f)
                .WithChannelImage(KnownChannel.BaseColor, baseColorPath);
            var glbPath = CreateModel(
                fixtureDirectory,
                "mask.glb",
                [materialBuilder]);
            var destination = PackFileContainer.CreatePackFile("test");

            var result = CreateImporter().Import(CreateSettings(glbPath, destination));

            Assert.That(result.Succeeded, Is.True, GetFailure(result));
            var importedMaterial = GetImportedMaterial(destination, "mask");
            Assert.That(importedMaterial.IntParams.TryGet(
                WeightedParamterIds.IntParams_Alpha_index,
                out var importedAlphaMode), Is.True);
            var realWh3Material = GetRealWh3CutoutMaterial();
            Assert.That(realWh3Material.IntParams.Get(
                WeightedParamterIds.IntParams_Alpha_index), Is.EqualTo(1));
            Assert.That(importedAlphaMode, Is.EqualTo(realWh3Material.IntParams.Get(
                WeightedParamterIds.IntParams_Alpha_index)));

            using var bitmap = DecodeDds(
                destination.FileList[@"models\tex\mesh_node_base_colour.dds"]);
            Assert.Multiple(() =>
            {
                Assert.That(bitmap.GetPixel(0, 0).A, Is.Zero);
                Assert.That(bitmap.GetPixel(3, 0).A, Is.EqualTo(255));
                Assert.That(result.MaterialSummary?.MaskedMaterials,
                    Is.EqualTo(new[] { new MaskedMaterialImportSummary("hair_mask", 0.7f) }));
            });
        }
        finally
        {
            Directory.Delete(fixtureDirectory, recursive: true);
        }
    }

    [Test]
    public void ExportThenImport_RealWh3Materials_PreservesOpaqueAndMaskModes()
    {
        var fixtureDirectory = CreateFixtureDirectory();
        try
        {
            var glbPath = Path.Combine(fixtureDirectory, "material-roundtrip.glb");
            var original = GetRealWh3Model();
            var exportSettings = new RmvToGltfExporterSettings(
                new PackFile("source.rigid_model_v2", new MemorySource([])),
                [],
                glbPath,
                false,
                false,
                false,
                false,
                true);
            var modelRoot = ModelRoot.CreateModel();
            var scene = modelRoot.UseScene("default");
            // The exporter reads each part's alpha flag through the model resolver, as RmvToGltfExporter does.
            var resolvedModel = new ModelAssetResolver().Resolve(new PackFile(
                "source.rigid_model_v2",
                new MemorySource(ModelFactory.Create().Save(original))));
            var meshes = new GltfMeshBuilder().Build(
                resolvedModel,
                [],
                exportSettings,
                willHaveSkeleton: false);
            for (var index = 0; index < meshes.Count; index++)
            {
                scene.CreateNode($"mesh_{index}")
                    .WithMesh(modelRoot.CreateMesh(meshes[index]));
            }
            modelRoot.SaveGLB(glbPath);
            var destination = PackFileContainer.CreatePackFile("test");

            var result = CreateImporter().Import(CreateSettings(glbPath, destination));

            Assert.That(result.Succeeded, Is.True, GetFailure(result));
            var imported = ModelFactory.Create().Load(
                destination.FileList[@"models\material-roundtrip.rigid_model_v2"]
                    .DataSource.ReadData());
            var originalAlpha = original.ModelList[0]
                .Select(model => IsAlphaEnabled(model.Material))
                .ToArray();
            var importedAlpha = imported.ModelList[0]
                .Select(model => IsAlphaEnabled(model.Material))
                .ToArray();
            Assert.Multiple(() =>
            {
                Assert.That(originalAlpha, Is.EqualTo(new[] { false, true, false, true }));
                Assert.That(importedAlpha, Is.EqualTo(originalAlpha));
            });
        }
        finally
        {
            Directory.Delete(fixtureDirectory, recursive: true);
        }
    }

    [Test]
    public void Import_BlendMaterials_SkipsAllMaterialsAndKeepsMeshImport()
    {
        var fixtureDirectory = CreateFixtureDirectory();
        try
        {
            var glbPath = CreateModel(
                fixtureDirectory,
                "blend.glb",
                [
                    new MaterialBuilder("eye_glass")
                        .WithMetallicRoughness()
                        .WithAlpha(SharpGLTF.Materials.AlphaMode.BLEND),
                    new MaterialBuilder("aura_film")
                        .WithMetallicRoughness()
                        .WithAlpha(SharpGLTF.Materials.AlphaMode.BLEND),
                ]);
            var destination = PackFileContainer.CreatePackFile("test");
            var existingBytes = new byte[] { 1, 2, 3 };
            destination.FileList.Add(
                @"existing\keep.bin",
                new PackFile("keep.bin", new MemorySource(existingBytes)));

            var result = CreateImporter().Import(CreateSettings(glbPath, destination));

            var rmvPath = @"models\blend.rigid_model_v2";
            var imported = result.Succeeded
                ? ModelFactory.Create().Load(
                    destination.FileList[rmvPath].DataSource.ReadData())
                : null;
            Assert.Multiple(() =>
            {
                Assert.That(result.Succeeded, Is.True, GetFailure(result));
                Assert.That(result.Errors, Is.Empty);
                Assert.That(result.Warnings, Has.Some.Contains("eye_glass"));
                Assert.That(result.Warnings, Has.Some.Contains("aura_film"));
                Assert.That(result.Warnings, Has.Some.Contains("BLEND"));
                Assert.That(result.OutputPaths, Does.Contain(rmvPath));
                Assert.That(destination.FileList.Keys,
                    Has.None.EndsWith(".dds").IgnoreCase);
                Assert.That(imported?.ModelList[0]
                    .SelectMany(model => model.Material.GetAllTextures()),
                    Is.Empty);
                Assert.That(destination.FileList[@"existing\keep.bin"].DataSource.ReadData(),
                    Is.EqualTo(existingBytes));
            });
        }
        finally
        {
            Directory.Delete(fixtureDirectory, recursive: true);
        }
    }

    [Test]
    public void Import_UnsupportedChannels_ReportsStructuredSkippedSemantics()
    {
        var fixtureDirectory = CreateFixtureDirectory();
        try
        {
            var texturePath = CreatePng(
                fixtureDirectory,
                "packed-source.png",
                Color.CornflowerBlue);
            var materialBuilder = new MaterialBuilder("decorated")
                .WithMetallicRoughness()
                .WithChannelImage(KnownChannel.BaseColor, texturePath)
                .WithChannelImage(KnownChannel.Emissive, texturePath)
                .WithChannelImage(KnownChannel.Occlusion, texturePath);
            var glbPath = CreateModel(
                fixtureDirectory,
                "unsupported.glb",
                [materialBuilder]);
            var destination = PackFileContainer.CreatePackFile("test");

            var result = CreateImporter().Import(CreateSettings(glbPath, destination));

            Assert.That(result.Succeeded, Is.True, GetFailure(result));
            Assert.Multiple(() =>
            {
                Assert.That(result.MaterialSummary?.SkippedSemantics,
                    Is.EquivalentTo(new[]
                    {
                        new SkippedMaterialSemantic("decorated", "Emissive"),
                        new SkippedMaterialSemantic("decorated", "Occlusion"),
                    }));
                Assert.That(destination.FileList.Keys.Count(path =>
                    path.EndsWith(".dds", StringComparison.OrdinalIgnoreCase)),
                    Is.EqualTo(4));
                Assert.That(destination.FileList.Keys,
                    Has.None.Contains("emissive"));
                Assert.That(destination.FileList.Keys,
                    Has.None.Contains("occlusion"));
            });
        }
        finally
        {
            Directory.Delete(fixtureDirectory, recursive: true);
        }
    }

    private static GltfImporter CreateImporter() => new(
        PackFileSerivceTestHelper.Create(TestData.InputPack),
        Mock.Of<ISkeletonAnimationLookUpHelper>(),
        new RmvMaterialBuilder());

    private static GltfImporterSettings CreateSettings(
        string gltfPath,
        PackFileContainer destination) => new(
        gltfPath,
        "models",
        destination,
        GameTypeEnum.Warhammer3,
        true,
        true,
        true,
        true,
        false,
        20);

    private static string CreateModel(
        string directory,
        string fileName,
        IReadOnlyList<MaterialBuilder> materials)
    {
        var modelRoot = ModelRoot.CreateModel();
        var scene = modelRoot.UseScene("default");
        for (var index = 0; index < materials.Count; index++)
        {
            var geometry = new MeshBuilder<
                VertexPositionNormalTangent,
                VertexTexture1,
                VertexJoints4>($"mesh_{index}");
            geometry.UsePrimitive(materials[index]).AddTriangle(
                CreateVertex(index * 2, 0),
                CreateVertex(index * 2 + 1, 0),
                CreateVertex(index * 2, 1));
            scene.CreateNode(index == 0 ? "mesh_node" : $"mesh_node_{index + 1}")
                .WithMesh(modelRoot.CreateMesh(geometry));
        }

        var path = Path.Combine(directory, fileName);
        modelRoot.Save(path);
        return path;
    }

    private static VertexBuilder<
        VertexPositionNormalTangent,
        VertexTexture1,
        VertexJoints4> CreateVertex(float x, float y)
    {
        var vertex = new VertexBuilder<
            VertexPositionNormalTangent,
            VertexTexture1,
            VertexJoints4>();
        vertex.Geometry.Position = new Vector3(x, y, 0);
        vertex.Geometry.Normal = Vector3.UnitZ;
        vertex.Geometry.Tangent = new Vector4(1, 0, 0, 1);
        vertex.Material.TexCoord = new Vector2(x, y);
        vertex.Skinning.SetBindings((0, 1), (0, 0), (0, 0), (0, 0));
        return vertex;
    }

    private static string CreatePng(
        string directory,
        string fileName,
        Color color)
    {
        var path = Path.Combine(directory, fileName);
        using var bitmap = new Bitmap(4, 4, PixelFormat.Format32bppArgb);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.Clear(color);
        bitmap.Save(path, ImageFormat.Png);
        return path;
    }

    private static string CreateAlphaPng(string directory, string fileName)
    {
        var path = Path.Combine(directory, fileName);
        using var bitmap = new Bitmap(4, 4, PixelFormat.Format32bppArgb);
        for (var y = 0; y < bitmap.Height; y++)
        {
            for (var x = 0; x < bitmap.Width; x++)
            {
                bitmap.SetPixel(
                    x,
                    y,
                    Color.FromArgb(x < 2 ? 150 : 220, 80, 120, 160));
            }
        }

        bitmap.Save(path, ImageFormat.Png);
        return path;
    }

    private static WeightedMaterial GetImportedMaterial(
        PackFileContainer destination,
        string fileName)
    {
        var rmv = ModelFactory.Create().Load(
            destination.FileList[$@"models\{fileName}.rigid_model_v2"].DataSource.ReadData());
        return (WeightedMaterial)rmv.ModelList[0][0].Material;
    }

    private static WeightedMaterial GetRealWh3CutoutMaterial()
        => (WeightedMaterial)GetRealWh3Model().ModelList[0][1].Material;

    private static RmvFile GetRealWh3Model()
    {
        var root = FindRepositoryRoot();
        var path = Path.Combine(
            root,
            "Data",
            "Karl_and_celestialgeneral_Pack",
            "variantmeshes",
            "wh_variantmodels",
            "hu1",
            "emp",
            "emp_karl_franz",
            "emp_karl_franz.rigid_model_v2");
        return ModelFactory.Create().Load(File.ReadAllBytes(path));
    }

    private static bool IsAlphaEnabled(IRmvMaterial material) =>
        material is WeightedMaterial weighted &&
        weighted.IntParams.TryGet(
            WeightedParamterIds.IntParams_Alpha_index,
            out var alphaMode) &&
        alphaMode != 0;

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null &&
               !File.Exists(Path.Combine(directory.FullName, "AssetEditor.sln")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new DirectoryNotFoundException(
            "Can't find the repository root holding AssetEditor.sln.");
    }

    private static Bitmap DecodeDds(PackFile packFile)
    {
        var png = MeshImportExport.TextureHelper.ConvertDdsToPng(
            packFile.DataSource.ReadData());
        using var stream = new MemoryStream(png);
        using var source = new Bitmap(stream);
        return new Bitmap(source);
    }

    private static string CreateFixtureDirectory()
    {
        var path = Path.Combine(
            Path.GetTempPath(),
            $"AssetEditorGltfMaterialModes-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }

    private static string GetFailure(Editors.ImportExport.Importing.ImportResult result) =>
        result.Exception?.ToString() ?? string.Join(Environment.NewLine, result.Errors);
}
