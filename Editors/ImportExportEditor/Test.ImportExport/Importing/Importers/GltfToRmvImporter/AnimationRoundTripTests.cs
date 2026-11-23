using Editors.ImportExport.Exporting.Exporters.RmvToGltf;
using Editors.ImportExport.Exporting.Exporters.RmvToGltf.Helpers;
using Editors.ImportExport.Importing.Importers.GltfToRmv;
using Editors.ImportExport.Importing.Importers.GltfToRmv.Helper;
using GameWorld.Core.Animation;
using GameWorld.Core.Services;
using Moq;
using Shared.Core.PackFiles.Models;
using Shared.Core.PackFiles.Models.FileSources;
using Shared.Core.PackFiles.Models.Containers;
using Shared.Core.Services;
using Shared.Core.Settings;
using Shared.GameFormats.Animation;
using Shared.GameFormats.RigidModel;
using Shared.GameFormats.RigidModel.Transforms;
using Shared.TestUtility;
using SharpGLTF.Geometry;
using SharpGLTF.Geometry.VertexTypes;
using SharpGLTF.Materials;
using SharpGLTF.Schema2;
using Test.ImportExport.Exporting.Exporters.RmvToGlft;
using Test.TestingUtility.TestUtility;
using Xna = Microsoft.Xna.Framework;

namespace Test.ImportExport.Importing.Importers.GltfImporterTest;

public class AnimationRoundTripTests
{
    private const string SkeletonPath = @"animations\skeletons\humanoid01.anim";
    private const string AnimationPath = @"animations\battle\humanoid01\2handed_hammer\stand\hu1_2hh_stand_idle_01.anim";
    private const string ModelPath = @"variantmeshes\wh_variantmodels\hu1\emp\emp_karl_franz\emp_karl_franz.rigid_model_v2";


    [Test]
    public void ExportThenImport_RealHumanoidAnimation_PreservesLocalBoneTransforms()
    {
        var packFileService = PackFileSerivceTestHelper.Create(
            PathHelper.GetDataFolder("Data\\Karl_and_celestialgeneral_Pack"));
        var skeletonPackFile = packFileService.FindFile(SkeletonPath);
        var animationPackFile = packFileService.FindFile(AnimationPath);
        Assert.That(skeletonPackFile, Is.Not.Null);
        Assert.That(animationPackFile, Is.Not.Null);

        var skeleton = AnimationFile.Create(skeletonPackFile!);
        var originalAnimation = AnimationFile.Create(animationPackFile!);
        var modelRoot = ModelRoot.CreateModel();
        var exportSettings = new RmvToGltfExporterSettings(
            new PackFile("model.rigid_model_v2", new MemorySource([])),
            [animationPackFile!],
            "roundtrip.gltf",
            false,
            false,
            false,
            true,
            true);
        var gltfSkeleton = new GltfSkeletonBuilder(packFileService)
            .CreateSkeleton(skeleton, modelRoot, exportSettings);
        new GltfAnimationBuilder(packFileService).Build(
            skeleton,
            exportSettings,
            gltfSkeleton,
            modelRoot);
        AddMinimalSkinnedMesh(modelRoot, gltfSkeleton);

        var importedAnimation = AnimationBuilder.Build(
            new AnimationBuilderSettings(
                modelRoot,
                skeleton.Header.SkeletonName,
                originalAnimation.Header.FrameRate,
                PackFileContainer.CreatePackFile("roundtrip"),
                "animations"),
            skeleton,
            modelRoot.LogicalAnimations.Single());

        AssertAnimationsMatch(originalAnimation, importedAnimation, skeleton);
    }

