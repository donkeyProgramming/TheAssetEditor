using System.Numerics;
using Editors.ImportExport.Importing.Importers.GltfToRmv;
using Editors.ImportExport.Importing.Importers.GltfToRmv.Helper;
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
using Shared.TestUtility;
using SharpGLTF.Geometry;
using SharpGLTF.Geometry.VertexTypes;
using SharpGLTF.Materials;
using SharpGLTF.Schema2;

namespace Test.ImportExport.Importing.Importers.GltfImporterTest;

public class GltfHumanoidAutoScaleTests
{
    private const string HumanoidSkeletonPath = @"animations\skeletons\humanoid01.anim";

    [Test]
    public void Import_ExternalHumanoid_AppliesOneScaleToModelSkeletonAnimationAndSummary()
    {
        var glbPath = CreateExternalHumanoidGlb(
            sourceHeight: 4,
            headName: "Head",
            leftFootName: "BALL_L",
            rightFootName: "Ball_Right");
        try
        {
            var destination = PackFileContainer.CreatePackFile("test");
            var importer = CreateImporter(
                destination,
                CreateReferenceContainer("ca", height: 2, isCaPack: true),
                CreateReferenceContainer("mod", height: 20, isCaPack: false));

            var result = importer.Import(CreateSettings(glbPath, destination));
            var unscaledDestination = PackFileContainer.CreatePackFile("unscaled");
            var unscaledResult = CreateImporter(unscaledDestination).Import(
                CreateSettings(glbPath, unscaledDestination, autoScaleHumanoid: false));

            var baseName = Path.GetFileNameWithoutExtension(glbPath).ToLowerInvariant();
            var model = ModelFactory.Create().Load(
                destination.FileList[$@"models\{baseName}.rigid_model_v2"].DataSource.ReadData());
            var unscaledModel = ModelFactory.Create().Load(
                unscaledDestination.FileList[$@"models\{baseName}.rigid_model_v2"].DataSource.ReadData());
            var skeleton = AnimationFile.Create(
                destination.FileList[@"animations\skeletons\externalarmature.anim"]);
            var unscaledSkeleton = AnimationFile.Create(
                unscaledDestination.FileList[@"animations\skeletons\externalarmature.anim"]);
            var animation = AnimationFile.Create(
                destination.FileList[$@"models\{baseName}_move.anim"]);
            var unscaledAnimation = AnimationFile.Create(
                unscaledDestination.FileList[$@"models\{baseName}_move.anim"]);
            var headIndex = Array.FindIndex(
                skeleton.Bones,
                bone => string.Equals(bone.Name, "head", StringComparison.OrdinalIgnoreCase));
            var vertex = model.ModelList[0][0].Mesh.VertexList[0];
            var unscaledVertex = unscaledModel.ModelList[0][0].Mesh.VertexList[0];

            Assert.Multiple(() =>
            {
                Assert.That(result.Succeeded, Is.True, string.Join(Environment.NewLine, result.Errors));
                Assert.That(unscaledResult.Succeeded, Is.True, string.Join(Environment.NewLine, unscaledResult.Errors));
                Assert.That(result.HumanoidScale, Is.Not.Null);
                Assert.That(result.HumanoidScale!.Applied, Is.True);
                Assert.That(result.HumanoidScale.SourceHeight, Is.EqualTo(4).Within(0.0001f));
                Assert.That(result.HumanoidScale.ReferenceHeight, Is.EqualTo(2).Within(0.0001f));
                Assert.That(result.HumanoidScale.ScaleFactor, Is.EqualTo(0.5f).Within(0.0001f));
                Assert.That(
                    model.ModelList[0][0].Mesh.VertexList.Max(vertex => vertex.Position.Y),
                    Is.EqualTo(2).Within(0.0001f));
                Assert.That(
                    skeleton.AnimationParts[0].DynamicFrames[0].Transforms[headIndex].Y,
                    Is.EqualTo(2).Within(0.0001f));
                Assert.That(
                    animation.AnimationParts[0].DynamicFrames[^1].Transforms[0].Y,
                    Is.EqualTo(1).Within(0.0001f));
                Assert.That(vertex.Normal, Is.EqualTo(unscaledVertex.Normal));
                Assert.That(vertex.Tangent, Is.EqualTo(unscaledVertex.Tangent));
                Assert.That(vertex.Uv, Is.EqualTo(unscaledVertex.Uv));
                Assert.That(vertex.BoneIndex, Is.EqualTo(unscaledVertex.BoneIndex));
                Assert.That(vertex.BoneWeight, Is.EqualTo(unscaledVertex.BoneWeight));
                Assert.That(
                    model.ModelList[0][0].Material.ModelName,
                    Is.EqualTo(unscaledModel.ModelList[0][0].Material.ModelName));
                Assert.That(
                    skeleton.AnimationParts[0].DynamicFrames[0].Quaternion,
                    Is.EqualTo(unscaledSkeleton.AnimationParts[0].DynamicFrames[0].Quaternion));
                Assert.That(
                    animation.AnimationParts[0].DynamicFrames[^1].Quaternion,
                    Is.EqualTo(unscaledAnimation.AnimationParts[0].DynamicFrames[^1].Quaternion));
            });
        }
        finally
        {
            File.Delete(glbPath);
        }
    }

