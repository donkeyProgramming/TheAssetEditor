using System.Drawing;
using System.Drawing.Imaging;
using System.Numerics;
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
using Shared.GameFormats.RigidModel.Types;
using Shared.GameFormats.RigidModel.Vertex;
using Shared.TestUtility;
using SharpGLTF.Geometry;
using SharpGLTF.Geometry.VertexTypes;
using SharpGLTF.Materials;
using SharpGLTF.Schema2;

namespace Test.ImportExport.Importing.Importers.GltfImporterTest;

public class GltfImporterAtomicImportTests
{
    [Test]
    public void Import_ModelTargetExists_ReturnsConflictAndLeavesPackUnchanged()
    {
        var glbPath = CreateStaticGlb();
        try
        {
            var packFileService = PackFileSerivceTestHelper.Create(
                TestData.InputPack);
            var destination = PackFileContainer.CreatePackFile("test");
            var targetPath = $"models\\{Path.GetFileNameWithoutExtension(glbPath)}.rigid_model_v2"
                .ToLowerInvariant();
            var existingFile = new PackFile(
                Path.GetFileName(targetPath),
                new MemorySource([1, 2, 3]));
            destination.FileList[targetPath] = existingFile;
            var importer = new GltfImporter(
                packFileService,
                Mock.Of<ISkeletonAnimationLookUpHelper>(),
                new RmvMaterialBuilder());

            var result = importer.Import(new GltfImporterSettings(
                glbPath,
                " models ",
                destination,
                GameTypeEnum.Warhammer3,
                true,
                false,
                false,
                false,
                false,
                20));

            Assert.Multiple(() =>
            {
                Assert.That(result.Succeeded, Is.False);
                Assert.That(result.Errors, Has.Some.Contains(targetPath));
                Assert.That(destination.FileList, Has.Count.EqualTo(1));
                Assert.That(destination.FileList[targetPath], Is.SameAs(existingFile));
                Assert.That(
                    destination.FileList[targetPath].DataSource.ReadData(),
                    Is.EqualTo(new byte[] { 1, 2, 3 }));
            });
        }
        finally
        {
            File.Delete(glbPath);
        }
    }

    [Test]
    public void Import_MultipleSkeletonMarkers_ReturnsFailureInsteadOfUsingFirstMarker()
    {
        var modelRoot = ModelRoot.CreateModel();
        var scene = modelRoot.UseScene("default");
        scene.CreateNode("//skeleton//first");
        scene.CreateNode("//skeleton//second");
        var glbPath = Path.Combine(
            Path.GetTempPath(),
            $"{Guid.NewGuid():N}.glb");
        modelRoot.SaveGLB(glbPath);
        try
        {
            var destination = PackFileContainer.CreatePackFile("test");
            var importer = new GltfImporter(
                PackFileSerivceTestHelper.Create(TestData.InputPack),
                Mock.Of<ISkeletonAnimationLookUpHelper>(),
                new RmvMaterialBuilder());

            var result = importer.Import(new GltfImporterSettings(
                glbPath,
                "models",
                destination,
                GameTypeEnum.Warhammer3,
                false,
                false,
                false,
                false,
                false,
                20));

            Assert.Multiple(() =>
            {
                Assert.That(result.Succeeded, Is.False);
                Assert.That(result.Errors, Has.Some.Contains("//skeleton// marker"));
                Assert.That(destination.FileList, Is.Empty);
            });
        }
        finally
        {
            File.Delete(glbPath);
        }
    }