    [Test]
    public void ExportThenImport_RealHumanoidModel_PreservesNumericDataAndDoesNotCopySkeleton()
    {
        var packFileService = PackFileSerivceTestHelper.Create(
            PathHelper.GetDataFolder(@"Data\Karl_and_celestialgeneral_Pack"));
        var modelPackFile = packFileService.FindFile(ModelPath);
        var skeletonPackFile = packFileService.FindFile(SkeletonPath);
        Assert.That(modelPackFile, Is.Not.Null);
        Assert.That(skeletonPackFile, Is.Not.Null);

        var skeleton = AnimationFile.Create(skeletonPackFile!);
        var originalModel = ModelFactory.Create().Load(
            modelPackFile!.DataSource.ReadData());
        var skeletonLookup = new Mock<ISkeletonAnimationLookUpHelper>();
        skeletonLookup
            .Setup(lookup => lookup.GetSkeletonFileFromName("humanoid01"))
            .Returns(skeleton);
        var textureHandler = new Mock<IGltfTextureHandler>();
        textureHandler
            .Setup(handler => handler.HandleTextures(
                It.IsAny<ResolvedModelAsset>(),
                It.IsAny<RmvToGltfExporterSettings>(),
                It.IsAny<GltfTextureExportSession>()))
            .Returns([]);
        var sceneSaver = new TestGltfSceneSaver();
        var glbPath = Path.Combine(
            Path.GetTempPath(),
            $"original_humanoid_roundtrip_{Guid.NewGuid():N}.glb");

        try
        {
            var exportSettings = new RmvToGltfExporterSettings(
                modelPackFile,
                [],
                glbPath,
                ExportMaterials: false,
                ConvertMaterialTextureToBlender: false,
                ConvertNormalTextureToBlue: false,
                ExportAnimations: false,
                MirrorMesh: true);
            var exporter = new RmvToGltfExporter(
                sceneSaver,
                new GltfMeshBuilder(),
                textureHandler.Object,
                new GltfSkeletonBuilder(packFileService),
                new GltfAnimationBuilder(packFileService),
                skeletonLookup.Object);

            exporter.Export(exportSettings);
            sceneSaver.ModelRoot!.SaveGLB(glbPath);

            var destination = PackFileContainer.CreatePackFile("roundtrip");
            var importer = new GltfImporter(
                packFileService,
                skeletonLookup.Object,
                new RmvMaterialBuilder());
            var result = importer.Import(new GltfImporterSettings(
                glbPath,
                "models",
                destination,
                GameTypeEnum.Warhammer3,
                ImportMeshes: true,
                ImportMaterials: false,
                ConvertMaterialFromBlenderType: false,
                ConvertNormalTextureFromBlueToOrangeType: false,
                ImportAnimations: false,
                AnimationKeysPerSecond: 20,
                AutoDetectAnimationKeysPerSecond: false,
                AutoScaleHumanoid: false));

            Assert.That(
                result.Succeeded,
                Is.True,
                result.Exception?.ToString() ??
                string.Join(Environment.NewLine, result.Errors));
            var importedModelPath = result.OutputPaths.Single(path =>
                path.EndsWith(".rigid_model_v2", StringComparison.OrdinalIgnoreCase));
            var importedModel = ModelFactory.Create().Load(
                destination.FileList[importedModelPath].DataSource.ReadData());
            var originalBounds = GetLod0Bounds(originalModel);
            var importedBounds = GetLod0Bounds(importedModel);

            Assert.Multiple(() =>
            {
                Assert.That(
                    importedModel.ModelList[0].Sum(model => model.Mesh.IndexList.Length),
                    Is.EqualTo(originalModel.ModelList[0]
                        .Sum(model => model.Mesh.IndexList.Length)));
                Assert.That(
                    Xna.Vector3.Distance(originalBounds.Min, importedBounds.Min),
                    Is.LessThan(0.001f));
                Assert.That(
                    Xna.Vector3.Distance(originalBounds.Max, importedBounds.Max),
                    Is.LessThan(0.001f));
                Assert.That(
                    result.OutputPaths,
                    Does.Not.Contain(SkeletonPath));
            });
        }
        finally
        {
            File.Delete(glbPath);
        }
    }