    [Test]
    public void Import_ExternalHumanoidWithAutoScaleDisabled_PreservesSourceSize()
    {
        var glbPath = CreateExternalHumanoidGlb(sourceHeight: 4);
        try
        {
            var destination = PackFileContainer.CreatePackFile("test");
            var result = CreateImporter(destination).Import(
                CreateSettings(glbPath, destination, autoScaleHumanoid: false));

            var baseName = Path.GetFileNameWithoutExtension(glbPath).ToLowerInvariant();
            var model = ModelFactory.Create().Load(
                destination.FileList[$@"models\{baseName}.rigid_model_v2"].DataSource.ReadData());
            var skeleton = AnimationFile.Create(
                destination.FileList[@"animations\skeletons\externalarmature.anim"]);
            var animation = AnimationFile.Create(
                destination.FileList[$@"models\{baseName}_move.anim"]);
            var headIndex = Array.FindIndex(skeleton.Bones, bone => bone.Name == "head");

            Assert.Multiple(() =>
            {
                Assert.That(result.Succeeded, Is.True, string.Join(Environment.NewLine, result.Errors));
                Assert.That(result.HumanoidScale!.Applied, Is.False);
                Assert.That(result.HumanoidScale.ScaleFactor, Is.EqualTo(1));
                Assert.That(result.HumanoidScale.Reason, Does.Contain("turned off"));
                Assert.That(
                    model.ModelList[0][0].Mesh.VertexList.Max(vertex => vertex.Position.Y),
                    Is.EqualTo(4).Within(0.0001f));
                Assert.That(
                    skeleton.AnimationParts[0].DynamicFrames[0].Transforms[headIndex].Y,
                    Is.EqualTo(4).Within(0.0001f));
                Assert.That(
                    animation.AnimationParts[0].DynamicFrames[^1].Transforms[0].Y,
                    Is.EqualTo(2).Within(0.0001f));
            });
        }
        finally
        {
            File.Delete(glbPath);
        }
    }

    [Test]
    public void Import_StaticModelWithAutoScaleEnabled_PreservesSizeAndReportsReason()
    {
        var glbPath = CreateStaticGlb(height: 4);
        try
        {
            var destination = PackFileContainer.CreatePackFile("test");
            var result = CreateImporter(destination).Import(
                CreateSettings(glbPath, destination, importAnimations: false));

            var baseName = Path.GetFileNameWithoutExtension(glbPath).ToLowerInvariant();
            var model = ModelFactory.Create().Load(
                destination.FileList[$@"models\{baseName}.rigid_model_v2"].DataSource.ReadData());

            Assert.Multiple(() =>
            {
                Assert.That(result.Succeeded, Is.True, string.Join(Environment.NewLine, result.Errors));
                Assert.That(result.HumanoidScale!.Applied, Is.False);
                Assert.That(result.HumanoidScale.ScaleFactor, Is.EqualTo(1));
                Assert.That(result.HumanoidScale.Reason, Does.Contain("static models"));
                Assert.That(
                    model.ModelList[0][0].Mesh.VertexList.Max(vertex => vertex.Position.Y),
                    Is.EqualTo(4).Within(0.0001f));
            });
        }
        finally
        {
            File.Delete(glbPath);
        }
    }

