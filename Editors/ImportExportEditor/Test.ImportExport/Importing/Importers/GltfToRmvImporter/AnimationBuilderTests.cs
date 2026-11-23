using Editors.ImportExport.Exporting.Exporters.RmvToGltf.Helpers;
using Editors.ImportExport.Importing.Importers.GltfToRmv.Helper;
using GameWorld.Core.Animation;
using Shared.Core.PackFiles.Models;
using Shared.Core.PackFiles.Models.Containers;
using Shared.Core.Services;
using Shared.GameFormats.Animation;
using SharpGLTF.Geometry;
using SharpGLTF.Geometry.VertexTypes;
using SharpGLTF.Materials;
using SharpGLTF.Schema2;
using Xna = Microsoft.Xna.Framework;
using Numerics = System.Numerics;

namespace Test.ImportExport.Importing.Importers.GltfImporterTest;

public class AnimationBuilderTests
{
    [Test]
    public void Build_SkinJointUsesDifferentLocalBasis_PreservesSkinnedPosition()
    {
        var modelRoot = ModelRoot.CreateModel();
        var scene = modelRoot.UseScene("default");
        var joint = scene.CreateNode("root");
        var restRotation = Numerics.Quaternion.CreateFromAxisAngle(
            Numerics.Vector3.UnitZ,
            MathF.PI / 2.0f);
        var restMatrix = Numerics.Matrix4x4.CreateFromQuaternion(restRotation);
        joint.LocalMatrix = restMatrix;
        Assert.That(Numerics.Matrix4x4.Invert(restMatrix, out var inverseBindMatrix), Is.True);

        var geometry = new MeshBuilder<
            VertexPositionNormalTangent,
            VertexTexture1,
            VertexJoints4>("skinned_mesh");
        var primitive = geometry.UsePrimitive(
            new MaterialBuilder("material").WithMetallicRoughness());
        primitive.AddTriangle(
            CreateSkinnedVertex(new Numerics.Vector3(0, 1, 0)),
            CreateSkinnedVertex(new Numerics.Vector3(1, 0, 0)),
            CreateSkinnedVertex(Numerics.Vector3.Zero));
        var mesh = modelRoot.CreateMesh(geometry);
        scene.CreateNode("mesh").WithSkinnedMesh(
            mesh,
            [(joint, inverseBindMatrix)]);

        var motion = Numerics.Matrix4x4.CreateRotationX(0.6f);
        var animatedMatrix = restMatrix * motion;
        var animatedRotation = Numerics.Quaternion.CreateFromRotationMatrix(animatedMatrix);
        var animation = modelRoot.CreateAnimation("basis_changed");
        animation.CreateRotationChannel(joint, new Dictionary<float, Numerics.Quaternion>
        {
            [0] = restRotation,
            [1] = animatedRotation,
        });

        var skeleton = CreateSingleBoneSkeleton();
        var imported = AnimationBuilder.Build(
            new AnimationBuilderSettings(
                modelRoot,
                "test",
                1.0f,
                PackFileContainer.CreatePackFile("test"),
                "animations",
                false),
            skeleton,
            animation);
        var gameSkeleton = new GameSkeleton(skeleton, null!);
        var clip = new AnimationClip(imported, gameSkeleton);
        var sampledFrame = AnimationSampler.Sample(1.0f, gameSkeleton, clip);
        var gamePoint = new Xna.Vector3(0, 1, 0);
        var actualPosition = Xna.Vector3.Transform(
            gamePoint,
            sampledFrame.BoneTransforms[0].WorldTransform);

        var expectedGltf = Numerics.Vector3.Transform(
            new Numerics.Vector3(0, 1, 0),
            inverseBindMatrix * joint.GetWorldMatrix(animation, animation.Duration));
        var expectedPosition = new Xna.Vector3(
            -expectedGltf.X,
            expectedGltf.Y,
            expectedGltf.Z);

        Assert.That(
            Xna.Vector3.Distance(actualPosition, expectedPosition),
            Is.LessThan(0.0001f));
    }

