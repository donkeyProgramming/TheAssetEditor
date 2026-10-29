using System.IO;
using Editors.ImportExport.Exporting.Exporters.DdsToMaterialPng;
using Editors.ImportExport.Exporting.Exporters.DdsToNormalPng;
using Editors.ImportExport.Exporting.Exporters.RmvToGltf;
using Editors.ImportExport.Exporting.Exporters.RmvToGltf.Helpers;
using GameWorld.Core.Services;
using Moq;
using Shared.Core.PackFiles.Models;
using Shared.GameFormats.RigidModel;
using Shared.GameFormats.RigidModel.LodHeader;
using Shared.GameFormats.RigidModel.MaterialHeaders;
using Shared.GameFormats.RigidModel.Types;
using Shared.GameFormats.RigidModel.Vertex;
using SharpGLTF.Materials;
using SharpGLTF.Schema2;

namespace Test.ImportExport.Exporting.Exporters.RmvToGlft;

public class GltfTextureExportSessionTests
{
    [Test]
    public void ComposedSessionReusesAnIdenticalSourcePath()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"asset-editor-textures-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);

        try
        {
            var materialExporter = new Mock<IDdsToMaterialPngExporter>();
            materialExporter
                .Setup(x => x.Export(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
                .Returns((string source, string output, bool _) =>
                {
                    var path = Path.Combine(Path.GetDirectoryName(output)!, "shared.png");
                    File.WriteAllBytes(path, System.Text.Encoding.UTF8.GetBytes(source));
                    return path;
                });

            var handler = new GltfTextureHandler(new Mock<IDdsToNormalPngExporter>().Object, materialExporter.Object);
            var asset = CreateAsset("textures/same/shared.dds", "textures/same/shared.dds");
            var settings = new RmvToGltfExporterSettings(
                asset.InputFile,
                [],
                Path.Combine(outputDirectory, "model.glb"),
                true,
                false,
                false,
                false,
                false);

            var textures = handler.HandleTextures(asset, settings, new GltfTextureExportSession(collisionSafe: true));

            Assert.That(textures, Has.Count.EqualTo(2));
            Assert.That(textures.Select(x => x.SystemFilePath).Distinct(StringComparer.OrdinalIgnoreCase).Count(), Is.EqualTo(1));
            materialExporter.Verify(x => x.Export(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Once);
        }
        finally
        {
            if (Directory.Exists(outputDirectory))
                Directory.Delete(outputDirectory, recursive: true);
        }
    }

    [Test]
    public void ComposedSessionDisambiguatesDifferentSourcesWithTheSameBasename()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"asset-editor-textures-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);

        try
        {
            var materialExporter = new Mock<IDdsToMaterialPngExporter>();
            materialExporter
                .Setup(x => x.Export(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
                .Returns((string source, string output, bool _) =>
                {
                    var path = Path.Combine(Path.GetDirectoryName(output)!, "shared.png");
                    File.WriteAllBytes(path, System.Text.Encoding.UTF8.GetBytes(source));
                    return path;
                });

            var normalExporter = new Mock<IDdsToNormalPngExporter>();
            var handler = new GltfTextureHandler(normalExporter.Object, materialExporter.Object);
            var asset = CreateAsset(
                "textures/first/shared.dds",
                "textures/second/shared.dds");
            var settings = new RmvToGltfExporterSettings(
                asset.InputFile,
                [],
                Path.Combine(outputDirectory, "model.glb"),
                true,
                false,
                false,
                false,
                false);

            var textures = handler.HandleTextures(asset, settings, new GltfTextureExportSession(collisionSafe: true));

            Assert.That(textures, Has.Count.EqualTo(2));
            Assert.That(textures.Select(x => x.SystemFilePath).Distinct(StringComparer.OrdinalIgnoreCase).Count(), Is.EqualTo(2));
            Assert.That(textures.All(x => File.Exists(x.SystemFilePath)), Is.True);
            materialExporter.Verify(x => x.Export(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Exactly(2));
        }
        finally
        {
            if (Directory.Exists(outputDirectory))
                Directory.Delete(outputDirectory, recursive: true);
        }
    }

    [Test]
    public void MaskExportDoesNotReplaceBaseColourInFinalGlbMaterial()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"asset-editor-textures-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);

        var outputPath = Path.Combine(outputDirectory, "model.glb");
        var baseColourPath = Path.Combine(outputDirectory, "body_base_colour.png");
        var maskPath = Path.Combine(outputDirectory, "body_mask.png");
        File.WriteAllBytes(baseColourPath, OnePixelPng);

        try
        {
            var materialExporter = new Mock<IDdsToMaterialPngExporter>();
            materialExporter
                .Setup(x => x.Export(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
                .Returns((string source, string output, bool convertToBlender) =>
                    source.EndsWith("base_colour.dds", StringComparison.OrdinalIgnoreCase)
                        ? baseColourPath
                        // Deliberately leave this path absent. The production
                        // handler still invokes the exporter, while avoiding a
                        // platform image decoder in this channel-binding test.
                        : maskPath);

            var normalExporter = new Mock<IDdsToNormalPngExporter>();
            var handler = new GltfTextureHandler(normalExporter.Object, materialExporter.Object);
            var asset = CreateTexturedAsset(
                "textures/body_base_colour.dds",
                "textures/body_mask.dds");
            var settings = new RmvToGltfExporterSettings(
                asset.InputFile,
                [],
                outputPath,
                true,
                false,
                false,
                false,
                false);

            var textures = handler.HandleTextures(
                asset,
                settings,
                new GltfTextureExportSession(collisionSafe: false));

            materialExporter.Verify(
                x => x.Export(
                    It.Is<string>(path => path.EndsWith("base_colour.dds", StringComparison.OrdinalIgnoreCase)),
                    It.IsAny<string>(),
                    It.IsAny<bool>()),
                Times.Once);
            materialExporter.Verify(
                x => x.Export(
                    It.Is<string>(path => path.EndsWith("body_mask.dds", StringComparison.OrdinalIgnoreCase)),
                    It.IsAny<string>(),
                    It.IsAny<bool>()),
                Times.Once);

            Assert.That(textures, Has.Count.EqualTo(1));
            Assert.That(textures[0].GlftTexureType, Is.EqualTo(KnownChannel.BaseColor));
            Assert.That(textures[0].SystemFilePath, Is.EqualTo(baseColourPath));

            var meshBuilder = new GltfMeshBuilder()
                .Build(asset, textures, settings, willHaveSkeleton: false)
                .Single();
            var model = ModelRoot.CreateModel();
            var scene = model.UseScene("default");
            var mesh = model.CreateMesh(meshBuilder);
            scene.CreateNode("body").WithMesh(mesh);

            var baseColourChannel = model.LogicalMaterials.Single().FindChannel("BaseColor");
            Assert.That(baseColourChannel, Is.Not.Null);
            var baseColourImage = baseColourChannel!.Value.Texture!.PrimaryImage!;
            Assert.That(baseColourImage.Content.SourcePath, Does.EndWith("body_base_colour.png"));
            Assert.That(baseColourImage.Content.SourcePath, Does.Not.EndWith("body_mask.png"));

            var staticModel = ModelRoot.CreateModel();
            staticModel.CreateMesh(new GltfStaticMeshBuilder().Build(asset, textures, settings).Single());
            Assert.That(staticModel.LogicalMaterials.Single().Alpha, Is.EqualTo(SharpGLTF.Schema2.AlphaMode.MASK));

            model.Save(outputPath);
            ModelRoot.Validate(outputPath);
            var reloaded = ModelRoot.Load(outputPath);
            var reloadedBaseColourChannel = reloaded.LogicalMaterials.Single().FindChannel("BaseColor");
            Assert.That(reloadedBaseColourChannel, Is.Not.Null);
            Assert.That(reloadedBaseColourChannel!.Value.Texture, Is.Not.Null);
            Assert.That(reloaded.LogicalImages, Has.Count.EqualTo(1));
        }
        finally
        {
            if (Directory.Exists(outputDirectory))
                Directory.Delete(outputDirectory, recursive: true);
        }
    }

    private static ResolvedModelAsset CreateAsset(string firstTexture, string secondTexture)
    {
        var firstModel = CreateModel("first", firstTexture);
        var secondModel = CreateModel("second", secondTexture);
        var models = new[] { firstModel, secondModel };
        var header = new RmvFileHeader { Version = RmvVersionEnum.RMV2_V6, LodCount = 1 };
        header.SkeletonName = string.Empty;
        var rmv = new RmvFile
        {
            Header = header,
            ModelList = new[] { models },
            LodHeaders = new[] { LodHeaderFactory.Create().CreateEmpty(RmvVersionEnum.RMV2_V6, 0, 0, 0) }
        };

        var parts = new[]
        {
            new ResolvedModelPart(0, 0, firstModel, ResolvedModelMaterial.Create(firstModel.Material)),
            new ResolvedModelPart(0, 1, secondModel, ResolvedModelMaterial.Create(secondModel.Material))
        };
        var input = PackFile.CreateFromASCII("model.rigid_model_v2", "model");
        return new ResolvedModelAsset(input, input, null, null, rmv, new[] { parts }, []);
    }

    private static RmvModel CreateModel(string name, string texture)
    {
        var material = new WeightedMaterial { ModelName = name };
        material.SetTexture(TextureType.Diffuse, texture);
        return new RmvModel
        {
            Material = material,
            Mesh = new RmvMesh { VertexList = [], IndexList = [] }
        };
    }

    private static ResolvedModelAsset CreateTexturedAsset(string baseColourPath, string maskPath)
    {
        var material = new WeightedMaterial { ModelName = "textured" };
        material.SetTexture(TextureType.BaseColour, baseColourPath);
        material.SetTexture(TextureType.Mask, maskPath);

        var model = new RmvModel
        {
            Material = material,
            Mesh = new RmvMesh
            {
                VertexList =
                [
                    CreateVertex(0, 0, 0),
                    CreateVertex(1, 0, 0),
                    CreateVertex(0, 1, 0)
                ],
                IndexList = [0, 1, 2]
            }
        };
        var header = new RmvFileHeader
        {
            Version = RmvVersionEnum.RMV2_V6,
            LodCount = 1,
            SkeletonName = string.Empty
        };
        var rmv = new RmvFile
        {
            Header = header,
            ModelList = [new[] { model }],
            LodHeaders = [LodHeaderFactory.Create().CreateEmpty(RmvVersionEnum.RMV2_V6, 0, 0, 0)]
        };
        var input = PackFile.CreateFromASCII("textured.rigid_model_v2", "textured");
        var part = new ResolvedModelPart(0, 0, model, ResolvedModelMaterial.Create(material));
        return new ResolvedModelAsset(input, input, null, null, rmv, [new[] { part }], []);
    }

    private static CommonVertex CreateVertex(float x, float y, float z)
        => new()
        {
            Position = new Microsoft.Xna.Framework.Vector4(x, y, z, 1),
            Normal = Microsoft.Xna.Framework.Vector3.UnitZ,
            Tangent = Microsoft.Xna.Framework.Vector3.UnitX,
            Uv = Microsoft.Xna.Framework.Vector2.Zero,
            BoneIndex = new byte[4],
            BoneWeight = new float[4]
        };

    private static readonly byte[] OnePixelPng = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=");
}