    [Test]
    public void Import_InvalidAnimationRateAfterSkeletonAndMeshBuild_ReturnsFailureAndLeavesPackUnchanged()
    {
        var glbPath = CreateAnimatedSkinnedGlb();
        try
        {
            var packFileService = PackFileSerivceTestHelper.Create(
                TestData.InputPack);
            var destination = PackFileContainer.CreatePackFile("test");
            const string existingPath = @"models\existing.bin";
            var existingFile = new PackFile(
                "existing.bin",
                new MemorySource([7, 8, 9]));
            destination.FileList[existingPath] = existingFile;
            var importer = new GltfImporter(
                packFileService,
                Mock.Of<ISkeletonAnimationLookUpHelper>(),
                new RmvMaterialBuilder());
            var settings = new GltfImporterSettings(
                glbPath,
                "models",
                destination,
                GameTypeEnum.Warhammer3,
                true,
                false,
                false,
                false,
                true,
                0,
                AutoDetectAnimationKeysPerSecond: false);

            ImportResult? result = null;
            Exception? thrownException = null;
            try
            {
                result = importer.Import(settings);
            }
            catch (Exception exception)
            {
                thrownException = exception;
            }

            Assert.Multiple(() =>
            {
                Assert.That(thrownException, Is.Null);
                Assert.That(result?.Succeeded, Is.False);
                Assert.That(result?.Errors, Is.Not.Empty);
                Assert.That(destination.FileList, Has.Count.EqualTo(1));
                Assert.That(destination.FileList[existingPath], Is.SameAs(existingFile));
                Assert.That(
                    existingFile.DataSource.ReadData(),
                    Is.EqualTo(new byte[] { 7, 8, 9 }));
            });
        }
        finally
        {
            File.Delete(glbPath);
        }
    }

    [Test]
    public void Import_VertexLimitFixture_AutomaticallySplitsEveryInstanceAndReportsSummary()
    {
        var gltfPath = Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "TestData",
            "Gltf",
            "vertex_limit.gltf");
        var packFileService = PackFileSerivceTestHelper.Create(TestData.InputPack);
        var destination = PackFileContainer.CreatePackFile("test");
        const string existingPath = @"models\existing.bin";
        var existingFile = new PackFile(
            "existing.bin",
            new MemorySource([7, 8, 9]));
        destination.FileList[existingPath] = existingFile;
        var importer = new GltfImporter(
            packFileService,
            Mock.Of<ISkeletonAnimationLookUpHelper>(),
            new RmvMaterialBuilder());