    [TestCase(0.00005f, -1.00005f)]
    [TestCase(0.0002f, -1.0f)]
    public void Build_NearlyEquivalentBindPose_UsesExpectedAnimationBasis(
        float sourceBindTranslation,
        float expectedTranslation)
    {
        var modelRoot = ModelRoot.CreateModel();
        var scene = modelRoot.UseScene("default");
        var joint = scene.CreateNode("root");
        joint.LocalMatrix = Numerics.Matrix4x4.CreateTranslation(
            sourceBindTranslation,
            0,
            0);
        var geometry = new MeshBuilder<
            VertexPositionNormalTangent,
            VertexTexture1,
            VertexJoints4>("skinned_mesh");
        geometry.UsePrimitive(
                new MaterialBuilder("material").WithMetallicRoughness())
            .AddTriangle(
                CreateSkinnedVertex(Numerics.Vector3.Zero),
                CreateSkinnedVertex(Numerics.Vector3.UnitX),
                CreateSkinnedVertex(Numerics.Vector3.UnitY));
        scene.CreateNode("mesh").WithSkinnedMesh(
            modelRoot.CreateMesh(geometry),
            (joint, Numerics.Matrix4x4.CreateTranslation(
                -sourceBindTranslation,
                0,
                0)));
        var animation = modelRoot.CreateAnimation("near_bind_boundary");
        var animatedTranslation = new Numerics.Vector3(
            sourceBindTranslation + 1,
            0,
            0);
        animation.CreateTranslationChannel(
            joint,
            new Dictionary<float, Numerics.Vector3>
            {
                [0] = animatedTranslation,
                [1] = animatedTranslation,
            });

        var imported = AnimationBuilder.Build(
            new AnimationBuilderSettings(
                modelRoot,
                "test",
                1.0f,
                PackFileContainer.CreatePackFile("test"),
                "animations",
                false),
            CreateSingleBoneSkeleton(),
            animation);

        Assert.That(
            imported.AnimationParts[0].DynamicFrames[0].Transforms[0].X,
            Is.EqualTo(expectedTranslation).Within(0.000001f));
    }

    [Test]
    public void Build_UniformGltfKeys_AutoDetectsSamplingRateAndAnimDuration()
    {
        const float frameRate = 24.0f;
        var modelRoot = ModelRoot.CreateModel();
        var node = modelRoot.UseScene("default").CreateNode("root");
        var animation = modelRoot.CreateAnimation("baked_24_fps");
        animation.CreateRotationChannel(node, new Dictionary<float, Numerics.Quaternion>
        {
            [0.0f / frameRate] = Numerics.Quaternion.Identity,
            [1.0f / frameRate] = Numerics.Quaternion.Identity,
            [2.0f / frameRate] = Numerics.Quaternion.Identity,
            [3.0f / frameRate] = Numerics.Quaternion.Identity,
        });

        var result = AnimationBuilder.Build(
            new AnimationBuilderSettings(
                modelRoot,
                "test",
                20.0f,
                PackFileContainer.CreatePackFile("test"),
                "animations"),
            CreateSingleBoneSkeleton(),
            animation);

        Assert.Multiple(() =>
        {
            Assert.That(result.Header.FrameRate, Is.EqualTo(frameRate).Within(0.001f));
            Assert.That(result.AnimationParts[0].DynamicFrames, Has.Count.EqualTo(4));
            Assert.That(
                result.Header.AnimationTotalPlayTimeInSec,
                Is.EqualTo(3.0f / frameRate).Within(0.0001f));
        });
    }

    [Test]
    public void Build_NonUniformGltfKeys_UsesManualSamplingRateFallback()
    {
        var modelRoot = ModelRoot.CreateModel();
        var node = modelRoot.UseScene("default").CreateNode("root");
        var animation = modelRoot.CreateAnimation("sparse_keys");
        animation.CreateRotationChannel(node, new Dictionary<float, Numerics.Quaternion>
        {
            [0.00f] = Numerics.Quaternion.Identity,
            [0.04f] = Numerics.Quaternion.Identity,
            [0.09f] = Numerics.Quaternion.Identity,
        });

        var result = AnimationBuilder.Build(
            new AnimationBuilderSettings(
                modelRoot,
                "test",
                20.0f,
                PackFileContainer.CreatePackFile("test"),
                "animations"),
            CreateSingleBoneSkeleton(),
            animation);

        Assert.That(result.Header.FrameRate, Is.EqualTo(20.0f));
    }