    [Test]
    public void Import_ExternalNonHumanoid_PreservesSizeWithoutLoadingReferenceSkeleton()
    {
        var glbPath = CreateExternalHumanoidGlb(
            sourceHeight: 4,
            headName: "sensor",
            leftFootName: "wing_left",
            rightFootName: "wing_right");
        try
        {
            var destination = PackFileContainer.CreatePackFile("test");
            var result = CreateImporter(destination).Import(CreateSettings(glbPath, destination));

            Assert.Multiple(() =>
            {
                Assert.That(result.Succeeded, Is.True, string.Join(Environment.NewLine, result.Errors));
                Assert.That(result.HumanoidScale!.Applied, Is.False);
                Assert.That(result.HumanoidScale.ScaleFactor, Is.EqualTo(1));
                Assert.That(result.HumanoidScale.Reason, Does.Contain("non-humanoid"));
            });
        }
        finally
        {
            File.Delete(glbPath);
        }
    }

    [Test]
    public void Import_PartialHumanoidAnchors_ReturnsFailureAndLeavesPackUnchanged()
    {
        var glbPath = CreateExternalHumanoidGlb(sourceHeight: 4, rightFootName: null);
        try
        {
            var destination = CreateDestinationWithExistingFile(out var existingFile);
            var result = CreateImporter(destination).Import(CreateSettings(glbPath, destination));

            Assert.Multiple(() =>
            {
                Assert.That(result.Succeeded, Is.False);
                Assert.That(result.Errors, Has.Some.Contains("Only some humanoid anchors"));
                Assert.That(result.Errors, Has.Some.Contains("right foot"));
                AssertDestinationUnchanged(destination, existingFile);
            });
        }
        finally
        {
            File.Delete(glbPath);
        }
    }

    [Test]
    public void Import_DuplicateHumanoidAnchor_ReturnsFailureAndLeavesPackUnchanged()
    {
        var glbPath = CreateExternalHumanoidGlb(sourceHeight: 4, duplicateHead: true);
        try
        {
            var destination = CreateDestinationWithExistingFile(out var existingFile);
            var result = CreateImporter(destination).Import(CreateSettings(glbPath, destination));

            Assert.Multiple(() =>
            {
                Assert.That(result.Succeeded, Is.False);
                Assert.That(result.Errors, Has.Some.Contains("more than one candidate"));
                Assert.That(result.Errors, Has.Some.Contains("head"));
                AssertDestinationUnchanged(destination, existingFile);
            });
        }
        finally
        {
            File.Delete(glbPath);
        }
    }

    [Test]
    public void Import_ZeroHumanoidHeight_ReturnsFailureAndLeavesPackUnchanged()
    {
        var glbPath = CreateExternalHumanoidGlb(sourceHeight: 0);
        try
        {
            var destination = CreateDestinationWithExistingFile(out var existingFile);
            var result = CreateImporter(destination).Import(CreateSettings(glbPath, destination));

            Assert.Multiple(() =>
            {
                Assert.That(result.Succeeded, Is.False);
                Assert.That(result.Errors, Has.Some.Contains("height of the source skeleton is invalid"));
                AssertDestinationUnchanged(destination, existingFile);
            });
        }
        finally
        {
            File.Delete(glbPath);
        }
    }

    [Test]
    public void Import_HumanoidWithoutCaReference_ReturnsFailureAndLeavesPackUnchanged()
    {
        var glbPath = CreateExternalHumanoidGlb(sourceHeight: 4);
        try
        {
            var destination = CreateDestinationWithExistingFile(out var existingFile);
            var result = CreateImporter(destination).Import(CreateSettings(glbPath, destination));

            Assert.Multiple(() =>
            {
                Assert.That(result.Succeeded, Is.False);
                Assert.That(result.Errors, Has.Some.Contains("loaded CA packs"));
                Assert.That(result.Errors, Has.Some.Contains("humanoid01.anim"));
                AssertDestinationUnchanged(destination, existingFile);
            });
        }
        finally
        {
            File.Delete(glbPath);
        }
    }

    [Test]
    public void Import_HumanoidForAnotherGame_ReturnsFailureAndLeavesPackUnchanged()
    {
        var glbPath = CreateExternalHumanoidGlb(sourceHeight: 4);
        try
        {
            var destination = CreateDestinationWithExistingFile(out var existingFile);
            var result = CreateImporter(
                destination,
                CreateReferenceContainer("ca", height: 2, isCaPack: true)).Import(
                    CreateSettings(
                        glbPath,
                        destination,
                        gameType: GameTypeEnum.Warhammer2));

            Assert.Multiple(() =>
            {
                Assert.That(result.Succeeded, Is.False);
                Assert.That(result.Errors, Has.Some.Contains("Warhammer 3"));
                AssertDestinationUnchanged(destination, existingFile);
            });
        }
        finally
        {
            File.Delete(glbPath);
        }
    }

