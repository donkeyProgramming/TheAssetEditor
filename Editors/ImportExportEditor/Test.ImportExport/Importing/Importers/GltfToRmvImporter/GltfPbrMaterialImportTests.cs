using System.Drawing;
using System.Drawing.Imaging;
using System.Numerics;
using System.Text.Json;
using DirectXTexNet;
using Editors.ImportExport.Importing.Importers.GltfToRmv;
using Editors.ImportExport.Importing.Importers.GltfToRmv.Helper;
using GameWorld.Core.Services;
using Moq;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.PackFiles.Models.Containers;
using Shared.Core.Services;
using Shared.Core.Settings;
using Shared.GameFormats.RigidModel;
using Shared.GameFormats.RigidModel.Types;
using Shared.TestUtility;
using SharpGLTF.Geometry;
using SharpGLTF.Geometry.VertexTypes;
using SharpGLTF.Materials;
using SharpGLTF.Schema2;

namespace Test.ImportExport.Importing.Importers.GltfImporterTest;

public class GltfPbrMaterialImportTests
{
    [Test]
    public void Import_OpaqueBaseColorGlb_WritesCompleteWh3MaterialWithCorrectColorSpaces()
    {
        var fixtureDirectory = CreateFixtureDirectory();
        try
        {
            var baseColorPath = CreatePng(
                fixtureDirectory,
                "base-color.png",
                Color.FromArgb(255, 64, 128, 192));
            var glbPath = CreateStaticModel(
                fixtureDirectory,
                "opaque.glb",
                material => material.WithChannelImage(KnownChannel.BaseColor, baseColorPath));
            var destination = PackFileContainer.CreatePackFile("test");

            var result = CreateImporter().Import(CreateSettings(glbPath, destination));
            Assert.That(
                result.Succeeded,
                Is.True,
                result.Exception?.ToString() ?? string.Join(Environment.NewLine, result.Errors));
            var rmv = ModelFactory.Create().Load(
                destination.FileList[@"models\opaque.rigid_model_v2"].DataSource.ReadData());
            var material = rmv.ModelList[0][0].Material;
            var observed = new
            {
                result.Succeeded,
                TextureTypes = string.Join(",", material.GetAllTextures()
                    .Select(texture => texture.TexureType)
                    .OrderBy(type => type)),
                BaseColourFormat = GetDdsFormat(
                    destination.FileList[@"models\tex\mesh_node_base_colour.dds"]),
                NormalFormat = GetDdsFormat(
                    destination.FileList[@"models\tex\mesh_node_normal.dds"]),
                MaterialMapFormat = GetDdsFormat(
                    destination.FileList[@"models\tex\mesh_node_material_map.dds"]),
                MaskFormat = GetDdsFormat(
                    destination.FileList[@"models\tex\mesh_node_mask.dds"]),
            };

            Assert.That(observed, Is.EqualTo(new
            {
                Succeeded = true,
                TextureTypes = string.Join(",", new[]
                {
                    TextureType.BaseColour,
                    TextureType.MaterialMap,
                    TextureType.Mask,
                    TextureType.Normal,
                }.OrderBy(type => type)),
                BaseColourFormat = DXGI_FORMAT.BC1_UNORM_SRGB,
                NormalFormat = DXGI_FORMAT.BC3_UNORM,
                MaterialMapFormat = DXGI_FORMAT.BC1_UNORM_SRGB,
                MaskFormat = DXGI_FORMAT.BC3_UNORM,
            }));
        }
        finally
        {
            Directory.Delete(fixtureDirectory, recursive: true);
        }
    }