    [Test]
    public void ExportThenImport_NonUnitBindQuaternion_DoesNotCreateBoneScale()
    {
        var packFileService = PackFileSerivceTestHelper.Create(
            PathHelper.GetDataFolder("Data\\Karl_and_celestialgeneral_Pack"));
        var skeleton = CreateSkeletonWithNonUnitBindQuaternion();
        var modelRoot = ModelRoot.CreateModel();
        var exportSettings = new RmvToGltfExporterSettings(
            new PackFile("model.rigid_model_v2", new MemorySource([])),
            [],
            "roundtrip.gltf",
            false,
            false,
            false,
            false,
            true);
        var gltfSkeleton = new GltfSkeletonBuilder(packFileService)
            .CreateSkeleton(skeleton, modelRoot, exportSettings);
        AddMinimalSkinnedMesh(modelRoot, gltfSkeleton);
        var glbPath = Path.Combine(
            Path.GetTempPath(),
            $"non_unit_bind_roundtrip_{Guid.NewGuid():N}.glb");

        try
        {
            modelRoot.SaveGLB(glbPath);
            var destination = PackFileContainer.CreatePackFile("roundtrip");
            var importer = new GltfImporter(
                packFileService,
                Mock.Of<ISkeletonAnimationLookUpHelper>(),
                new RmvMaterialBuilder());

            var result = importer.Import(new GltfImporterSettings(
                glbPath,
                "animations",
                destination,
                GameTypeEnum.Warhammer3,
                ImportMeshes: false,
                ImportMaterials: false,
                ConvertMaterialFromBlenderType: false,
                ConvertNormalTextureFromBlueToOrangeType: false,
                ImportAnimations: true,
                AnimationKeysPerSecond: 20,
                AutoDetectAnimationKeysPerSecond: false,
                AutoScaleHumanoid: false));

            Assert.That(
                result.Succeeded,
                Is.True,
                result.Exception?.ToString() ??
                string.Join(Environment.NewLine, result.Errors));
            var importedSkeleton = AnimationFile.Create(
                destination.FileList[@"animations\skeletons\non_unit_bind.anim"]);
            Assert.That(importedSkeleton.Bones, Has.Length.EqualTo(skeleton.Bones.Length));
        }
        finally
        {
            File.Delete(glbPath);
        }
    }

    [Test]
    public void ExportThenImport_NonUnitAnimationQuaternion_DoesNotCreateBoneScale()
    {
        var packFileService = PackFileSerivceTestHelper.Create(
            PathHelper.GetDataFolder("Data\\Karl_and_celestialgeneral_Pack"));
        var skeleton = CreateSingleBoneAnimation(1.0f);
        var animation = CreateSingleBoneAnimation(0.99995f);
        var animationPackFile = new PackFile(
            "non_unit_animation.anim",
            new MemorySource(AnimationFile.ConvertToBytes(animation)));
        var modelRoot = ModelRoot.CreateModel();
        var exportSettings = new RmvToGltfExporterSettings(
            new PackFile("model.rigid_model_v2", new MemorySource([])),
            [animationPackFile],
            "roundtrip.gltf",
            false,
            false,
            false,
            true,
            true);
        var gltfSkeleton = new GltfSkeletonBuilder(packFileService)
            .CreateSkeleton(skeleton, modelRoot, exportSettings);
        new GltfAnimationBuilder(packFileService).Build(
            skeleton,
            exportSettings,
            gltfSkeleton,
            modelRoot);

        var gltfAnimation = modelRoot.LogicalAnimations.Single();
        var exportedRotation = gltfSkeleton.Data[0].Item1
            .GetLocalTransform(gltfAnimation, 0)
            .Rotation;
        Assert.That(exportedRotation.Length(), Is.EqualTo(1).Within(0.000001f));

        var importedAnimation = AnimationBuilder.Build(
            new AnimationBuilderSettings(
                modelRoot,
                skeleton.Header.SkeletonName,
                animation.Header.FrameRate,
                PackFileContainer.CreatePackFile("roundtrip"),
                "animations"),
            skeleton,
            gltfAnimation);

        Assert.That(
            importedAnimation.AnimationParts[0].DynamicFrames,
            Is.Not.Empty);
    }