        var result = importer.Import(new GltfImporterSettings(
            gltfPath,
            "models",
            destination,
            GameTypeEnum.Warhammer3,
            true,
            false,
            false,
            false,
            false,
            20));

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Warnings, Has.Some.Contains("Split 2 oversized mesh parts"));
            Assert.That(result.Warnings, Has.Some.Contains("into 4 RMV2 meshes"));
            Assert.That(destination.FileList, Has.Count.EqualTo(2));
            Assert.That(destination.FileList[existingPath], Is.SameAs(existingFile));
            Assert.That(
                existingFile.DataSource.ReadData(),
                Is.EqualTo(new byte[] { 7, 8, 9 }));

            var rmvPath = destination.FileList.Keys.Single(path =>
                path.EndsWith(".rigid_model_v2", StringComparison.OrdinalIgnoreCase));
            var rmv = ModelFactory.Create().Load(
                destination.FileList[rmvPath].DataSource.ReadData());
            Assert.That(rmv.ModelList[0], Has.Length.EqualTo(4));
            Assert.That(
                rmv.ModelList[0].Select(model => model.Mesh.VertexList.Length),
                Is.EqualTo(new[] { 65535, 3, 65535, 3 }));
            Assert.That(
                rmv.ModelList[0],
                Has.All.Matches<RmvModel>(model =>
                    model.Mesh.IndexList.All(index => index < model.Mesh.VertexList.Length)));
        });
    }

    [Test]
    public void Import_ExactVertexLimitFixture_KeepsSingleSegmentWithoutSplitWarning()
    {
        var gltfPath = Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "TestData",
            "Gltf",
            "vertex_limit_exact.gltf");
        var packFileService = PackFileSerivceTestHelper.Create(TestData.InputPack);
        var destination = PackFileContainer.CreatePackFile("test");
        var importer = new GltfImporter(
            packFileService,
            Mock.Of<ISkeletonAnimationLookUpHelper>(),
            new RmvMaterialBuilder());

        var result = importer.Import(new GltfImporterSettings(
            gltfPath,
            "models",
            destination,
            GameTypeEnum.Warhammer3,
            true,
            false,
            false,
            false,
            false,
            20));

        Assert.That(result.Succeeded, Is.True, string.Join(Environment.NewLine, result.Errors));
        var rmvPath = destination.FileList.Keys.Single(path =>
            path.EndsWith(".rigid_model_v2", StringComparison.OrdinalIgnoreCase));
        var rmv = ModelFactory.Create().Load(
            destination.FileList[rmvPath].DataSource.ReadData());
        Assert.Multiple(() =>
        {
            Assert.That(rmv.ModelList[0], Has.Length.EqualTo(1));
            Assert.That(rmv.ModelList[0][0].Mesh.VertexList, Has.Length.EqualTo(65536));
            Assert.That(result.Warnings, Has.None.Contains("oversized mesh parts"));
        });
    }

    [Test]
    public void Import_OversizedSparsePrimitive_ReportsCompactedSegment()
    {
        var gltfPath = Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "TestData",
            "Gltf",
            "vertex_limit_sparse.gltf");
        var destination = PackFileContainer.CreatePackFile("test");
        var importer = new GltfImporter(
            PackFileSerivceTestHelper.Create(TestData.InputPack),
            Mock.Of<ISkeletonAnimationLookUpHelper>(),
            new RmvMaterialBuilder());

        var result = importer.Import(new GltfImporterSettings(
            gltfPath,
            "models",
            destination,
            GameTypeEnum.Warhammer3,
            true,
            false,
            false,
            false,
            false,
            20));

        Assert.That(result.Succeeded, Is.True, string.Join(Environment.NewLine, result.Errors));
        var rmvPath = destination.FileList.Keys.Single(path =>
            path.EndsWith(".rigid_model_v2", StringComparison.OrdinalIgnoreCase));
        var rmv = ModelFactory.Create().Load(
            destination.FileList[rmvPath].DataSource.ReadData());
        Assert.Multiple(() =>
        {
            Assert.That(rmv.ModelList[0], Has.Length.EqualTo(1));
            Assert.That(rmv.ModelList[0][0].Mesh.VertexList, Has.Length.EqualTo(3));
            Assert.That(result.Warnings, Has.Some.Contains("Split 1 oversized mesh parts"));
            Assert.That(result.Warnings, Has.Some.Contains("into 1 RMV2 meshes"));
        });
    }

    [Test]
    public void Import_UnsupportedPrimitive_ReturnsReadableErrorAndLeavesPackUnchanged()
    {
        var gltfPath = Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "TestData",
            "Gltf",
            "unsupported_primitive.gltf");
        var packFileService = PackFileSerivceTestHelper.Create(TestData.InputPack);
        var destination = PackFileContainer.CreatePackFile("test");
        const string existingPath = @"models\existing.bin";
        var existingFile = new PackFile("existing.bin", new MemorySource([7, 8, 9]));
        destination.FileList[existingPath] = existingFile;
        var importer = new GltfImporter(
            packFileService,
            Mock.Of<ISkeletonAnimationLookUpHelper>(),
            new RmvMaterialBuilder());

        var result = importer.Import(new GltfImporterSettings(
            gltfPath,
            "models",
            destination,
            GameTypeEnum.Warhammer3,
            true,
            false,
            false,
            false,
            false,
            20));

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors, Has.Some.Contains("only triangles"));
            Assert.That(destination.FileList, Has.Count.EqualTo(1));
            Assert.That(destination.FileList[existingPath], Is.SameAs(existingFile));
            Assert.That(
                existingFile.DataSource.ReadData(),
                Is.EqualTo(new byte[] { 7, 8, 9 }));
        });
    }

    [Test]
    public void Import_OversizedIndexedSkinnedMaterialPrimitive_PreservesTrianglesAndAttributes()
    {
        const int sourceTriangleCount = 32768;
        const int sourceVertexCount = 65537;
        var imagePath = CreatePng();
        var glbPath = CreateOversizedIndexedSkinnedGlb(
            sourceTriangleCount,
            imagePath);
        try
        {
            var packFileService = PackFileSerivceTestHelper.Create(TestData.InputPack);
            var destination = PackFileContainer.CreatePackFile("test");
            var importer = new GltfImporter(
                packFileService,
                Mock.Of<ISkeletonAnimationLookUpHelper>(),
                new RmvMaterialBuilder());

            var result = importer.Import(new GltfImporterSettings(
                glbPath,
                "models",
                destination,
                GameTypeEnum.Warhammer3,
                true,
                true,
                false,
                false,
                false,
                20,
                NewSkeletonName: "oversized_test_skeleton",
                AutoScaleHumanoid: false));

            Assert.That(result.Succeeded, Is.True, string.Join(Environment.NewLine, result.Errors));
            var rmvPath = destination.FileList.Keys.Single(path =>
                path.EndsWith(".rigid_model_v2", StringComparison.OrdinalIgnoreCase));
            var rmv = ModelFactory.Create().Load(
                destination.FileList[rmvPath].DataSource.ReadData());
            var models = rmv.ModelList[0];
            var splitModels = models.Take(2).ToArray();
            var duplicatedCenterVertices = splitModels
                .SelectMany(model => model.Mesh.VertexList)
                .Where(vertex => Math.Abs(vertex.Position.X) < 0.0001f)
                .ToList();

            Assert.Multiple(() =>
            {
                Assert.That(models, Has.Length.EqualTo(3));
                Assert.That(
                    models.Select(model => model.Mesh.VertexList.Length),
                    Is.EqualTo(new[] { 65535, 3, 3 }));
                Assert.That(
                    splitModels.Sum(model => model.Mesh.VertexList.Length),
                    Is.EqualTo(sourceVertexCount + 1));
                Assert.That(
                    splitModels.Sum(model => model.Mesh.IndexList.Length),
                    Is.EqualTo(sourceTriangleCount * 3));
                Assert.That(
                    models.Select(model => model.Material.ModelName),
                    Is.Unique);
                Assert.That(
                    models.Select(model => model.Material.ModelName),
                    Does.Contain("mesh_node_split1"));
                Assert.That(
                    models,
                    Has.All.Matches<RmvModel>(model =>
                        model.Material.MaterialId == ModelMaterialEnum.weighted &&
                        model.Material.GetAllTextures().Any(texture =>
                            texture.TexureType == TextureType.BaseColour)));
                Assert.That(
                    models.SelectMany(model => model.Mesh.VertexList),
                    Has.All.Matches<CommonVertex>(vertex =>
                        vertex.WeightCount == 4 &&
                        vertex.BoneIndex.SequenceEqual(new byte[] { 0, 1, 2, 3 }) &&
                        Math.Abs(vertex.BoneWeight[0] - 0.4f) < 0.003f &&
                        Math.Abs(vertex.BoneWeight[1] - 0.3f) < 0.003f &&
                        Math.Abs(vertex.BoneWeight[2] - 0.2f) < 0.003f &&
                        Math.Abs(vertex.BoneWeight[3] - 0.1f) < 0.003f &&
                        Math.Abs(vertex.BoneWeight.Sum() - 1) < 0.0001f));
                Assert.That(
                    models.SelectMany(model => model.Mesh.VertexList),
                    Has.All.Matches<CommonVertex>(vertex =>
                        Vector3.Dot(
                            new Vector3(vertex.Normal.X, vertex.Normal.Y, vertex.Normal.Z),
                            Vector3.UnitZ) > 0.99f));
                Assert.That(
                    models.SelectMany(model => model.Mesh.VertexList),
                    Has.All.Matches<CommonVertex>(vertex =>
                        Vector3.Dot(
                            new Vector3(vertex.Tangent.X, vertex.Tangent.Y, vertex.Tangent.Z),
                            -Vector3.UnitX) > 0.99f));
                Assert.That(
                    models.SelectMany(model => model.Mesh.VertexList),
                    Has.All.Matches<CommonVertex>(vertex =>
                        Vector3.Dot(
                            new Vector3(vertex.BiNormal.X, vertex.BiNormal.Y, vertex.BiNormal.Z),
                            Vector3.UnitY) > 0.99f));
                Assert.That(
                    splitModels,
                    Has.All.Matches<RmvModel>(model =>
                    {
                        var centerIndex = Array.FindIndex(
                            model.Mesh.VertexList,
                            vertex => Math.Abs(vertex.Position.X) < 0.0001f);
                        return centerIndex >= 0 &&
                            model.Mesh.IndexList.Chunk(3).All(indices =>
                                indices[0] == centerIndex &&
                                indices[1] != centerIndex &&
                                indices[2] != centerIndex &&
                                model.Mesh.VertexList[indices[1]].Uv ==
                                    new Microsoft.Xna.Framework.Vector2(1, 1) &&
                                model.Mesh.VertexList[indices[2]].Uv ==
                                    new Microsoft.Xna.Framework.Vector2(1, 0) &&
                                model.Mesh.VertexList[indices[1]].Position.X ==
                                    model.Mesh.VertexList[indices[2]].Position.X &&
                                model.Mesh.VertexList[indices[1]].Position.Y == 1 &&
                                model.Mesh.VertexList[indices[2]].Position.Y == 0) &&
                            model.Mesh.IndexList
                                .Where(index => index != centerIndex)
                                .GroupBy(index => index)
                                .All(group => group.Count() == 1);
                    }));
                Assert.That(duplicatedCenterVertices, Has.Count.EqualTo(2));
                Assert.That(
                    duplicatedCenterVertices.All(vertex =>
                        vertex.Uv.X == 0 && vertex.Uv.Y == 0),
                    Is.True);
                Assert.That(
                    splitModels.Select(model => model.Material.GetAllTextures()
                        .Single(texture => texture.TexureType == TextureType.BaseColour).Path),
                    Has.All.EqualTo(@"models\tex\mesh_node_base_colour.dds"));
            });
        }
        finally
        {
            File.Delete(glbPath);
            File.Delete(imagePath);
        }
    }

    [Test]
    public void Import_ModelSkeletonAnimationAndTexture_SucceedsWithOnePackWriteAndReportsEveryOutput()
    {
        var imagePath = CreatePng();
        var glbPath = CreateAnimatedSkinnedGlb(imagePath);
        try
        {
            var innerPackFileService = PackFileSerivceTestHelper.Create(
                TestData.InputPack);
            var destination = PackFileContainer.CreatePackFile("test");
            var packFileService = new Mock<IPackFileService>();
            packFileService
                .Setup(service => service.AddFilesToPack(
                    destination,
                    It.IsAny<List<NewPackFileEntry>>()))
                .Callback<IPackFileContainer, List<NewPackFileEntry>>((container, entries) =>
                        innerPackFileService.AddFilesToPack(
                            container,
                            entries));
            var importer = new GltfImporter(
                packFileService.Object,
                Mock.Of<ISkeletonAnimationLookUpHelper>(),
                new RmvMaterialBuilder());

            var result = importer.Import(new GltfImporterSettings(
                glbPath,
                "models",
                destination,
                GameTypeEnum.Warhammer3,
                true,
                true,
                false,
                false,
                true,
                20));

            Assert.Multiple(() =>
            {
                Assert.That(result.Succeeded, Is.True);
                Assert.That(result.Errors, Is.Empty);
                Assert.That(result.Warnings, Is.Empty);
                Assert.That(result.OutputPaths, Is.EquivalentTo(destination.FileList.Keys));
                Assert.That(destination.FileList.Keys, Has.Some.EndsWith(".rigid_model_v2"));
                Assert.That(destination.FileList.Keys, Has.Some.EndsWith(".dds"));
                Assert.That(destination.FileList.Keys.Count(path => path.EndsWith(".anim")), Is.EqualTo(2));
            });
            packFileService.Verify(
                service => service.AddFilesToPack(
                    destination,
                    It.IsAny<List<NewPackFileEntry>>()),
                Times.Once);
        }
        finally
        {
            File.Delete(glbPath);
            File.Delete(imagePath);
        }
    }

    [Test]
    public void Import_AllOutputTargetsExist_ReportsEveryConflictAndLeavesPackUnchanged()
    {
        var imagePath = CreatePng();
        var glbPath = CreateAnimatedSkinnedGlb(imagePath);
        try
        {
            var packFileService = PackFileSerivceTestHelper.Create(
                TestData.InputPack);
            var destination = PackFileContainer.CreatePackFile("test");
            var baseName = Path.GetFileNameWithoutExtension(glbPath)
                .ToLowerInvariant();
            var conflictPaths = new[]
            {
                @"animations\skeletons\test_skeleton.anim",
                $@"models\{baseName}_idle.anim",
                @"models\tex\mesh_node_base_colour.dds",
                $@"models\{baseName}.rigid_model_v2",
            };
            var existingFiles = conflictPaths
                .Select((path, index) => new
                {
                    Path = path,
                    Content = (byte)(index + 1),
                    File = new PackFile(
                        Path.GetFileName(path),
                        new MemorySource([(byte)(index + 1)])),
                })
                .ToList();
            foreach (var existing in existingFiles)
                destination.FileList[existing.Path] = existing.File;

            var importer = new GltfImporter(
                packFileService,
                Mock.Of<ISkeletonAnimationLookUpHelper>(),
                new RmvMaterialBuilder());
            var result = importer.Import(new GltfImporterSettings(
                glbPath,
                "models",
                destination,
                GameTypeEnum.Warhammer3,
                true,
                true,
                false,
                false,
                true,
                20));

            Assert.Multiple(() =>
            {
                Assert.That(result.Succeeded, Is.False);
                Assert.That(result.Errors, Has.Count.EqualTo(conflictPaths.Length + 1));
                Assert.That(result.Errors[^1], Does.Contain("Overwrite existing files"));
                foreach (var conflictPath in conflictPaths)
                    Assert.That(result.Errors, Has.Some.Contains(conflictPath));
                Assert.That(destination.FileList, Has.Count.EqualTo(existingFiles.Count));
                foreach (var existing in existingFiles)
                {
                    Assert.That(destination.FileList[existing.Path], Is.SameAs(existing.File));
                    Assert.That(
                        existing.File.DataSource.ReadData(),
                        Is.EqualTo(new[] { existing.Content }));
                }
            });
        }
        finally
        {
            File.Delete(glbPath);
            File.Delete(imagePath);
        }
    }

    [Test]
    public void Import_ModelTargetExistsWithOverwrite_ReplacesFile()
    {
        var glbPath = CreateStaticGlb();
        try
        {
            var packFileService = PackFileSerivceTestHelper.Create(TestData.InputPack);
            var destination = PackFileContainer.CreatePackFile("test");
            var targetPath = $@"models\{Path.GetFileNameWithoutExtension(glbPath)}.rigid_model_v2".ToLowerInvariant();
            destination.FileList[targetPath] = new PackFile(Path.GetFileName(targetPath), new MemorySource([1, 2, 3]));
            var importer = new GltfImporter(packFileService, Mock.Of<ISkeletonAnimationLookUpHelper>(), new RmvMaterialBuilder());

            var result = importer.Import(new GltfImporterSettings(
                glbPath, "models", destination, GameTypeEnum.Warhammer3,
                true, false, false, false, false, 20,
                OverwriteExisting: true));

            Assert.Multiple(() =>
            {
                Assert.That(result.Succeeded, Is.True, string.Join(Environment.NewLine, result.Errors));
                Assert.That(result.OutputPaths, Is.EqualTo(new[] { targetPath }));
                Assert.That(destination.FileList, Has.Count.EqualTo(1));
                var model = ModelFactory.Create().Load(destination.FileList[targetPath].DataSource.ReadData());
                Assert.That(model.ModelList[0], Is.Not.Empty);
            });
        }
        finally
        {
            File.Delete(glbPath);
        }
    }

    [Test]
    public void Import_MaterialAtPackRoot_NormalizesTextureTargetPath()
    {
        var imagePath = CreatePng();
        var glbPath = CreateAnimatedSkinnedGlb(imagePath);
        try
        {
            var packFileService = PackFileSerivceTestHelper.Create(
                TestData.InputPack);
            var destination = PackFileContainer.CreatePackFile("test");
            var importer = new GltfImporter(
                packFileService,
                Mock.Of<ISkeletonAnimationLookUpHelper>(),
                new RmvMaterialBuilder());

            var result = importer.Import(new GltfImporterSettings(
                glbPath,
                "",
                destination,
                GameTypeEnum.Warhammer3,
                false,
                true,
                false,
                false,
                false,
                20));

            Assert.Multiple(() =>
            {
                Assert.That(result.Succeeded, Is.True);
                Assert.That(result.OutputPaths, Is.EqualTo(new[]
                {
                    @"tex\mesh_node_base_colour.dds",
                    @"tex\mesh_node_normal.dds",
                    @"tex\mesh_node_material_map.dds",
                    @"tex\mesh_node_mask.dds",
                }));
                Assert.That(destination.FileList.Keys, Is.EquivalentTo(result.OutputPaths));
            });
        }
        finally
        {
            File.Delete(glbPath);
            File.Delete(imagePath);
        }
    }

    private static string CreateStaticGlb()
    {
        var material = new MaterialBuilder("material")
            .WithMetallicRoughness();
        var geometry = new MeshBuilder<
            VertexPositionNormalTangent,
            VertexTexture1,
            VertexJoints4>("mesh");
        var primitive = geometry.UsePrimitive(material);
        primitive.AddTriangle(
            CreateVertex(0, 0),
            CreateVertex(1, 0),
            CreateVertex(0, 1));

        var modelRoot = ModelRoot.CreateModel();
        var mesh = modelRoot.CreateMesh(geometry);
        modelRoot.UseScene("default").CreateNode("mesh_node").WithMesh(mesh);

        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.glb");
        modelRoot.SaveGLB(path);
        return path;
    }

    private static string CreateAnimatedSkinnedGlb(string? imagePath = null)
    {
        var material = new MaterialBuilder("material")
            .WithMetallicRoughness();
        if (imagePath != null)
            material.WithChannelImage(KnownChannel.BaseColor, imagePath);
        var geometry = new MeshBuilder<
            VertexPositionNormalTangent,
            VertexTexture1,
            VertexJoints4>("mesh");
        var primitive = geometry.UsePrimitive(material);
        primitive.AddTriangle(
            CreateVertex(0, 0),
            CreateVertex(1, 0),
            CreateVertex(0, 1));

        var modelRoot = ModelRoot.CreateModel();
        var mesh = modelRoot.CreateMesh(geometry);
        var scene = modelRoot.UseScene("default");
        scene.CreateNode("//skeleton//test_skeleton");
        var root = scene.CreateNode("root");
        scene.CreateNode("mesh_node").WithSkinnedMesh(
            mesh,
            (root, Matrix4x4.Identity));
        modelRoot.CreateAnimation("idle").CreateTranslationChannel(
            root,
            new Dictionary<float, Vector3>
            {
                [0] = Vector3.Zero,
                [0.05f] = Vector3.UnitY,
            });

        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.glb");
        modelRoot.SaveGLB(path);
        return path;
    }

    private static string CreateOversizedIndexedSkinnedGlb(
        int triangleCount,
        string imagePath)
    {
        var material = new MaterialBuilder("material")
            .WithMetallicRoughness()
            .WithChannelImage(KnownChannel.BaseColor, imagePath);
        var geometry = new MeshBuilder<
            VertexPositionNormalTangent,
            VertexTexture1,
            VertexJoints4>("mesh");
        var primitive = geometry.UsePrimitive(material);
        var center = CreateOversizedVertex(Vector3.Zero, Vector2.Zero);
        for (var triangleIndex = 0; triangleIndex < triangleCount; triangleIndex++)
        {
            var x = triangleIndex + 1;
            primitive.AddTriangle(
                center,
                CreateOversizedVertex(new Vector3(x, 0, 0), new Vector2(1, 0)),
                CreateOversizedVertex(new Vector3(x, 1, 0), new Vector2(1, 1)));
        }

        var collisionGeometry = new MeshBuilder<
            VertexPositionNormalTangent,
            VertexTexture1,
            VertexJoints4>("collision_mesh");
        collisionGeometry.UsePrimitive(material).AddTriangle(
            CreateOversizedVertex(new Vector3(0, 0, 1), Vector2.Zero),
            CreateOversizedVertex(new Vector3(1, 0, 1), new Vector2(1, 0)),
            CreateOversizedVertex(new Vector3(1, 1, 1), new Vector2(1, 1)));

        var modelRoot = ModelRoot.CreateModel();
        var mesh = modelRoot.CreateMesh(geometry);
        var collisionMesh = modelRoot.CreateMesh(collisionGeometry);
        var scene = modelRoot.UseScene("default");
        var root = scene.CreateNode("root");
        var bone1 = root.CreateNode("bone1");
        var bone2 = bone1.CreateNode("bone2");
        var bone3 = bone2.CreateNode("bone3");
        scene.CreateNode("mesh_node").WithSkinnedMesh(
            mesh,
            (root, Matrix4x4.Identity),
            (bone1, Matrix4x4.Identity),
            (bone2, Matrix4x4.Identity),
            (bone3, Matrix4x4.Identity));
        scene.CreateNode("mesh_node_split1").WithSkinnedMesh(
            collisionMesh,
            (root, Matrix4x4.Identity),
            (bone1, Matrix4x4.Identity),
            (bone2, Matrix4x4.Identity),
            (bone3, Matrix4x4.Identity));

        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.glb");
        modelRoot.SaveGLB(path);
        return path;
    }

    private static VertexBuilder<
        VertexPositionNormalTangent,
        VertexTexture1,
        VertexJoints4> CreateOversizedVertex(Vector3 position, Vector2 uv)
    {
        var vertex = new VertexBuilder<
            VertexPositionNormalTangent,
            VertexTexture1,
            VertexJoints4>();
        vertex.Geometry.Position = position;
        vertex.Geometry.Normal = Vector3.UnitZ;
        vertex.Geometry.Tangent = new Vector4(1, 0, 0, 1);
        vertex.Material.TexCoord = uv;
        vertex.Skinning.SetBindings(
            (0, 0.4f),
            (1, 0.3f),
            (2, 0.2f),
            (3, 0.1f));
        return vertex;
    }

    private static string CreatePng()
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.png");
        using var bitmap = new Bitmap(2, 2);
        bitmap.SetPixel(0, 0, Color.White);
        bitmap.Save(path, ImageFormat.Png);
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
}