    [Test]
    public void Build_AutoDetectionDisabled_UsesManualSamplingRate()
    {
        const float sourceFrameRate = 24.0f;
        var modelRoot = ModelRoot.CreateModel();
        var node = modelRoot.UseScene("default").CreateNode("root");
        var animation = modelRoot.CreateAnimation("baked_24_fps");
        animation.CreateRotationChannel(node, new Dictionary<float, Numerics.Quaternion>
        {
            [0.0f / sourceFrameRate] = Numerics.Quaternion.Identity,
            [1.0f / sourceFrameRate] = Numerics.Quaternion.Identity,
            [2.0f / sourceFrameRate] = Numerics.Quaternion.Identity,
            [3.0f / sourceFrameRate] = Numerics.Quaternion.Identity,
        });

        var result = AnimationBuilder.Build(
            new AnimationBuilderSettings(
                modelRoot,
                "test",
                30.0f,
                PackFileContainer.CreatePackFile("test"),
                "animations",
                false),
            CreateSingleBoneSkeleton(),
            animation);

        Assert.That(result.Header.FrameRate, Is.EqualTo(30.0f));
    }

    [Test]
    public void Build_AnimatedTransformContainsShear_ThrowsWithAnimationAndBoneNames()
    {
        var modelRoot = ModelRoot.CreateModel();
        var scene = modelRoot.UseScene("default");
        var joint = scene.CreateNode("root");
        var geometry = new MeshBuilder<
            VertexPositionNormalTangent,
            VertexTexture1,
            VertexJoints4>("skinned_mesh");
        geometry.UsePrimitive(
                new MaterialBuilder("material").WithMetallicRoughness())
            .AddTriangle(
                CreateSkinnedVertex(Numerics.Vector3.Zero),
                CreateSkinnedVertex(Numerics.Vector3.UnitX),
                CreateSkinnedVertex(Numerics.Vector3.UnitY));
        scene.CreateNode("mesh").WithSkinnedMesh(
            modelRoot.CreateMesh(geometry),
            (joint, Numerics.Matrix4x4.Identity));
        var animation = modelRoot.CreateAnimation("Sheared Action");
        animation.CreateScaleChannel(joint, new Dictionary<float, Numerics.Vector3>
        {
            [0] = new Numerics.Vector3(2, 1, 1),
            [1] = new Numerics.Vector3(2, 1, 1),
        });
        var skeleton = CreateSingleBoneSkeleton();
        var targetBindRotation = Numerics.Quaternion.CreateFromAxisAngle(
            Numerics.Vector3.UnitZ,
            -MathF.PI / 4.0f);
        skeleton.AnimationParts[0].DynamicFrames[0].Quaternion[0] =
            new Shared.GameFormats.RigidModel.Transforms.RmvVector4(
                targetBindRotation.X,
                targetBindRotation.Y,
                targetBindRotation.Z,
                targetBindRotation.W);

        var exception = Assert.Throws<InvalidDataException>(() =>
            AnimationBuilder.Build(
                new AnimationBuilderSettings(
                    modelRoot,
                    "test",
                    1.0f,
                    PackFileContainer.CreatePackFile("test"),
                    "animations",
                    false),
                skeleton,
                animation));

        Assert.Multiple(() =>
        {
            Assert.That(exception!.Message, Does.Contain("Sheared Action"));
            Assert.That(exception.Message, Does.Contain("root"));
            Assert.That(exception.Message, Does.Contain("shear"));
        });
    }