    [Test]
    public void Import_MissingGameMaps_WritesNeutralWh3DefaultsWhenConversionsAreDisabled()
    {
        var fixtureDirectory = CreateFixtureDirectory();
        try
        {
            var baseColorPath = CreatePng(
                fixtureDirectory,
                "base-color.png",
                Color.CornflowerBlue);
            var glbPath = CreateStaticModel(
                fixtureDirectory,
                "neutral.glb",
                material => material.WithChannelImage(KnownChannel.BaseColor, baseColorPath));
            var destination = PackFileContainer.CreatePackFile("test");

            var result = CreateImporter().Import(CreateSettings(
                glbPath,
                destination,
                convertMaterial: false,
                convertNormal: false));

            Assert.That(
                result.Succeeded,
                Is.True,
                result.Exception?.ToString() ?? string.Join(Environment.NewLine, result.Errors));
            var normal = GetFirstPixel(destination.FileList[@"models\tex\mesh_node_normal.dds"]);
            var materialMap = GetFirstPixel(destination.FileList[@"models\tex\mesh_node_material_map.dds"]);
            var mask = GetFirstPixel(destination.FileList[@"models\tex\mesh_node_mask.dds"]);
            Assert.Multiple(() =>
            {
                Assert.That(normal.R, Is.InRange(245, 255));
                Assert.That(normal.G, Is.InRange(118, 138));
                Assert.That(normal.B, Is.InRange(0, 10));
                Assert.That(normal.A, Is.InRange(118, 138));
                Assert.That(materialMap.R, Is.InRange(0, 10));
                Assert.That(materialMap.G, Is.InRange(245, 255));
                Assert.That(materialMap.B, Is.InRange(0, 10));
                Assert.That(mask.R, Is.InRange(0, 10));
                Assert.That(mask.G, Is.InRange(0, 10));
                Assert.That(mask.B, Is.InRange(0, 10));
                Assert.That(mask.A, Is.EqualTo(255));
            });
        }
        finally
        {
            Directory.Delete(fixtureDirectory, recursive: true);
        }
    }

    [Test]
    public void Import_BlueNormalGlb_ConvertsLinearChannelsToWh3OrangeNormal()
    {
        var fixtureDirectory = CreateFixtureDirectory();
        try
        {
            var normalPath = CreatePng(
                fixtureDirectory,
                "normal.png",
                Color.FromArgb(255, 64, 128, 255),
                PixelFormat.Format24bppRgb);
            var glbPath = CreateStaticModel(
                fixtureDirectory,
                "normal.glb",
                material => material.WithChannelImage(KnownChannel.Normal, normalPath));
            var destination = PackFileContainer.CreatePackFile("test");

            var result = CreateImporter().Import(CreateSettings(glbPath, destination));

            Assert.That(
                result.Succeeded,
                Is.True,
                result.Exception?.ToString() ?? string.Join(Environment.NewLine, result.Errors));
            var pixel = GetFirstPixel(destination.FileList[@"models\tex\mesh_node_normal.dds"]);
            Assert.Multiple(() =>
            {
                Assert.That(pixel.R, Is.InRange(245, 255));
                Assert.That(pixel.G, Is.InRange(118, 138));
                Assert.That(pixel.B, Is.InRange(0, 10));
                Assert.That(pixel.A, Is.InRange(54, 74));
            });
        }
        finally
        {
            Directory.Delete(fixtureDirectory, recursive: true);
        }
    }

    [Test]
    public void Import_MetallicRoughnessGlb_SwapsRedAndBlueIntoWh3MaterialMap()
    {
        var fixtureDirectory = CreateFixtureDirectory();
        try
        {
            var metallicRoughnessPath = CreatePng(
                fixtureDirectory,
                "metallic-roughness.png",
                Color.FromArgb(255, 200, 96, 160),
                PixelFormat.Format24bppRgb);
            var glbPath = CreateStaticModel(
                fixtureDirectory,
                "material-map.glb",
                material => material.WithChannelImage(
                    KnownChannel.MetallicRoughness,
                    metallicRoughnessPath));
            var destination = PackFileContainer.CreatePackFile("test");

            var result = CreateImporter().Import(CreateSettings(glbPath, destination));

            Assert.That(
                result.Succeeded,
                Is.True,
                result.Exception?.ToString() ?? string.Join(Environment.NewLine, result.Errors));
            var pixel = GetFirstPixel(destination.FileList[@"models\tex\mesh_node_material_map.dds"]);
            Assert.Multiple(() =>
            {
                Assert.That(pixel.R, Is.InRange(150, 170));
                Assert.That(pixel.G, Is.InRange(86, 106));
                Assert.That(pixel.B, Is.InRange(190, 210));
                Assert.That(pixel.A, Is.EqualTo(255));
            });
        }
        finally
        {
            Directory.Delete(fixtureDirectory, recursive: true);
        }
    }

