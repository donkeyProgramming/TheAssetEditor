using Editors.ImportExport.Importing;
using Editors.ImportExport.Importing.Importers.GltfToRmv;
using Editors.ImportExport.Importing.Importers.GltfToRmv.Helper;
using Editors.ImportExport.Importing.Presentation;
using Editors.ImportImport.Importing.Presentation.RmvToGltf;
using GameWorld.Core.Services;
using Moq;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.PackFiles.Models.FileSources;
using Shared.Core.PackFiles.Models.Containers;
using Shared.Core.Services;
using Shared.Core.Settings;
using Shared.GameFormats.Animation;
using Shared.GameFormats.RigidModel;
using Shared.GameFormats.RigidModel.Transforms;
using Shared.GameFormats.RigidModel.Vertex;
using Shared.TestUtility;
using SharpGLTF.Geometry;
using SharpGLTF.Geometry.VertexTypes;
using SharpGLTF.Materials;
using SharpGLTF.Schema2;
using Numerics = System.Numerics;

namespace Test.ImportExport.Importing.Importers.GltfImporterTest;

public class GltfFullWorkflowImportTests
{
    private const string HumanoidSkeletonPath =
        @"animations\skeletons\humanoid01.anim";

    [Test]
    public void Import_OfficialBlenderFullWorkflowFixture_CreatesEveryAssetAndSummary()
    {
        var glbPath = Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "TestData",
            "Gltf",
            "external_full_workflow.glb");
        var modelRoot = ModelRoot.Load(glbPath);
        Assert.Multiple(() =>
        {
            Assert.That(
                modelRoot.Asset.Generator,
                Does.StartWith("Khronos glTF Blender I/O"));
            Assert.That(
                modelRoot.LogicalNodes,
                Has.None.Matches<Node>(node =>
                    node.Name?.StartsWith(
                        "//skeleton//",
                        StringComparison.OrdinalIgnoreCase) == true));
            Assert.That(modelRoot.LogicalMeshes.Single().Primitives, Has.Count.EqualTo(2));
            Assert.That(modelRoot.LogicalAnimations.Select(animation => animation.Name),
                Is.EquivalentTo(new[] { "Move", "Nod" }));
            Assert.That(
                modelRoot.LogicalAnimations.Single(animation => animation.Name == "Move")
                    .Channels.Select(channel => channel.TargetNode.Name),
                Is.EqualTo(new[] { "root" }));
            Assert.That(
                modelRoot.LogicalAnimations.Single(animation => animation.Name == "Nod")
                    .Channels.Select(channel => channel.TargetNode.Name),
                Is.EqualTo(new[] { "head" }));
        });

        var destination = PackFileContainer.CreatePackFile("test");
        var progressUpdates = new List<ImportProgress>();
        ImportProgress? progressAtPackWrite = null;
        var importer = CreateImporter(
            destination,
            () => progressAtPackWrite = progressUpdates.LastOrDefault());
        var result = importer.Import(new GltfImporterSettings(
            glbPath,
            "models",
            destination,
            GameTypeEnum.Warhammer3,
            ImportMeshes: true,
            ImportMaterials: true,
            ConvertMaterialFromBlenderType: true,
            ConvertNormalTextureFromBlueToOrangeType: true,
            ImportAnimations: true,
            AnimationKeysPerSecond: 24,
            AutoDetectAnimationKeysPerSecond: true,
            AutoScaleHumanoid: true),
            new InlineProgress<ImportProgress>(progressUpdates.Add));

        Assert.That(
            result.Succeeded,
            Is.True,
            string.Join(Environment.NewLine, result.Errors));

        const string rmvPath = @"models\external_full_workflow.rigid_model_v2";
        const string skeletonPath =
            @"animations\skeletons\externalworkflowarmature.anim";
        var animationPaths = new[]
        {
            @"models\external_full_workflow_move.anim",
            @"models\external_full_workflow_nod.anim",
        };
        var rmv = ModelFactory.Create().Load(
            destination.FileList[rmvPath].DataSource.ReadData());
        var skeleton = AnimationFile.Create(destination.FileList[skeletonPath]);
        var moveAnimation = AnimationFile.Create(
            destination.FileList[animationPaths[0]]);
        var nodAnimation = AnimationFile.Create(
            destination.FileList[animationPaths[1]]);
        var rootBoneIndex = Array.FindIndex(
            skeleton.Bones,
            bone => bone.Name == "root");
        var headBoneIndex = Array.FindIndex(
            skeleton.Bones,
            bone => bone.Name == "head");
        var texturePaths = rmv.ModelList[0]
            .SelectMany(model => model.Material.GetAllTextures())
            .Select(texture => texture.Path.ToLowerInvariant())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var message = ImporterCoreViewModel.BuildResultMessage(result);