    [Test]
    public void ExportThenImport_ClipNamedAfterAnimFile_DoesNotDoubleTheExtension()
    {
        var packFileService = PackFileSerivceTestHelper.Create(
            PathHelper.GetDataFolder("Data\\Karl_and_celestialgeneral_Pack"));
        var skeleton = CreateSingleBoneAnimation(1.0f);
        var animationBytes = AnimationFile.ConvertToBytes(CreateSingleBoneAnimation(1.0f));
        var modelRoot = ModelRoot.CreateModel();
        var exportSettings = new RmvToGltfExporterSettings(
            new PackFile("model.rigid_model_v2", new MemorySource([])),
            [new PackFile("idle.anim", new MemorySource(animationBytes))],
            "roundtrip.gltf",
            false,
            false,
            false,
            true,
            true);
        var gltfSkeleton = new GltfSkeletonBuilder(packFileService)
            .CreateSkeleton(skeleton, modelRoot, exportSettings);
        new GltfAnimationBuilder(packFileService).Build(skeleton, exportSettings, gltfSkeleton, modelRoot);
        AddMinimalSkinnedMesh(modelRoot, gltfSkeleton);
        var glbPath = Path.Combine(Path.GetTempPath(), $"clip_name_roundtrip_{Guid.NewGuid():N}.glb");

        try
        {
            modelRoot.SaveGLB(glbPath);
            var destination = PackFileContainer.CreatePackFile("roundtrip");
            var skeletonLookup = new Mock<ISkeletonAnimationLookUpHelper>();
            skeletonLookup
                .Setup(lookup => lookup.GetSkeletonFileFromName("single_bone"))
                .Returns(skeleton);
            var importer = new GltfImporter(packFileService, skeletonLookup.Object, new RmvMaterialBuilder());

            var result = importer.Import(new GltfImporterSettings(
                glbPath,
                "animations",
                destination,
                GameTypeEnum.Warhammer3,
                ImportMeshes: false,
                ImportMaterials: false,
                ConvertMaterialFromBlenderType: false,
                ConvertNormalTextureFromBlueToOrangeType: false,
                ImportAnimations: true,
                AnimationKeysPerSecond: 20,
                AutoDetectAnimationKeysPerSecond: false,
                AutoScaleHumanoid: false));

            var baseName = Path.GetFileNameWithoutExtension(glbPath).ToLowerInvariant();
            Assert.That(result.Succeeded, Is.True, string.Join(Environment.NewLine, result.Errors));
            Assert.That(modelRoot.LogicalAnimations.Single().Name, Is.EqualTo("idle.anim"));
            Assert.That(result.OutputPaths, Is.EqualTo(new[] { $@"animations\{baseName}_idle.anim" }));
        }
        finally
        {
            File.Delete(glbPath);
        }
    }

    private static void AddMinimalSkinnedMesh(
        ModelRoot modelRoot,
        ProcessedGltfSkeleton skeleton)
    {
        var geometry = new MeshBuilder<
            VertexPositionNormalTangent,
            VertexTexture1,
            VertexJoints4>("minimal_skinned_mesh");
        var primitive = geometry.UsePrimitive(
            new MaterialBuilder("material").WithMetallicRoughness());
        primitive.AddTriangle(
            CreateSkinnedVertex(0, 0),
            CreateSkinnedVertex(1, 0),
            CreateSkinnedVertex(0, 1));
        modelRoot.DefaultScene!.CreateNode("minimal_skinned_mesh")
            .WithSkinnedMesh(
                modelRoot.CreateMesh(geometry),
                skeleton.Data.ToArray());
    }

    private static VertexBuilder<
        VertexPositionNormalTangent,
        VertexTexture1,
        VertexJoints4> CreateSkinnedVertex(float x, float y)
    {
        var vertex = new VertexBuilder<
            VertexPositionNormalTangent,
            VertexTexture1,
            VertexJoints4>();
        vertex.Geometry.Position = new System.Numerics.Vector3(x, y, 0);
        vertex.Geometry.Normal = System.Numerics.Vector3.UnitZ;
        vertex.Geometry.Tangent = new System.Numerics.Vector4(1, 0, 0, 1);
        vertex.Material.TexCoord = new System.Numerics.Vector2(x, y);
        vertex.Skinning.SetBindings((0, 1), (0, 0), (0, 0), (0, 0));
        return vertex;
    }