    [Test]
    public void Import_GltfWithRelativeExternalImage_ImportsTexture()
    {
        var fixtureDirectory = CreateFixtureDirectory();
        try
        {
            var baseColorPath = CreatePng(
                fixtureDirectory,
                "external-base-color.png",
                Color.CornflowerBlue);
            var gltfPath = CreateStaticModel(
                fixtureDirectory,
                "external.gltf",
                material => material.WithChannelImage(KnownChannel.BaseColor, baseColorPath));
            var externalImages = GetExternalImagePaths(gltfPath);
            var destination = PackFileContainer.CreatePackFile("test");

            var result = CreateImporter().Import(CreateSettings(gltfPath, destination));

            Assert.Multiple(() =>
            {
                Assert.That(externalImages, Is.Not.Empty);
                Assert.That(result.Succeeded, Is.True,
                    result.Exception?.ToString() ?? string.Join(Environment.NewLine, result.Errors));
                Assert.That(destination.FileList,
                    Contains.Key(@"models\tex\mesh_node_base_colour.dds"));
            });
        }
        finally
        {
            Directory.Delete(fixtureDirectory, recursive: true);
        }
    }

    [Test]
    public void Import_GltfWithMissingExternalImage_FailsBeforeWritingPack()
    {
        var fixtureDirectory = CreateFixtureDirectory();
        try
        {
            var baseColorPath = CreatePng(
                fixtureDirectory,
                "missing-base-color.png",
                Color.CornflowerBlue);
            var gltfPath = CreateStaticModel(
                fixtureDirectory,
                "missing.gltf",
                material => material.WithChannelImage(KnownChannel.BaseColor, baseColorPath));
            var externalImages = GetExternalImagePaths(gltfPath);
            Assert.That(externalImages, Is.Not.Empty);
            foreach (var imagePath in externalImages)
                File.Delete(imagePath);
            var destination = PackFileContainer.CreatePackFile("test");

            var result = CreateImporter().Import(CreateSettings(gltfPath, destination));

            Assert.Multiple(() =>
            {
                Assert.That(result.Succeeded, Is.False);
                Assert.That(result.Errors,
                    Has.Some.Contains("Can't read the glTF/GLB file"));
                Assert.That(destination.FileList, Is.Empty);
            });
        }
        finally
        {
            Directory.Delete(fixtureDirectory, recursive: true);
        }
    }

    [Test]
    public void Import_EquivalentDecodedImagesAndSemantic_WritesOneDdsPerSemantic()
    {
        var fixtureDirectory = CreateFixtureDirectory();
        try
        {
            var firstBaseColorPath = CreatePng(
                fixtureDirectory,
                "shared-base-color-32.png",
                Color.CornflowerBlue);
            var secondBaseColorPath = CreatePng(
                fixtureDirectory,
                "shared-base-color-24.png",
                Color.CornflowerBlue,
                PixelFormat.Format24bppRgb);
            var glbPath = CreateTwoMeshModel(
                fixtureDirectory,
                "deduplicated.glb",
                "first_node",
                firstBaseColorPath,
                "second_node",
                secondBaseColorPath);
            var destination = PackFileContainer.CreatePackFile("test");

            var result = CreateImporter().Import(CreateSettings(glbPath, destination));

            Assert.That(
                result.Succeeded,
                Is.True,
                result.Exception?.ToString() ?? string.Join(Environment.NewLine, result.Errors));
            var rmv = ModelFactory.Create().Load(
                destination.FileList[@"models\deduplicated.rigid_model_v2"].DataSource.ReadData());
            var textureTypes = new[]
            {
                TextureType.BaseColour,
                TextureType.Normal,
                TextureType.MaterialMap,
                TextureType.Mask,
            };
            Assert.Multiple(() =>
            {
                Assert.That(destination.FileList.Keys.Count(path =>
                    path.EndsWith(".dds", StringComparison.OrdinalIgnoreCase)), Is.EqualTo(4));
                foreach (var textureType in textureTypes)
                {
                    Assert.That(
                        rmv.ModelList[0][0].Material.GetTexture(textureType)!.Value.Path,
                        Is.EqualTo(rmv.ModelList[0][1].Material.GetTexture(textureType)!.Value.Path));
                }
            });
        }
        finally
        {
            Directory.Delete(fixtureDirectory, recursive: true);
        }
    }