    [TestCase(0, 0, 1, 1.000078f)]
    [TestCase(1, 2, 3, 1.000078f)]
    [TestCase(1, 2, 3, 0.99995f)]
    public void Build_UnitScaleKeysWithRetargetMatrixDrift_PreservesRotation(
        float axisX,
        float axisY,
        float axisZ,
        float storedQuaternionLength)
    {
        var modelRoot = ModelRoot.CreateModel();
        var scene = modelRoot.UseScene("default");
        var joint = scene.CreateNode("root");
        var geometry = new MeshBuilder<
            VertexPositionNormalTangent,
            VertexTexture1,
            VertexJoints4>("skinned_mesh");
        geometry.UsePrimitive(
                new MaterialBuilder("material").WithMetallicRoughness())
            .AddTriangle(
                CreateSkinnedVertex(Numerics.Vector3.Zero),
                CreateSkinnedVertex(Numerics.Vector3.UnitX),
                CreateSkinnedVertex(Numerics.Vector3.UnitY));
        scene.CreateNode("mesh").WithSkinnedMesh(
            modelRoot.CreateMesh(geometry),
            (joint, Numerics.Matrix4x4.Identity));
        var animation = modelRoot.CreateAnimation("Blender Roundtrip");
        animation.CreateScaleChannel(joint, new Dictionary<float, Numerics.Vector3>
        {
            [0] = Numerics.Vector3.One,
            [1] = new Numerics.Vector3(0.9999999f, 1, 1),
        });
        animation.CreateRotationChannel(joint, new Dictionary<float, Numerics.Quaternion>
        {
            [0] = Numerics.Quaternion.Identity,
            [1] = Numerics.Quaternion.Identity,
        });
        var skeleton = CreateSingleBoneSkeleton();
        var targetBindRotation = Numerics.Quaternion.CreateFromAxisAngle(
            Numerics.Vector3.Normalize(new Numerics.Vector3(axisX, axisY, axisZ)),
            MathF.PI / 2.0f);
        skeleton.AnimationParts[0].DynamicFrames[0].Quaternion[0] =
            new Shared.GameFormats.RigidModel.Transforms.RmvVector4(
                targetBindRotation.X * storedQuaternionLength,
                targetBindRotation.Y * storedQuaternionLength,
                targetBindRotation.Z * storedQuaternionLength,
                targetBindRotation.W * storedQuaternionLength);

        var imported = AnimationBuilder.Build(
            new AnimationBuilderSettings(
                modelRoot,
                "test",
                1.0f,
                PackFileContainer.CreatePackFile("test"),
                "animations",
                false),
            skeleton,
            animation);

        var rotation = imported.AnimationParts[0].DynamicFrames[0].Quaternion[0];
        var rotationLength = MathF.Sqrt(
            rotation.X * rotation.X +
            rotation.Y * rotation.Y +
            rotation.Z * rotation.Z +
            rotation.W * rotation.W);
        Assert.Multiple(() =>
        {
            Assert.That(rotationLength, Is.EqualTo(1.0f).Within(0.000001f));
            Assert.That(
                MathF.Abs(Numerics.Quaternion.Dot(
                    targetBindRotation,
                    new Numerics.Quaternion(rotation.X, rotation.Y, rotation.Z, rotation.W))),
                Is.EqualTo(1.0f).Within(0.000001f));
        });
    }

    [Test]
    public void TrackSampler_PartialTranslationChannel_PreservesNodeRotation()
    {
        var modelRoot = ModelRoot.CreateModel();
        var node = modelRoot.UseScene("default").CreateNode("root");
        var rotation = Numerics.Quaternion.CreateFromAxisAngle(Numerics.Vector3.UnitX, 0.5f);
        node.LocalMatrix = Numerics.Matrix4x4.CreateFromQuaternion(rotation);
        var animation = modelRoot.CreateAnimation("translation_only");
        animation.CreateTranslationChannel(node, new Dictionary<float, Numerics.Vector3>
        {
            [0] = Numerics.Vector3.Zero,
            [1] = Numerics.Vector3.One,
        });

        var sampled = GltfAnimationTrackSampler.SampleQuaternion(
            modelRoot,
            animation,
            "root",
            0.5f,
            Xna.Quaternion.Identity);

        Assert.Multiple(() =>
        {
            Assert.That(sampled.X, Is.EqualTo(rotation.X).Within(0.0001f));
            Assert.That(sampled.Y, Is.EqualTo(-rotation.Y).Within(0.0001f));
            Assert.That(sampled.Z, Is.EqualTo(-rotation.Z).Within(0.0001f));
            Assert.That(sampled.W, Is.EqualTo(rotation.W).Within(0.0001f));
        });
    }