    private static AnimationFile CreateSkeletonWithNonUnitBindQuaternion()
    {
        var rotation = Xna.Quaternion.CreateFromAxisAngle(
            Xna.Vector3.UnitZ,
            MathF.PI);
        const int boneCount = 185;
        const float storedQuaternionLength = 0.99995f;
        var frame = new AnimationFile.Frame();
        var bones = new AnimationFile.BoneInfo[boneCount];
        var part = new AnimationFile.AnimationPart();
        for (var boneIndex = 0; boneIndex < boneCount; boneIndex++)
        {
            bones[boneIndex] = new AnimationFile.BoneInfo
            {
                Id = boneIndex,
                ParentId = boneIndex - 1,
                Name = $"bone_{boneIndex}",
            };
            frame.Transforms.Add(new RmvVector3(
                boneIndex == 0 ? 0 : 0.1f,
                0,
                0));
            frame.Quaternion.Add(new RmvVector4(
                rotation.X * storedQuaternionLength,
                rotation.Y * storedQuaternionLength,
                rotation.Z * storedQuaternionLength,
                rotation.W * storedQuaternionLength));
            part.TranslationMappings.Add(new AnimationFile.AnimationBoneMapping(boneIndex));
            part.RotationMappings.Add(new AnimationFile.AnimationBoneMapping(boneIndex));
        }
        part.DynamicFrames.Add(frame);

        return new AnimationFile
        {
            Header = new AnimationFile.AnimationHeader
            {
                Version = 7,
                SkeletonName = "non_unit_bind",
                FrameRate = 20,
                AnimationTotalPlayTimeInSec = 0.05f,
            },
            Bones = bones,
            AnimationParts = [part],
        };
    }

    private static AnimationFile CreateSingleBoneAnimation(
        float storedQuaternionLength)
    {
        var rotation = Xna.Quaternion.CreateFromAxisAngle(
            Xna.Vector3.UnitZ,
            MathF.PI);
        var frame = new AnimationFile.Frame
        {
            Transforms = [new RmvVector3(0, 0, 0)],
            Quaternion =
            [
                new RmvVector4(
                    rotation.X * storedQuaternionLength,
                    rotation.Y * storedQuaternionLength,
                    rotation.Z * storedQuaternionLength,
                    rotation.W * storedQuaternionLength),
            ],
        };
        var part = new AnimationFile.AnimationPart
        {
            DynamicFrames =
            [
                frame,
                new AnimationFile.Frame
                {
                    Transforms = [frame.Transforms[0]],
                    Quaternion = [frame.Quaternion[0]],
                },
            ],
            TranslationMappings = [new AnimationFile.AnimationBoneMapping(0)],
            RotationMappings = [new AnimationFile.AnimationBoneMapping(0)],
        };

        return new AnimationFile
        {
            Header = new AnimationFile.AnimationHeader
            {
                Version = 7,
                SkeletonName = "single_bone",
                FrameRate = 20,
                AnimationTotalPlayTimeInSec = 0.1f,
            },
            Bones =
            [
                new AnimationFile.BoneInfo
                {
                    Id = 0,
                    ParentId = AnimationFile.BoneIndexNoParent,
                    Name = "root",
                },
            ],
            AnimationParts = [part],
        };
    }