    [Test]
    public void ImporterViewModel_DisablingAutoScale_ReachesImporterSettings()
    {
        var glbPath = CreateExternalHumanoidGlb(sourceHeight: 4);
        try
        {
            var destination = PackFileContainer.CreatePackFile("test");
            var viewModel = new RmvToGltfImporterViewModel(CreateImporter(destination))
            {
                AutoScaleHumanoid = false,
                ImportMaterials = false,
            };

            var result = viewModel.Execute(
                new PackFile(glbPath, new FileSystemSource(glbPath)),
                "models",
                destination,
                GameTypeEnum.Warhammer3,
            null,
            CancellationToken.None);

            Assert.Multiple(() =>
            {
                Assert.That(result.Succeeded, Is.True, string.Join(Environment.NewLine, result.Errors));
                Assert.That(result.HumanoidScale!.Applied, Is.False);
                Assert.That(result.HumanoidScale.Reason, Does.Contain("turned off"));
            });
        }
        finally
        {
            File.Delete(glbPath);
        }
    }

    private static GltfImporter CreateImporter(
        PackFileContainer destination,
        params PackFileContainer[] referenceContainers)
    {
        var innerPackFileService = PackFileSerivceTestHelper.Create(TestData.InputPack);
        var packFileService = new Mock<IPackFileService>();
        packFileService
            .Setup(service => service.GetAllPackfileContainers())
            .Returns(referenceContainers.Cast<IPackFileContainer>().ToList());
        packFileService
            .Setup(service => service.AddFilesToPack(
                destination,
                It.IsAny<List<NewPackFileEntry>>()))
            .Callback<IPackFileContainer, List<NewPackFileEntry>>((container, entries) =>
                    innerPackFileService.AddFilesToPack(
                        container,
                        entries));
        return new GltfImporter(
            packFileService.Object,
            Mock.Of<ISkeletonAnimationLookUpHelper>(),
            new RmvMaterialBuilder());
    }

    private static PackFileContainer CreateReferenceContainer(
        string name,
        float height,
        bool isCaPack)
    {
        var container = PackFileContainer.CreatePackFile(name);
        container.IsCaPackFile = isCaPack;
        container.FileList[HumanoidSkeletonPath] = new PackFile(
            "humanoid01.anim",
            new MemorySource(AnimationFile.ConvertToBytes(CreateHumanoidSkeleton(height))));
        return container;
    }

    private static AnimationFile CreateHumanoidSkeleton(float height)
    {
        var frame = new AnimationFile.Frame
        {
            Transforms =
            [
                new RmvVector3(0, 0, 0),
                new RmvVector3(-0.25f, 0, 0),
                new RmvVector3(0.25f, 0, 0),
                new RmvVector3(0, height, 0),
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
            part.TranslationMappings.Add(new AnimationFile.AnimationBoneMapping(boneIndex));
            part.RotationMappings.Add(new AnimationFile.AnimationBoneMapping(boneIndex));
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
                new AnimationFile.BoneInfo { Id = 0, ParentId = -1, Name = "root" },
                new AnimationFile.BoneInfo { Id = 1, ParentId = 0, Name = "toe_left_0" },
                new AnimationFile.BoneInfo { Id = 2, ParentId = 0, Name = "toe_right_0" },
                new AnimationFile.BoneInfo { Id = 3, ParentId = 0, Name = "head" },
            ],
            AnimationParts = [part],
        };
    }

    private static GltfImporterSettings CreateSettings(
        string glbPath,
        PackFileContainer destination,
        bool autoScaleHumanoid = true,
        bool importAnimations = true,
        GameTypeEnum gameType = GameTypeEnum.Warhammer3) => new(
        glbPath,
        "models",
        destination,
        gameType,
        true,
        false,
        false,
        false,
        importAnimations,
        20,
        AutoScaleHumanoid: autoScaleHumanoid);