    [Test]
    public void Import_SamePixelsWithDifferentSemantics_WritesDistinctDdsFiles()
    {
        var fixtureDirectory = CreateFixtureDirectory();
        try
        {
            var sharedPath = CreatePng(
                fixtureDirectory,
                "shared.png",
                Color.FromArgb(255, 64, 128, 192));
            var glbPath = CreateStaticModel(
                fixtureDirectory,
                "semantic.glb",
                material =>
                {
                    material.WithChannelImage(KnownChannel.BaseColor, sharedPath);
                    material.WithChannelImage(KnownChannel.Normal, sharedPath);
                });
            var destination = PackFileContainer.CreatePackFile("test");

            var result = CreateImporter().Import(CreateSettings(glbPath, destination));

            Assert.That(
                result.Succeeded,
                Is.True,
                result.Exception?.ToString() ?? string.Join(Environment.NewLine, result.Errors));
            var rmv = ModelFactory.Create().Load(
                destination.FileList[@"models\semantic.rigid_model_v2"].DataSource.ReadData());
            var baseColour = rmv.ModelList[0][0].Material.GetTexture(TextureType.BaseColour)!.Value.Path;
            var normal = rmv.ModelList[0][0].Material.GetTexture(TextureType.Normal)!.Value.Path;
            Assert.Multiple(() =>
            {
                Assert.That(baseColour, Is.Not.EqualTo(normal));
                Assert.That(destination.FileList, Contains.Key(baseColour));
                Assert.That(destination.FileList, Contains.Key(normal));
            });
        }
        finally
        {
            Directory.Delete(fixtureDirectory, recursive: true);
        }
    }