    [TestCase(0)]
    [TestCase(-1)]
    public void Build_InvalidSamplingRate_Throws(float keysPerSecond)
    {
        var modelRoot = ModelRoot.CreateModel();
        var animation = modelRoot.CreateAnimation("test");
        var settings = new AnimationBuilderSettings(
            modelRoot,
            "test",
            keysPerSecond,
            PackFileContainer.CreatePackFile("test"),
            "animations");

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            AnimationBuilder.Build(settings, new AnimationFile(), animation));
    }

    [Test]
    public void Build_AnimationWithoutChannels_ThrowsInsteadOfCreatingBindPoseAnimation()
    {
        var modelRoot = ModelRoot.CreateModel();
        var animation = modelRoot.CreateAnimation("empty");
        var settings = new AnimationBuilderSettings(
            modelRoot,
            "test",
            20,
            PackFileContainer.CreatePackFile("test"),
            "animations",
            false);

        Assert.Throws<InvalidDataException>(() =>
            AnimationBuilder.Build(
                settings,
                CreateSingleBoneSkeleton(),
                animation));
    }

    [Test]
    public void Build_AnimationExceedsTransformBudget_ThrowsBeforeAllocatingFrames()
    {
        var modelRoot = ModelRoot.CreateModel();
        var node = modelRoot.UseScene("default").CreateNode("root");
        var animation = modelRoot.CreateAnimation("too_long");
        animation.CreateTranslationChannel(
            node,
            new Dictionary<float, Numerics.Vector3>
            {
                [0] = Numerics.Vector3.Zero,
                [5_000_001] = Numerics.Vector3.One,
            });
        var settings = new AnimationBuilderSettings(
            modelRoot,
            "test",
            1,
            PackFileContainer.CreatePackFile("test"),
            "animations",
            false);

        Assert.Throws<InvalidDataException>(() =>
            AnimationBuilder.Build(
                settings,
                CreateSingleBoneSkeleton(),
                animation));
    }

    [Test]
    public void Build_CancelledBeforeSampling_ThrowsOperationCancelled()
    {
        var modelRoot = ModelRoot.CreateModel();
        var node = modelRoot.UseScene("default").CreateNode("root");
        var animation = modelRoot.CreateAnimation("cancelled");
        animation.CreateTranslationChannel(
            node,
            new Dictionary<float, Numerics.Vector3>
            {
                [0] = Numerics.Vector3.Zero,
                [1] = Numerics.Vector3.One,
            });
        var settings = new AnimationBuilderSettings(
            modelRoot,
            "test",
            20,
            PackFileContainer.CreatePackFile("test"),
            "animations",
            false);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Assert.Throws<OperationCanceledException>(() =>
            AnimationBuilder.Build(
                settings,
                CreateSingleBoneSkeleton(),
                animation,
                cancellationToken: cancellation.Token));
    }

    private static AnimationFile CreateSingleBoneSkeleton()
    {
        var frame = new AnimationFile.Frame
        {
            Transforms = [new Shared.GameFormats.RigidModel.Transforms.RmvVector3(0, 0, 0)],
            Quaternion = [new Shared.GameFormats.RigidModel.Transforms.RmvVector4(0, 0, 0, 1)],
        };
        var part = new AnimationFile.AnimationPart
        {
            DynamicFrames = [frame],
        };
        part.TranslationMappings.Add(new AnimationFile.AnimationBoneMapping(0));
        part.RotationMappings.Add(new AnimationFile.AnimationBoneMapping(0));

        return new AnimationFile
        {
            Header = new AnimationFile.AnimationHeader
            {
                FrameRate = 20.0f,
                SkeletonName = "test",
            },
            Bones =
            [
                new AnimationFile.BoneInfo
                {
                    Id = 0,
                    ParentId = -1,
                    Name = "root",
                },
            ],
            AnimationParts = [part],
        };
    }

    private static VertexBuilder<
        VertexPositionNormalTangent,
        VertexTexture1,
        VertexJoints4> CreateSkinnedVertex(Numerics.Vector3 position)
    {
        var vertex = new VertexBuilder<
            VertexPositionNormalTangent,
            VertexTexture1,
            VertexJoints4>();
        vertex.Geometry.Position = position;
        vertex.Geometry.Normal = Numerics.Vector3.UnitZ;
        vertex.Geometry.Tangent = new Numerics.Vector4(1, 0, 0, 1);
        vertex.Skinning.SetBindings((0, 1), (0, 0), (0, 0), (0, 0));
        return vertex;
    }
}