        Assert.Multiple(() =>
        {
            Assert.That(result.OutputPaths, Is.EquivalentTo(destination.FileList.Keys));
            Assert.That(result.OutputPaths, Does.Contain(rmvPath));
            Assert.That(result.OutputPaths, Does.Contain(skeletonPath));
            Assert.That(result.OutputPaths, Is.SupersetOf(animationPaths));
            Assert.That(rmv.LodHeaders, Has.Length.EqualTo(1));
            Assert.That(rmv.ModelList[0], Has.Length.EqualTo(2));
            Assert.That(rmv.Header.SkeletonName, Is.EqualTo("ExternalWorkflowArmature"));
            Assert.That(
                rmv.ModelList[0].Select(model => model.Material.ModelName),
                Is.EquivalentTo(new[]
                {
                    "ExternalWorkflowMesh_part1",
                    "ExternalWorkflowMesh_part2",
                }));
            Assert.That(
                rmv.ModelList[0]
                    .SelectMany(model => model.Mesh.VertexList)
                    .Max(vertex => vertex.Position.Y),
                Is.EqualTo(2).Within(0.0001f));
            Assert.That(
                rmv.ModelList[0]
                    .SelectMany(model => model.Mesh.VertexList),
                Has.All.Matches<CommonVertex>(vertex =>
                    vertex.BoneWeight
                        .Select(weight => (byte)(weight * byte.MaxValue))
                        .Sum(value => value) == byte.MaxValue));
            Assert.That(
                skeleton.Bones.Select(bone => bone.Name),
                Is.EqualTo(new[]
                {
                    "root",
                    "pelvis",
                    "head",
                    "left_foot",
                    "right_foot",
                    "helper",
                }));
            Assert.That(animationPaths, Has.All.Matches<string>(path =>
                AnimationFile.Create(destination.FileList[path])
                    .Header.SkeletonName == "ExternalWorkflowArmature"));
            Assert.That(
                moveAnimation.AnimationParts[0].DynamicFrames[^1]
                    .Transforms[rootBoneIndex].Z,
                Is.EqualTo(0.25f).Within(0.001f));
            Assert.That(
                Math.Abs(nodAnimation.AnimationParts[0].DynamicFrames[^1]
                    .Quaternion[headBoneIndex].X),
                Is.EqualTo(MathF.Sin(MathF.PI / 18)).Within(0.001f));
            Assert.That(texturePaths, Has.Count.GreaterThanOrEqualTo(5));
            Assert.That(texturePaths, Has.All.Matches<string>(path =>
                destination.FileList.ContainsKey(path)));
            Assert.That(result.HumanoidScale!.Applied, Is.True);
            Assert.That(result.HumanoidScale.SourceHeight, Is.EqualTo(4).Within(0.0001f));
            Assert.That(result.HumanoidScale.ReferenceHeight, Is.EqualTo(2).Within(0.0001f));
            Assert.That(result.HumanoidScale.ScaleFactor, Is.EqualTo(0.5f).Within(0.0001f));
            Assert.That(result.Warnings, Has.Some.Contains("on 6 vertices"));
            Assert.That(result.Warnings, Has.Some.Contains("15.0%"));
            Assert.That(result.Warnings, Has.Some.Contains("no normals"));
            Assert.That(result.Warnings, Has.Some.Contains("no tangents"));
            Assert.That(
                result.MaterialSummary!.MaskedMaterials.Single().MaterialName,
                Is.EqualTo("MaskMaterial"));
            Assert.That(
                result.MaterialSummary.MaskedMaterials.Single().AlphaCutoff,
                Is.EqualTo(0.4f).Within(0.0001f));
            Assert.That(
                result.MaterialSummary.SkippedSemantics.Select(item => item.Semantic),
                Does.Contain("Emissive"));
            Assert.That(
                result.MaterialSummary.SkippedSemantics.Select(item => item.Semantic),
                Does.Contain("Occlusion"));
            Assert.That(message, Does.Contain("Files written:"));
            Assert.That(message, Does.Contain("Automatic humanoid scaling:"));
            Assert.That(message, Does.Contain("Scale factor: 0.5"));
            Assert.That(message, Does.Contain("Skin weights"));
            Assert.That(message, Does.Contain("rebuilt from the triangles"));
            Assert.That(message, Does.Contain("MaskMaterial"));
            Assert.That(message, Does.Contain("emissive channel"));
            Assert.That(message, Does.Contain("occlusion channel"));
            Assert.That(result.SourceForward!.SourceDirection,
                Is.EqualTo("+Z (standard glTF)"));
            Assert.That(message, Does.Contain("Source front direction"));
            Assert.That(message, Does.Contain("+Z (standard glTF)"));
            Assert.That(message, Does.Contain("No extra rotation"));
            Assert.That(
                progressUpdates[0].Status,
                Is.EqualTo("Reading and validating the glTF/GLB file..."));
            Assert.That(progressUpdates[0].Total, Is.Zero);
            Assert.That(
                progressUpdates,
                Has.Some.Matches<ImportProgress>(update =>
                    update.Status.StartsWith(
                        "Converting meshes",
                        StringComparison.Ordinal) &&
                    update.Completed > 0 &&
                    update.Completed < update.Total &&
                    !string.IsNullOrWhiteSpace(update.Detail)));
            Assert.That(
                progressUpdates,
                Has.Some.Matches<ImportProgress>(update =>
                    update.Status.StartsWith(
                        "Converting animation",
                        StringComparison.Ordinal) &&
                    update.Completed > 0 &&
                    update.Completed < update.Total &&
                    update.Detail?.Contains(
                        "frame ",
                        StringComparison.Ordinal) == true));
            Assert.That(
                progressAtPackWrite?.Status,
                Is.EqualTo("Writing to the pack..."));
            Assert.That(progressAtPackWrite?.Total, Is.Zero);
            Assert.That(
                progressUpdates[^1].Status,
                Is.EqualTo("Import finished"));
            Assert.That(
                progressUpdates[^1].Completed,
                Is.EqualTo(progressUpdates[^1].Total));
        });
    }

    [Test]
    public void Import_BlendMaterial_SkipsMaterialsAndKeepsMeshSkeletonAndAnimations()
    {
        var fixtureDirectory = Path.Combine(
            Path.GetTempPath(),
            $"asseteditor-gltf-blend-{Guid.NewGuid():N}");
        Directory.CreateDirectory(fixtureDirectory);
        try
        {
            var sourcePath = Path.Combine(
                TestContext.CurrentContext.TestDirectory,
                "TestData",
                "Gltf",
                "external_full_workflow.glb");
            var glbPath = Path.Combine(fixtureDirectory, "blend-full-workflow.glb");
            var modelRoot = ModelRoot.Load(sourcePath);
            var blendMaterial = modelRoot.LogicalMaterials[0];
            blendMaterial.Alpha = SharpGLTF.Schema2.AlphaMode.BLEND;
            modelRoot.SaveGLB(glbPath);
            var destination = PackFileContainer.CreatePackFile("test");

            var result = CreateImporter(destination).Import(new GltfImporterSettings(
                glbPath,
                "models",
                destination,
                GameTypeEnum.Warhammer3,
                ImportMeshes: true,
                ImportMaterials: true,
                ConvertMaterialFromBlenderType: true,
                ConvertNormalTextureFromBlueToOrangeType: true,
                ImportAnimations: true,
                AnimationKeysPerSecond: 24,
                AutoDetectAnimationKeysPerSecond: true,
                AutoScaleHumanoid: true));

            const string rmvPath = @"models\blend-full-workflow.rigid_model_v2";
            const string skeletonPath =
                @"animations\skeletons\externalworkflowarmature.anim";
            var animationPaths = new[]
            {
                @"models\blend-full-workflow_move.anim",
                @"models\blend-full-workflow_nod.anim",
            };
            var rmv = result.Succeeded
                ? ModelFactory.Create().Load(
                    destination.FileList[rmvPath].DataSource.ReadData())
                : null;
            var message = ImporterCoreViewModel.BuildResultMessage(result);

            Assert.Multiple(() =>
            {
                Assert.That(result.Succeeded, Is.True,
                    string.Join(Environment.NewLine, result.Errors));
                Assert.That(result.OutputPaths, Does.Contain(rmvPath));
                Assert.That(result.OutputPaths, Does.Contain(skeletonPath));
                Assert.That(result.OutputPaths, Is.SupersetOf(animationPaths));
                Assert.That(destination.FileList.Keys,
                    Has.None.EndsWith(".dds").IgnoreCase);
                Assert.That(
                    rmv?.ModelList[0]
                        .SelectMany(model => model.Material.GetAllTextures()),
                    Is.Empty);
                Assert.That(result.MaterialSummary, Is.Null);
                Assert.That(result.Warnings, Has.Some.Contains("BLEND"));
                Assert.That(result.Warnings,
                    Has.Some.Contains(blendMaterial.Name));
                Assert.That(result.Warnings,
                    Has.Some.Contains("materials and textures"));
                Assert.That(result.Warnings,
                    Has.Some.Contains("other selected content"));
                Assert.That(message, Does.Contain("Warnings:"));
                Assert.That(message, Does.Contain(blendMaterial.Name));
                Assert.That(message, Does.Contain("materials and textures"));
            });
        }
        finally
        {
            Directory.Delete(fixtureDirectory, recursive: true);
        }
    }

    [TestCase(
        GltfSourceForwardDirection.PositiveX,
        MathF.PI / 2,
        "+X (Unreal/PSK)")]
    [TestCase(GltfSourceForwardDirection.NegativeX, -MathF.PI / 2, "-X")]
    [TestCase(GltfSourceForwardDirection.NegativeZ, MathF.PI, "-Z")]
    public void Import_NonPositiveZForward_RotatesEveryAssetAfterAutoScaleAndReportsDirection(
        GltfSourceForwardDirection sourceForwardDirection,
        float rotationRadians,
        string sourceDirectionLabel)
    {
        var glbPath = Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "TestData",
            "Gltf",
            "external_full_workflow.glb");
        var positiveZDestination = PackFileContainer.CreatePackFile("positive-z");
        var rotatedDestination = PackFileContainer.CreatePackFile("rotated");
        var positiveZResult = CreateImporter(positiveZDestination).Import(
            CreateFullWorkflowSettings(
                glbPath,
                positiveZDestination,
                GltfSourceForwardDirection.PositiveZ));
        var rotatedViewModel = new RmvToGltfImporterViewModel(
            CreateImporter(rotatedDestination))
        {
            SourceForwardDirection = sourceForwardDirection,
            AnimationKeysPerSecond = 24,
        };
        rotatedViewModel.Initialize(new PackFile(
            glbPath,
            new FileSystemSource(glbPath)));
        var rotatedResult = rotatedViewModel.Execute(
            new PackFile(glbPath, new FileSystemSource(glbPath)),
            "models",
            rotatedDestination,
            GameTypeEnum.Warhammer3,
            null,
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(positiveZResult.Succeeded, Is.True,
                string.Join(Environment.NewLine, positiveZResult.Errors));
            Assert.That(rotatedResult.Succeeded, Is.True,
                string.Join(Environment.NewLine, rotatedResult.Errors));
            Assert.That(
                rotatedResult.HumanoidScale!.SourceHeight,
                Is.EqualTo(positiveZResult.HumanoidScale!.SourceHeight).Within(0.0001f));
            Assert.That(
                rotatedResult.HumanoidScale.ReferenceHeight,
                Is.EqualTo(positiveZResult.HumanoidScale.ReferenceHeight).Within(0.0001f));
            Assert.That(
                rotatedResult.HumanoidScale.ScaleFactor,
                Is.EqualTo(positiveZResult.HumanoidScale.ScaleFactor).Within(0.0001f));
            Assert.That(
                rotatedResult.SourceForward!.SourceDirection,
                Is.EqualTo(sourceDirectionLabel));
            Assert.That(
                rotatedResult.SourceForward.Conversion,
                Does.Contain("game +Z"));
        });

        const string rmvPath = @"models\external_full_workflow.rigid_model_v2";
        const string skeletonPath =
            @"animations\skeletons\externalworkflowarmature.anim";
        var positiveZRmv = ModelFactory.Create().Load(
            positiveZDestination.FileList[rmvPath].DataSource.ReadData());
        var rotatedRmv = ModelFactory.Create().Load(
            rotatedDestination.FileList[rmvPath].DataSource.ReadData());
        var positiveZVertices = positiveZRmv.ModelList[0]
            .SelectMany(model => model.Mesh.VertexList)
            .ToList();
        var rotatedVertices = rotatedRmv.ModelList[0]
            .SelectMany(model => model.Mesh.VertexList)
            .ToList();
        Assert.That(rotatedVertices, Has.Count.EqualTo(positiveZVertices.Count));
        for (var vertexIndex = 0; vertexIndex < positiveZVertices.Count; vertexIndex++)
        {
            AssertVectorRotated(
                ToNumerics(positiveZVertices[vertexIndex].Position),
                ToNumerics(rotatedVertices[vertexIndex].Position),
                rotationRadians);
            AssertVectorRotated(
                ToNumerics(positiveZVertices[vertexIndex].Normal),
                ToNumerics(rotatedVertices[vertexIndex].Normal),
                rotationRadians,
                0.01f);
        }

        var positiveZSkeleton = AnimationFile.Create(
            positiveZDestination.FileList[skeletonPath]);
        var rotatedSkeleton = AnimationFile.Create(
            rotatedDestination.FileList[skeletonPath]);
        AssertAnimationRotated(
            positiveZSkeleton,
            rotatedSkeleton,
            rotationRadians);

        foreach (var animationPath in new[]
                 {
                     @"models\external_full_workflow_move.anim",
                     @"models\external_full_workflow_nod.anim",
                 })
        {
            var positiveZAnimation = AnimationFile.Create(
                positiveZDestination.FileList[animationPath]);
            var rotatedAnimation = AnimationFile.Create(
                rotatedDestination.FileList[animationPath]);
            AssertAnimationRotated(
                positiveZAnimation,
                rotatedAnimation,
                rotationRadians);
        }

        Assert.That(
            ImporterCoreViewModel.BuildResultMessage(rotatedResult),
            Does.Contain("Source front direction")
                .And.Contain(sourceDirectionLabel)
                .And.Contain("game +Z"));
    }

    [Test]
    public void Import_SkinnedMeshUnderScaledAncestor_KeepsMeshSkeletonAndRootInSameSpace()
    {
        var glbPath = CreateScaledAncestorGlb();
        try
        {
            var destination = PackFileContainer.CreatePackFile("test");
            var result = CreateImporter(destination).Import(new GltfImporterSettings(
                glbPath,
                "models",
                destination,
                GameTypeEnum.Warhammer3,
                ImportMeshes: true,
                ImportMaterials: false,
                ConvertMaterialFromBlenderType: false,
                ConvertNormalTextureFromBlueToOrangeType: false,
                ImportAnimations: true,
                AnimationKeysPerSecond: 1,
                AutoDetectAnimationKeysPerSecond: false,
                AutoScaleHumanoid: true));

            Assert.That(
                result.Succeeded,
                Is.True,
                string.Join(Environment.NewLine, result.Errors));

            var baseName = Path.GetFileNameWithoutExtension(glbPath).ToLowerInvariant();
            var rmv = ModelFactory.Create().Load(
                destination.FileList[$@"models\{baseName}.rigid_model_v2"]
                    .DataSource.ReadData());
            var skeleton = AnimationFile.Create(
                destination.FileList[@"animations\skeletons\scaledarmature.anim"]);
            var animation = AnimationFile.Create(
                destination.FileList[$@"models\{baseName}_root_move.anim"]);
            var vertices = rmv.ModelList[0]
                .SelectMany(model => model.Mesh.VertexList)
                .ToList();
            var modelHeight = vertices.Max(vertex => vertex.Position.Y) -
                              vertices.Min(vertex => vertex.Position.Y);
            var rootIndex = Array.FindIndex(
                skeleton.Bones,
                bone => bone.Name == "root");
            var headIndex = Array.FindIndex(
                skeleton.Bones,
                bone => bone.Name == "head");

            Assert.Multiple(() =>
            {
                Assert.That(result.HumanoidScale!.SourceHeight, Is.EqualTo(1).Within(0.0001f));
                Assert.That(result.HumanoidScale.ReferenceHeight, Is.EqualTo(2).Within(0.0001f));
                Assert.That(result.HumanoidScale.ScaleFactor, Is.EqualTo(2).Within(0.0001f));
                Assert.That(modelHeight, Is.EqualTo(2).Within(0.0001f));
                Assert.That(
                    skeleton.AnimationParts[0].DynamicFrames[0]
                        .Transforms[headIndex].Y,
                    Is.EqualTo(2).Within(0.0001f));
                Assert.That(
                    animation.AnimationParts[0].DynamicFrames[^1]
                        .Transforms[rootIndex].Y,
                    Is.EqualTo(0.6f).Within(0.0001f));
            });
        }
        finally
        {
            File.Delete(glbPath);
        }
    }

    [Test]
    public void Import_SkinnedMeshWithSingularAncestor_ReturnsReadableErrorAndLeavesPackUnchanged()
    {
        var fixtureDirectory = CreateSingularAncestorGltf();
        var gltfPath = Path.Combine(fixtureDirectory, "singular_ancestor.gltf");
        try
        {
            const string existingPath = @"models\existing.bin";
            var destination = PackFileContainer.CreatePackFile("test");
            var existingFile = new PackFile(
                "existing.bin",
                new MemorySource([7, 8, 9]));
            destination.FileList[existingPath] = existingFile;

            var result = CreateImporter(destination).Import(new GltfImporterSettings(
                gltfPath,
                "models",
                destination,
                GameTypeEnum.Warhammer3,
                ImportMeshes: true,
                ImportMaterials: false,
                ConvertMaterialFromBlenderType: false,
                ConvertNormalTextureFromBlueToOrangeType: false,
                ImportAnimations: false,
                AnimationKeysPerSecond: 20,
                AutoScaleHumanoid: false));

            Assert.Multiple(() =>
            {
                Assert.That(result.Succeeded, Is.False);
                Assert.That(result.Errors, Has.Some.Contains("can't be inverted"));
                Assert.That(destination.FileList, Has.Count.EqualTo(1));
                Assert.That(destination.FileList[existingPath], Is.SameAs(existingFile));
                Assert.That(
                    existingFile.DataSource.ReadData(),
                    Is.EqualTo(new byte[] { 7, 8, 9 }));
            });
        }
        finally
        {
            Directory.Delete(fixtureDirectory, recursive: true);
        }
    }

    [Test]
    public async Task ImportWindowSelection_OfficialBlenderGlb_UsesCompleteWorkflowDefaults()
    {
        var glbPath = Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "TestData",
            "Gltf",
            "external_full_workflow.glb");
        var destination = PackFileContainer.CreatePackFile("test");
        var importerViewModel = new RmvToGltfImporterViewModel(
            CreateImporter(destination));
        var windowViewModel = new ImporterCoreViewModel(
            [importerViewModel],
            new ApplicationSettingsService(GameTypeEnum.Warhammer3));

        windowViewModel.Initialize(destination, "models", glbPath);

        Assert.Multiple(() =>
        {
            Assert.That(windowViewModel.SelectedImporter, Is.SameAs(importerViewModel));
            Assert.That(windowViewModel.PossibleImporters, Has.Count.EqualTo(1));
            Assert.That(importerViewModel.ImportMeshes, Is.True);
            Assert.That(importerViewModel.ImportMaterials, Is.True);
            Assert.That(importerViewModel.ImportAnimations, Is.True);
            Assert.That(importerViewModel.AutoScaleHumanoid, Is.True);
            Assert.That(
                importerViewModel.NewSkeletonName,
                Is.EqualTo("ExternalWorkflowArmature"));
        });

        var result = await windowViewModel.ImportAsync();

        Assert.Multiple(() =>
        {
            Assert.That(
                result.Succeeded,
                Is.True,
                string.Join(Environment.NewLine, result.Errors));
            Assert.That(result.OutputPaths, Is.EquivalentTo(destination.FileList.Keys));
            Assert.That(result.OutputPaths, Has.Some.EndsWith(".rigid_model_v2"));
            Assert.That(result.OutputPaths.Count(path => path.EndsWith(".anim")),
                Is.EqualTo(3));
            Assert.That(result.OutputPaths, Has.Some.EndsWith(".dds"));
        });
    }

    private static GltfImporter CreateImporter(
        PackFileContainer destination,
        Action? beforePackWrite = null)
    {
        var innerPackFileService = PackFileSerivceTestHelper.Create(TestData.InputPack);
        var referenceContainer = CreateReferenceContainer();
        var packFileService = new Mock<IPackFileService>();
        packFileService
            .Setup(service => service.GetAllPackfileContainers())
            .Returns([referenceContainer]);
        packFileService
            .Setup(service => service.AddFilesToPack(
                destination,
                It.IsAny<List<NewPackFileEntry>>()))
            .Callback<IPackFileContainer, List<NewPackFileEntry>>((container, entries) =>
                {
                    beforePackWrite?.Invoke();
                    innerPackFileService.AddFilesToPack(
                        container,
                        entries);
                });
        return new GltfImporter(
            packFileService.Object,
            Mock.Of<ISkeletonAnimationLookUpHelper>(),
            new RmvMaterialBuilder());
    }

    private sealed class InlineProgress<T>(Action<T> report) : IProgress<T>
    {
        public void Report(T value) => report(value);
    }

    private static GltfImporterSettings CreateFullWorkflowSettings(
        string glbPath,
        PackFileContainer destination,
        GltfSourceForwardDirection sourceForwardDirection) =>
        new(
            glbPath,
            "models",
            destination,
            GameTypeEnum.Warhammer3,
            ImportMeshes: true,
            ImportMaterials: true,
            ConvertMaterialFromBlenderType: true,
            ConvertNormalTextureFromBlueToOrangeType: true,
            ImportAnimations: true,
            AnimationKeysPerSecond: 24,
            AutoDetectAnimationKeysPerSecond: true,
            AutoScaleHumanoid: true,
            SourceForwardDirection: sourceForwardDirection);

    private static void AssertAnimationRotated(
        AnimationFile positiveZ,
        AnimationFile rotated,
        float rotationRadians)
    {
        Assert.Multiple(() =>
        {
            Assert.That(rotated.Bones.Select(bone => bone.Name),
                Is.EqualTo(positiveZ.Bones.Select(bone => bone.Name)));
            Assert.That(rotated.AnimationParts,
                Has.Count.EqualTo(positiveZ.AnimationParts.Count));
        });
        for (var partIndex = 0; partIndex < positiveZ.AnimationParts.Count; partIndex++)
        {
            var positiveZPart = positiveZ.AnimationParts[partIndex];
            var rotatedPart = rotated.AnimationParts[partIndex];
            if (positiveZPart.StaticFrame != null)
            {
                Assert.That(rotatedPart.StaticFrame, Is.Not.Null);
                AssertFrameRotated(
                    positiveZPart.StaticFrame,
                    rotatedPart.StaticFrame!,
                    rotationRadians);
            }

            Assert.That(rotatedPart.DynamicFrames,
                Has.Count.EqualTo(positiveZPart.DynamicFrames.Count));
            for (var frameIndex = 0;
                 frameIndex < positiveZPart.DynamicFrames.Count;
                 frameIndex++)
            {
                AssertFrameRotated(
                    positiveZPart.DynamicFrames[frameIndex],
                    rotatedPart.DynamicFrames[frameIndex],
                    rotationRadians);
            }
        }
    }

    private static void AssertFrameRotated(
        AnimationFile.Frame positiveZ,
        AnimationFile.Frame rotated,
        float rotationRadians)
    {
        Assert.Multiple(() =>
        {
            Assert.That(rotated.Transforms,
                Has.Count.EqualTo(positiveZ.Transforms.Count));
            Assert.That(rotated.Quaternion,
                Has.Count.EqualTo(positiveZ.Quaternion.Count));
        });
        for (var transformIndex = 0;
             transformIndex < positiveZ.Transforms.Count;
             transformIndex++)
        {
            AssertVectorRotated(
                ToNumerics(positiveZ.Transforms[transformIndex]),
                ToNumerics(rotated.Transforms[transformIndex]),
                rotationRadians);
        }
        for (var rotationIndex = 0;
             rotationIndex < positiveZ.Quaternion.Count;
             rotationIndex++)
        {
            var expected = Rotate(
                ToNumerics(positiveZ.Quaternion[rotationIndex]),
                rotationRadians);
            var actual = ToNumerics(rotated.Quaternion[rotationIndex]);
            Assert.That(
                Math.Abs(Numerics.Quaternion.Dot(expected, actual)),
                Is.EqualTo(1).Within(0.0001f));
        }
    }

    private static void AssertVectorRotated(
        Numerics.Vector3 positiveZ,
        Numerics.Vector3 rotated,
        float rotationRadians,
        float tolerance = 0.0001f) =>
        Assert.That(
            Numerics.Vector3.Distance(
                rotated,
                Numerics.Vector3.Transform(
                    positiveZ,
                    Numerics.Matrix4x4.CreateRotationY(rotationRadians))),
            Is.LessThanOrEqualTo(tolerance));

    private static Numerics.Quaternion Rotate(
        Numerics.Quaternion positiveZ,
        float rotationRadians)
    {
        var basis = Numerics.Matrix4x4.CreateRotationY(rotationRadians);
        var converted = Numerics.Matrix4x4.Transpose(basis) *
                        Numerics.Matrix4x4.CreateFromQuaternion(positiveZ) *
                        basis;
        Assert.That(Numerics.Matrix4x4.Decompose(
            converted,
            out _,
            out var rotation,
            out _), Is.True);
        return Numerics.Quaternion.Normalize(rotation);
    }

    private static Numerics.Vector3 ToNumerics(
        Microsoft.Xna.Framework.Vector4 value) =>
        new(value.X, value.Y, value.Z);

    private static Numerics.Vector3 ToNumerics(
        Microsoft.Xna.Framework.Vector3 value) =>
        new(value.X, value.Y, value.Z);

    private static Numerics.Vector3 ToNumerics(RmvVector3 value) =>
        new(value.X, value.Y, value.Z);

    private static Numerics.Quaternion ToNumerics(RmvVector4 value) =>
        new(value.X, value.Y, value.Z, value.W);


    private static PackFileContainer CreateReferenceContainer()
    {
        var container = PackFileContainer.CreatePackFile("ca");
        container.IsCaPackFile = true;
        container.FileList[HumanoidSkeletonPath] = new PackFile(
            "humanoid01.anim",
            new MemorySource(AnimationFile.ConvertToBytes(
                CreateHumanoidSkeleton())));
        return container;
    }

    private static AnimationFile CreateHumanoidSkeleton()
    {
        var frame = new AnimationFile.Frame
        {
            Transforms =
            [
                new RmvVector3(0, 0, 0),
                new RmvVector3(-0.25f, 0, 0),
                new RmvVector3(0.25f, 0, 0),
                new RmvVector3(0, 2, 0),
            ],
            Quaternion = Enumerable.Repeat(
                new RmvVector4(0, 0, 0, 1),
                4).ToList(),
        };
        var part = new AnimationFile.AnimationPart
        {
            DynamicFrames = [frame, CloneFrame(frame)],
        };
        for (var boneIndex = 0; boneIndex < 4; boneIndex++)
        {
            part.TranslationMappings.Add(
                new AnimationFile.AnimationBoneMapping(boneIndex));
            part.RotationMappings.Add(
                new AnimationFile.AnimationBoneMapping(boneIndex));
        }

        return new AnimationFile
        {
            Header = new AnimationFile.AnimationHeader
            {
                Version = 7,
                SkeletonName = "humanoid01",
                AnimationTotalPlayTimeInSec = 0.1f,
            },
            Bones =
            [
                new AnimationFile.BoneInfo
                    { Id = 0, ParentId = -1, Name = "root" },
                new AnimationFile.BoneInfo
                    { Id = 1, ParentId = 0, Name = "toe_left_0" },
                new AnimationFile.BoneInfo
                    { Id = 2, ParentId = 0, Name = "toe_right_0" },
                new AnimationFile.BoneInfo
                    { Id = 3, ParentId = 0, Name = "head" },
            ],
            AnimationParts = [part],
        };
    }

    private static string CreateScaledAncestorGlb()
    {
        const float ancestorScale = 0.003f;
        const float sourceHeight = 1;
        var modelRoot = ModelRoot.CreateModel();
        var scene = modelRoot.UseScene("default");
        var armature = scene.CreateNode("ScaledArmature");
        armature.LocalMatrix = Numerics.Matrix4x4.CreateScale(ancestorScale);
        var root = armature.CreateNode("root");
        var leftFoot = root.CreateNode("toe_left_0");
        leftFoot.LocalMatrix = Numerics.Matrix4x4.CreateTranslation(-25, 0, 0);
        var rightFoot = root.CreateNode("toe_right_0");
        rightFoot.LocalMatrix = Numerics.Matrix4x4.CreateTranslation(25, 0, 0);
        var head = root.CreateNode("head");
        head.LocalMatrix = Numerics.Matrix4x4.CreateTranslation(
            0,
            sourceHeight / ancestorScale,
            0);

        var geometry = new MeshBuilder<
            VertexPositionNormalTangent,
            VertexTexture1,
            VertexJoints4>("scaled_mesh");
        geometry.UsePrimitive(
                new MaterialBuilder("material").WithMetallicRoughness())
            .AddTriangle(
                CreateScaledAncestorVertex(0, 0),
                CreateScaledAncestorVertex(1, 0),
                CreateScaledAncestorVertex(0, sourceHeight));
        var mesh = modelRoot.CreateMesh(geometry);
        Assert.That(Numerics.Matrix4x4.Invert(
            root.WorldMatrix,
            out var rootInverseBind), Is.True);
        Assert.That(Numerics.Matrix4x4.Invert(
            leftFoot.WorldMatrix,
            out var leftFootInverseBind), Is.True);
        Assert.That(Numerics.Matrix4x4.Invert(
            rightFoot.WorldMatrix,
            out var rightFootInverseBind), Is.True);
        Assert.That(Numerics.Matrix4x4.Invert(
            head.WorldMatrix,
            out var headInverseBind), Is.True);
        armature.CreateNode("mesh_node").WithSkinnedMesh(
            mesh,
            (root, rootInverseBind),
            (leftFoot, leftFootInverseBind),
            (rightFoot, rightFootInverseBind),
            (head, headInverseBind));
        modelRoot.LogicalSkins.Single().Name = "ScaledArmature";
        modelRoot.CreateAnimation("Root Move").CreateTranslationChannel(
            root,
            new Dictionary<float, Numerics.Vector3>
            {
                [0] = Numerics.Vector3.Zero,
                [1] = new Numerics.Vector3(0, 100, 0),
            });

        var path = Path.Combine(
            Path.GetTempPath(),
            $"scaled_ancestor_{Guid.NewGuid():N}.glb");
        modelRoot.SaveGLB(path);
        return path;
    }

    private static string CreateSingularAncestorGltf()
    {
        var modelRoot = ModelRoot.CreateModel();
        var scene = modelRoot.UseScene("default");
        var armature = scene.CreateNode("InvalidArmature");
        armature.LocalMatrix = Numerics.Matrix4x4.CreateScale(2);
        var root = armature.CreateNode("root");
        var geometry = new MeshBuilder<
            VertexPositionNormalTangent,
            VertexTexture1,
            VertexJoints4>("invalid_mesh");
        geometry.UsePrimitive(
                new MaterialBuilder("material").WithMetallicRoughness())
            .AddTriangle(
                CreateScaledAncestorVertex(0, 0),
                CreateScaledAncestorVertex(1, 0),
                CreateScaledAncestorVertex(0, 1));
        var mesh = modelRoot.CreateMesh(geometry);
        Assert.That(Numerics.Matrix4x4.Invert(
            root.WorldMatrix,
            out var rootInverseBind), Is.True);
        armature.CreateNode("mesh_node").WithSkinnedMesh(
            mesh,
            (root, rootInverseBind));
        modelRoot.LogicalSkins.Single().Name = "InvalidArmature";

        var directory = Path.Combine(
            Path.GetTempPath(),
            $"singular_ancestor_{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "singular_ancestor.gltf");
        modelRoot.SaveGLTF(path);

        var json = System.Text.Json.Nodes.JsonNode.Parse(
            File.ReadAllText(path))!;
        var armatureJson = json["nodes"]![armature.LogicalIndex]!.AsObject();
        armatureJson.Remove("matrix");
        armatureJson.Remove("rotation");
        armatureJson.Remove("translation");
        armatureJson["scale"] = new System.Text.Json.Nodes.JsonArray(0, 1, 1);
        File.WriteAllText(path, json.ToJsonString());
        return directory;
    }

    private static VertexBuilder<
        VertexPositionNormalTangent,
        VertexTexture1,
        VertexJoints4> CreateScaledAncestorVertex(float x, float y)
    {
        var vertex = new VertexBuilder<
            VertexPositionNormalTangent,
            VertexTexture1,
            VertexJoints4>();
        vertex.Geometry.Position = new Numerics.Vector3(x, y, 0);
        vertex.Geometry.Normal = Numerics.Vector3.UnitZ;
        vertex.Geometry.Tangent = new Numerics.Vector4(1, 0, 0, 1);
        vertex.Material.TexCoord = new Numerics.Vector2(x, y);
        vertex.Skinning.SetBindings((0, 1), (0, 0), (0, 0), (0, 0));
        return vertex;
    }

    private static AnimationFile.Frame CloneFrame(AnimationFile.Frame frame) => new()
    {
        Transforms = frame.Transforms.ToList(),
        Quaternion = frame.Quaternion.ToList(),
    };
}