    [Test]
    public void Import_DifferentTexturesWithSameTargetPath_FailsWithoutPartialWrites()
    {
        var fixtureDirectory = CreateFixtureDirectory();
        try
        {
            var firstPath = CreatePng(fixtureDirectory, "first.png", Color.Red);
            var secondPath = CreatePng(fixtureDirectory, "second.png", Color.Blue);
            var glbPath = CreateTwoMeshModel(
                fixtureDirectory,
                "conflict.glb",
                "mesh?",
                firstPath,
                "mesh*",
                secondPath);
            var destination = PackFileContainer.CreatePackFile("test");

            var result = CreateImporter().Import(CreateSettings(glbPath, destination));

            Assert.Multiple(() =>
            {
                Assert.That(result.Succeeded, Is.False);
                Assert.That(result.Errors, Has.Some.Contains("more than once"));
                Assert.That(destination.FileList, Is.Empty);
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
        PackFileContainer destination,
        bool convertMaterial = true,
        bool convertNormal = true) => new(
        gltfPath,
        "models",
        destination,
        GameTypeEnum.Warhammer3,
        true,
        true,
        convertMaterial,
        convertNormal,
        false,
        20);

    private static string CreateStaticModel(
        string directory,
        string fileName,
        Action<MaterialBuilder> configureMaterial)
    {
        var material = new MaterialBuilder("opaque_material")
            .WithMetallicRoughness();
        configureMaterial(material);
        var geometry = new MeshBuilder<
            VertexPositionNormalTangent,
            VertexTexture1,
            VertexJoints4>("mesh");
        geometry.UsePrimitive(material).AddTriangle(
            CreateVertex(0, 0),
            CreateVertex(1, 0),
            CreateVertex(0, 1));

        var modelRoot = ModelRoot.CreateModel();
        var mesh = modelRoot.CreateMesh(geometry);
        modelRoot.UseScene("default").CreateNode("mesh_node").WithMesh(mesh);

        var path = Path.Combine(directory, fileName);
        modelRoot.Save(path);
        return path;
    }

    private static string CreateTwoMeshModel(
        string directory,
        string fileName,
        string firstNodeName,
        string firstBaseColorPath,
        string secondNodeName,
        string secondBaseColorPath)
    {
        var modelRoot = ModelRoot.CreateModel();
        var scene = modelRoot.UseScene("default");
        AddMesh(modelRoot, scene, firstNodeName, firstBaseColorPath, 0);
        AddMesh(modelRoot, scene, secondNodeName, secondBaseColorPath, 2);

        var path = Path.Combine(directory, fileName);
        modelRoot.Save(path);
        return path;
    }

    private static void AddMesh(
        ModelRoot modelRoot,
        Scene scene,
        string nodeName,
        string baseColorPath,
        float xOffset)
    {
        var material = new MaterialBuilder($"{nodeName}_material")
            .WithMetallicRoughness()
            .WithChannelImage(KnownChannel.BaseColor, baseColorPath);
        var geometry = new MeshBuilder<
            VertexPositionNormalTangent,
            VertexTexture1,
            VertexJoints4>(nodeName);
        geometry.UsePrimitive(material).AddTriangle(
            CreateVertex(xOffset, 0),
            CreateVertex(xOffset + 1, 0),
            CreateVertex(xOffset, 1));
        scene.CreateNode(nodeName).WithMesh(modelRoot.CreateMesh(geometry));
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
        Color color,
        PixelFormat pixelFormat = PixelFormat.Format32bppArgb)
    {
        var path = Path.Combine(directory, fileName);
        using var bitmap = new Bitmap(4, 4, pixelFormat);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.Clear(color);
        bitmap.Save(path, ImageFormat.Png);
        return path;
    }

    private static string CreateFixtureDirectory()
    {
        var path = Path.Combine(
            Path.GetTempPath(),
            $"AssetEditorGltfPbr-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }

    private static IReadOnlyList<string> GetExternalImagePaths(string gltfPath)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(gltfPath));
        return document.RootElement.GetProperty("images")
            .EnumerateArray()
            .Select(image => image.GetProperty("uri").GetString())
            .Where(uri => !string.IsNullOrWhiteSpace(uri) &&
                !uri.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            .Select(uri => Path.GetFullPath(
                Path.Combine(
                    Path.GetDirectoryName(gltfPath)!,
                    Uri.UnescapeDataString(uri!))))
            .ToList();
    }

    private static DXGI_FORMAT GetDdsFormat(PackFile packFile)
    {
        var data = packFile.DataSource.ReadData();
        Assert.That(data, Has.Length.GreaterThanOrEqualTo(132));
        // Formats without an sRGB variant are written with a legacy FourCC, as in vanilla WH3.
        return BitConverter.ToUInt32(data, 84) switch
        {
            0x30315844 => (DXGI_FORMAT)BitConverter.ToInt32(data, 128), // "DX10"
            0x31545844 => DXGI_FORMAT.BC1_UNORM, // "DXT1"
            0x35545844 => DXGI_FORMAT.BC3_UNORM, // "DXT5"
            var fourCc => throw new AssertionException($"Unexpected DDS FourCC 0x{fourCc:X8}"),
        };
    }

    private static Color GetFirstPixel(PackFile packFile)
    {
        var png = MeshImportExport.TextureHelper.ConvertDdsToPng(
            packFile.DataSource.ReadData());
        using var stream = new MemoryStream(png);
        using var bitmap = new Bitmap(stream);
        return bitmap.GetPixel(0, 0);
    }
}