    private static string CreateExternalHumanoidGlb(
        float sourceHeight,
        string headName = "head",
        string? leftFootName = "toe_left_0",
        string? rightFootName = "toe_right_0",
        bool duplicateHead = false)
    {
        var geometry = new MeshBuilder<
            VertexPositionNormalTangent,
            VertexTexture1,
            VertexJoints4>("mesh");
        var primitive = geometry.UsePrimitive(
            new MaterialBuilder("material").WithMetallicRoughness());
        primitive.AddTriangle(
            CreateVertex(0, sourceHeight),
            CreateVertex(-0.5f, 0),
            CreateVertex(0.5f, 0));

        var modelRoot = ModelRoot.CreateModel();
        var mesh = modelRoot.CreateMesh(geometry);
        var scene = modelRoot.UseScene("default");
        var armature = scene.CreateNode("ExternalArmature");
        var root = armature.CreateNode("root");
        var joints = new List<Node> { root };
        if (leftFootName != null)
        {
            var leftToe = root.CreateNode(leftFootName);
            leftToe.LocalMatrix = Matrix4x4.CreateTranslation(-0.25f, 0, 0);
            joints.Add(leftToe);
        }
        if (rightFootName != null)
        {
            var rightToe = root.CreateNode(rightFootName);
            rightToe.LocalMatrix = Matrix4x4.CreateTranslation(0.25f, 0, 0);
            joints.Add(rightToe);
        }
        var head = root.CreateNode(headName);
        head.LocalMatrix = Matrix4x4.CreateTranslation(0, sourceHeight, 0);
        joints.Add(head);
        if (duplicateHead)
        {
            var secondHead = root.CreateNode("head_0");
            secondHead.LocalMatrix = Matrix4x4.CreateTranslation(0, sourceHeight, 0);
            joints.Add(secondHead);
        }

        scene.CreateNode("mesh_node").WithSkinnedMesh(
            mesh,
            joints.Select(CreateBinding).ToArray());
        modelRoot.LogicalSkins.Single().Name = "ExternalArmature";
        modelRoot.CreateAnimation("Move").CreateTranslationChannel(
            root,
            new Dictionary<float, Vector3>
            {
                [0.00f] = Vector3.Zero,
                [0.05f] = new Vector3(0, 2, 0),
            });

        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.glb");
        modelRoot.SaveGLB(path);
        return path;
    }

    private static string CreateStaticGlb(float height)
    {
        var geometry = new MeshBuilder<
            VertexPositionNormalTangent,
            VertexTexture1,
            VertexEmpty>("mesh");
        var primitive = geometry.UsePrimitive(
            new MaterialBuilder("material").WithMetallicRoughness());
        primitive.AddTriangle(
            CreateStaticVertex(0, height),
            CreateStaticVertex(-0.5f, 0),
            CreateStaticVertex(0.5f, 0));
        var modelRoot = ModelRoot.CreateModel();
        var mesh = modelRoot.CreateMesh(geometry);
        modelRoot.UseScene("default").CreateNode("mesh_node").Mesh = mesh;
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.glb");
        modelRoot.SaveGLB(path);
        return path;
    }

    private static (Node Joint, Matrix4x4 InverseBindMatrix) CreateBinding(Node joint)
    {
        Assert.That(Matrix4x4.Invert(joint.WorldMatrix, out var inverseBind), Is.True);
        return (joint, inverseBind);
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

    private static VertexBuilder<
        VertexPositionNormalTangent,
        VertexTexture1,
        VertexEmpty> CreateStaticVertex(float x, float y)
    {
        var vertex = new VertexBuilder<
            VertexPositionNormalTangent,
            VertexTexture1,
            VertexEmpty>();
        vertex.Geometry.Position = new Vector3(x, y, 0);
        vertex.Geometry.Normal = Vector3.UnitZ;
        vertex.Geometry.Tangent = new Vector4(1, 0, 0, 1);
        vertex.Material.TexCoord = new Vector2(x, y);
        return vertex;
    }

    private static PackFileContainer CreateDestinationWithExistingFile(
        out PackFile existingFile)
    {
        var destination = PackFileContainer.CreatePackFile("test");
        existingFile = new PackFile("existing.bin", new MemorySource([7, 8, 9]));
        destination.FileList[@"models\existing.bin"] = existingFile;
        return destination;
    }

    private static void AssertDestinationUnchanged(
        PackFileContainer destination,
        PackFile existingFile)
    {
        Assert.That(destination.FileList, Has.Count.EqualTo(1));
        Assert.That(destination.FileList[@"models\existing.bin"], Is.SameAs(existingFile));
        Assert.That(existingFile.DataSource.ReadData(), Is.EqualTo(new byte[] { 7, 8, 9 }));
    }

    private static AnimationFile.Frame CloneFrame(AnimationFile.Frame frame) => new()
    {
        Transforms = frame.Transforms.ToList(),
        Quaternion = frame.Quaternion.ToList(),
    };
}