    private static void AssertAnimationsMatch(
        AnimationFile originalAnimation,
        AnimationFile importedAnimation,
        AnimationFile skeleton)
    {
        var gameSkeleton = new GameSkeleton(skeleton, null!);
        var originalClip = new AnimationClip(originalAnimation, gameSkeleton);
        var importedClip = new AnimationClip(importedAnimation, gameSkeleton);
        var maxTranslationError = 0.0f;
        var maxRotationError = 0.0f;
        var maxWorldPositionError = 0.0f;
        var translationLocation = "";
        var rotationLocation = "";
        var worldPositionLocation = "";

        var comparedFrameCount = Math.Min(
            originalClip.DynamicFrames.Count,
            importedClip.DynamicFrames.Count);
        for (var frameIndex = 0; frameIndex < comparedFrameCount; frameIndex++)
        {
            var originalFrame = originalClip.DynamicFrames[frameIndex];
            var importedFrame = importedClip.DynamicFrames[frameIndex];
            var originalWorldTransforms = BuildWorldTransforms(originalFrame, gameSkeleton);
            var importedWorldTransforms = BuildWorldTransforms(importedFrame, gameSkeleton);
            for (var boneIndex = 0; boneIndex < originalClip.AnimationBoneCount; boneIndex++)
            {
                var translationError = Xna.Vector3.Distance(
                    originalFrame.Position[boneIndex],
                    importedFrame.Position[boneIndex]);
                if (translationError > maxTranslationError)
                {
                    maxTranslationError = translationError;
                    translationLocation = $"frame {frameIndex}, bone {boneIndex} ({skeleton.Bones[boneIndex].Name})";
                }

                var originalRotation = Xna.Quaternion.Normalize(
                    originalFrame.Rotation[boneIndex]);
                var importedRotation = Xna.Quaternion.Normalize(
                    importedFrame.Rotation[boneIndex]);
                var quaternionDot = Math.Clamp(
                    Math.Abs(Xna.Quaternion.Dot(originalRotation, importedRotation)),
                    0.0f,
                    1.0f);
                var rotationError = 2.0f * MathF.Acos(quaternionDot);
                if (rotationError > maxRotationError)
                {
                    maxRotationError = rotationError;
                    rotationLocation = $"frame {frameIndex}, bone {boneIndex} ({skeleton.Bones[boneIndex].Name})";
                }

                var worldPositionError = Xna.Vector3.Distance(
                    originalWorldTransforms[boneIndex].Translation,
                    importedWorldTransforms[boneIndex].Translation);
                if (worldPositionError > maxWorldPositionError)
                {
                    maxWorldPositionError = worldPositionError;
                    worldPositionLocation = $"frame {frameIndex}, bone {boneIndex} ({skeleton.Bones[boneIndex].Name})";
                }
            }
        }

        Assert.Multiple(() =>
        {
            Assert.That(
                importedClip.DynamicFrames,
                Has.Count.EqualTo(originalClip.DynamicFrames.Count));
            Assert.That(
                maxTranslationError,
                Is.LessThan(0.0001f),
                $"Largest local translation error at {translationLocation}");
            Assert.That(
                maxRotationError,
                Is.LessThan(0.002f),
                $"Largest local rotation error at {rotationLocation}");
            Assert.That(
                maxWorldPositionError,
                Is.LessThan(0.0001f),
                $"Largest bone world position error at {worldPositionLocation}");
        });
    }

    private static (Xna.Vector3 Min, Xna.Vector3 Max) GetLod0Bounds(RmvFile model)
    {
        var min = new Xna.Vector3(float.PositiveInfinity);
        var max = new Xna.Vector3(float.NegativeInfinity);
        foreach (var vertex in model.ModelList[0]
                     .SelectMany(segment => segment.Mesh.VertexList))
        {
            var position = new Xna.Vector3(
                vertex.Position.X,
                vertex.Position.Y,
                vertex.Position.Z);
            min = Xna.Vector3.Min(min, position);
            max = Xna.Vector3.Max(max, position);
        }

        return (min, max);
    }

    private static Xna.Matrix[] BuildWorldTransforms(
        AnimationClip.KeyFrame frame,
        GameSkeleton skeleton)
    {
        var worldTransforms = new Xna.Matrix[skeleton.BoneCount];
        var completed = new bool[skeleton.BoneCount];

        Xna.Matrix BuildWorldTransform(int boneIndex)
        {
            if (completed[boneIndex])
                return worldTransforms[boneIndex];

            var localTransform =
                Xna.Matrix.CreateScale(frame.Scale[boneIndex]) *
                Xna.Matrix.CreateFromQuaternion(
                    Xna.Quaternion.Normalize(frame.Rotation[boneIndex])) *
                Xna.Matrix.CreateTranslation(frame.Position[boneIndex]);
            var parentBoneIndex = skeleton.GetParentBoneIndex(boneIndex);
            worldTransforms[boneIndex] = parentBoneIndex == -1
                ? localTransform
                : localTransform * BuildWorldTransform(parentBoneIndex);
            completed[boneIndex] = true;
            return worldTransforms[boneIndex];
        }

        for (var boneIndex = 0; boneIndex < skeleton.BoneCount; boneIndex++)
            BuildWorldTransform(boneIndex);

        return worldTransforms;
    }
}
