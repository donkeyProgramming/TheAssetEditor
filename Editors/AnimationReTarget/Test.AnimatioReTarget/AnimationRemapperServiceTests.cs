using Editors.AnimatioReTarget.Editor;
using Editors.AnimatioReTarget.Editor.BoneHandling;
using Editors.AnimatioReTarget.Editor.BoneHandling.Presentation;
using Editors.AnimatioReTarget.Editor.Settings;
using GameWorld.Core.Animation;
using GameWorld.Core.Services;
using Microsoft.Xna.Framework;
using Moq;
using Shared.Core.Misc;
using Shared.Core.Services;
using Shared.GameFormats.Animation;

namespace Test.AnimatioReTarget;

public class AnimationRemapperServiceTests
{
    [Test]
    public void ReMapAnimation_RootTrackMovingAwayFromBody_DoesNotJumpAtTheContactBoundary()
    {
        var file = CreateSkeletonFile("source", ("motion", -1), ("hips", 0),
            ("shoulder", 1), ("hand", 2), ("attachment", 0));
        var source = GameSkeleton.CreateFromAnimationFile(file, new AnimationPlayer());
        source.Translation[1] = Vector3.UnitY;
        source.Translation[2] = Vector3.UnitY;
        source.Translation[3] = Vector3.UnitX;
        source.RebuildSkeletonMatrix();
        var target = source.Clone();
        target.Translation[1] *= 2;
        target.Translation[2] *= 2.5f;
        target.Translation[3] *= 1.2f;
        target.RebuildSkeletonMatrix();
        var animation = CreateAnimation(100, source.BoneCount, 5);
        for (var frameIndex = 0; frameIndex < animation.DynamicFrames.Count; frameIndex++)
        {
            animation.DynamicFrames[frameIndex].Position = source.Translation.ToList();
            animation.DynamicFrames[frameIndex].Position[4] = new Vector3(1 + frameIndex * 0.1f, 2, 0);
        }
        var bones = SkeletonBoneNodeHelper.Build(file);
        foreach (var bone in EnumerateNodes(bones))
        {
            bone.HasMapping = true;
            bone.MappedIndex = bone.BoneIndex;
        }

        var result = new AnimationRemapperService(new AnimationGenerationSettings { ApplyRelativeScale = false }, bones)
            .ReMapAnimation(source, target, animation);

        for (var frameIndex = 1; frameIndex < result.DynamicFrames.Count; frameIndex++)
            Assert.That(Vector3.Distance(result.DynamicFrames[frameIndex].Position[4], result.DynamicFrames[frameIndex - 1].Position[4]),
                Is.LessThan(0.25f), $"attachment, frame {frameIndex}: continuous motion");
    }

    [TestCase(1.0f)]
    [TestCase(1.0001f)]
    public void ReMapAnimation_UnchangedFrameCount_PreservesOriginalKeyFramePositions(float speed)
    {
        var file = CreateSkeletonFile("source", ("motion", -1), ("body", 0));
        var source = GameSkeleton.CreateFromAnimationFile(file, new AnimationPlayer());
        source.Translation[1] = Vector3.UnitY;
        source.RebuildSkeletonMatrix();
        var target = source.Clone();
        target.Translation[1] *= 2;
        target.RebuildSkeletonMatrix();
        var animation = CreateAnimation(53, source.BoneCount, 0.533333f);
        for (var index = 0; index < animation.DynamicFrames.Count; index++)
        {
            animation.DynamicFrames[index].Position[0] = new Vector3(index % 2 == 0 ? 0 : 1000000, 0, 0);
            animation.DynamicFrames[index].Position[1] = Vector3.UnitY;
        }
        var bones = SkeletonBoneNodeHelper.Build(file);
        foreach (var bone in EnumerateNodes(bones))
        {
            bone.HasMapping = true;
            bone.MappedIndex = bone.BoneIndex;
        }

        var result = new AnimationRemapperService(new AnimationGenerationSettings { AnimationSpeedMult = speed }, bones)
            .ReMapAnimation(source, target, animation);

        Assert.That(result.DynamicFrames.Count, Is.EqualTo(animation.DynamicFrames.Count));
        for (var frame = 0; frame < result.DynamicFrames.Count; frame++)
            Assert.That(result.DynamicFrames[frame].Position[0], Is.EqualTo(animation.DynamicFrames[frame].Position[0]),
                $"motion, frame {frame}");
    }

    [TestCase(false, false)]
    [TestCase(true, false)]
    [TestCase(false, true)]
    [TestCase(true, true)]
    public void ReMapAnimation_BriefRootAttachmentTrack_KeepsRootSpaceWithoutImplicitHandSnapping(bool relativeScale, bool nestedRoot)
    {
        var file = CreateSkeletonFile("source", ("motion", -1), ("hips", 0),
            ("shoulder", 1), ("hand", 2), ("attachment_root", 0), ("attachment", nestedRoot ? 4 : 0));
        var source = GameSkeleton.CreateFromAnimationFile(file, new AnimationPlayer());
        source.Translation[1] = Vector3.UnitY;
        source.Translation[2] = Vector3.UnitY;
        source.Translation[3] = Vector3.UnitX;
        source.RebuildSkeletonMatrix();
        var target = source.Clone();
        target.Translation[1] *= 2;
        target.Translation[2] *= 2.5f;
        target.Translation[3] *= 1.2f;
        target.RebuildSkeletonMatrix();
        var animation = CreateAnimation(60, source.BoneCount, 3);
        foreach (var frame in animation.DynamicFrames)
            frame.Position = source.Translation.ToList();
        animation.DynamicFrames[3].Position[5] = new Vector3(1, 2, 0);
        animation.DynamicFrames[4].Position[5] = new Vector3(1000, 3, 0);
        var bones = SkeletonBoneNodeHelper.Build(file);
        foreach (var bone in EnumerateNodes(bones))
        {
            bone.HasMapping = true;
            bone.MappedIndex = bone.BoneIndex;
        }

        var result = new AnimationRemapperService(new AnimationGenerationSettings { ApplyRelativeScale = relativeScale }, bones)
            .ReMapAnimation(source, target, animation);

        Assert.Multiple(() =>
        {
            for (var frameIndex = 0; frameIndex < result.DynamicFrames.Count; frameIndex++)
            {
                var pose = AnimationSampler.Sample(frameIndex, 0, target, result);
                var expected = animation.DynamicFrames[frameIndex].Position[5];
                Assert.That(Vector3.Distance(pose.GetSkeletonAnimatedWorld(target, 5).Translation, expected),
                    Is.LessThan(0.0001f), $"attachment, frame {frameIndex}");
            }
        });
    }

    [TestCase(0.5f)]
    [TestCase(2.0f)]
    public void ReMapAnimation_UniformSkeletonScale_MatchesUpstreamForBodyAndRootTracks(float scale)
    {
        var file = CreateSkeletonFile("source", ("motion", -1), ("hips", 0),
            ("shoulder", 1), ("hand", 2), ("attachment", 0), ("helper", 3), ("attachment_child", 4));
        var source = GameSkeleton.CreateFromAnimationFile(file, new AnimationPlayer());
        source.Translation[1] = Vector3.UnitY;
        source.Translation[2] = Vector3.UnitY;
        source.Translation[3] = Vector3.UnitX;
        source.Translation[6] = new Vector3(0.1f, 0.05f, 0.2f);
        source.RebuildSkeletonMatrix();
        var target = source.Clone();
        for (var index = 0; index < target.BoneCount; index++)
            target.Translation[index] *= scale;
        target.RebuildSkeletonMatrix();
        var animation = CreateAnimation(3, source.BoneCount, 1);
        for (var frameIndex = 0; frameIndex < animation.DynamicFrames.Count; frameIndex++)
        {
            var frame = animation.DynamicFrames[frameIndex];
            frame.Position = source.Translation.ToList();
            frame.Rotation[0] = Quaternion.CreateFromAxisAngle(Vector3.UnitY, frameIndex * 0.2f);
            frame.Position[0] = new Vector3(0.3f * frameIndex, 0, 0.4f * frameIndex);
            frame.Rotation[2] = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, frameIndex * 0.3f);
            frame.Position[5] = new Vector3(0.15f, 0.05f, 0.02f);
            var hand = AnimationSampler.Sample(frameIndex, 0, source, animation)
                .GetSkeletonAnimatedWorld(source, 3).Translation;
            frame.Position[4] = Vector3.Transform(hand, Matrix.Invert(
                Matrix.CreateFromQuaternion(frame.Rotation[0]) * Matrix.CreateTranslation(frame.Position[0])));
        }
        var bones = SkeletonBoneNodeHelper.Build(file);
        foreach (var bone in EnumerateNodes(bones))
        {
            bone.HasMapping = true;
            bone.MappedIndex = bone.BoneIndex;
        }

        var result = new AnimationRemapperService(new AnimationGenerationSettings(), bones)
            .ReMapAnimation(source, target, animation);

        AssertUpstreamPose(source, target, animation, result, bones, true);
    }

    private static void AssertUpstreamPose(GameSkeleton source, GameSkeleton target, AnimationClip animation,
        AnimationClip actual, IEnumerable<SkeletonBoneNode_new> bones, bool relativeScale)
    {
        var nodes = EnumerateNodes(bones).ToDictionary(bone => bone.BoneIndex);
        var mappings = Enumerable.Range(0, target.BoneCount)
            .Select(index => nodes[index].HasMapping ? nodes[index].MappedIndex : -1).ToArray();
        var expected = AnimationRemapperUpstreamCompatibilityTests.ConvertWithChannels(source, target, animation, mappings,
            relativeScale, Enumerable.Range(0, target.BoneCount).Select(index => nodes[index].ApplyTranslation).ToArray(),
            Enumerable.Range(0, target.BoneCount).Select(index => nodes[index].ApplyRotation).ToArray());
        AnimationRemapperUpstreamCompatibilityTests.AssertSamePose(target, expected, actual, 0.0001f);
    }

    private static IEnumerable<SkeletonBoneNode_new> EnumerateNodes(IEnumerable<SkeletonBoneNode_new> bones)
    {
        foreach (var bone in bones)
        {
            yield return bone;
            foreach (var child in EnumerateNodes(bone.Children))
                yield return child;
        }
    }

    [TestCase(false, 0.0f)]
    [TestCase(true, 0.0f)]
    [TestCase(false, 0.2f)]
    [TestCase(true, 0.2f)]
    public void ReMapAnimation_BranchedJointWithDifferentRestAngles_TransfersEveryChildPosition(
        bool relativeScale,
        float motion)
    {
        var sourceFile = CreateSkeletonFile("source", ("motion", -1), ("spine_2", 0),
            ("neck_0", 1), ("clav_left", 1), ("clav_right", 1), ("cape_0", 1));
        var targetFile = CreateSkeletonFile("target", ("motion", -1), ("spine_2", 0),
            ("neck_0", 1), ("clav_left", 1), ("clav_right", 1), ("cape_0", 1));
        var source = GameSkeleton.CreateFromAnimationFile(sourceFile, new AnimationPlayer());
        source.Translation[1] = Vector3.UnitY;
        source.Translation[2] = Vector3.UnitY;
        source.Translation[3] = new Vector3(0.5f, 0.3f, 0);
        source.Translation[4] = new Vector3(-0.5f, 0.3f, 0);
        source.Translation[5] = new Vector3(0, 0.4f, 0.5f);
        source.RebuildSkeletonMatrix();
        var target = GameSkeleton.CreateFromAnimationFile(targetFile, new AnimationPlayer());
        target.Translation[1] = Vector3.UnitY * 2;
        target.Translation[2] = Vector3.UnitY * 2;
        target.Translation[3] = new Vector3(1, 0.1f, 0.2f);
        target.Translation[4] = new Vector3(-1, 0.1f, 0.2f);
        target.Translation[5] = new Vector3(0, 0.3f, 1);
        target.Rotation[1] = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, 0.4f);
        target.RebuildSkeletonMatrix();
        var animation = CreateAnimation(2, source.BoneCount, 1);
        foreach (var frame in animation.DynamicFrames)
        {
            for (var index = 0; index < source.BoneCount; index++)
            {
                frame.Position[index] = source.Translation[index] +
                    (index > 1 ? Vector3.UnitZ * motion : Vector3.Zero);
                frame.Rotation[index] = source.Rotation[index];
            }
            frame.Rotation[1] = Quaternion.CreateFromYawPitchRoll(0.3f, -0.4f, 0.2f);
        }
        var bones = SkeletonBoneNodeHelper.Build(targetFile);
        var root = bones.Single();
        var joint = root.Children.Single();
        foreach (var bone in new[] { root, joint }.Concat(joint.Children))
        {
            bone.HasMapping = true;
            bone.MappedIndex = source.GetBoneIndexByName(bone.BoneName);
        }
        var service = new AnimationRemapperService(
            new AnimationGenerationSettings { ApplyRelativeScale = relativeScale }, bones);

        var result = service.ReMapAnimation(source, target, animation);

        AssertUpstreamPose(source, target, animation, result, bones, relativeScale);
    }

    [TestCase("lowerarm_roll_left_0")]
    [TestCase("leg_armour_left_0")]
    public void ReMapAnimation_DeformationBoneOffsets_DoNotRotateTheMainLimbAwayFromItsSource(string helperName)
    {
        var sourceFile = CreateSkeletonFile("source", ("root", -1), ("joint", 0), ("lowerleg_left", 1), (helperName, 1));
        var targetFile = CreateSkeletonFile("target", ("root", -1), ("joint", 0), ("lowerleg_left", 1), (helperName, 1));
        var source = GameSkeleton.CreateFromAnimationFile(sourceFile, new AnimationPlayer());
        source.Translation[1] = Vector3.UnitY;
        source.Translation[2] = Vector3.UnitX;
        source.Translation[3] = new Vector3(0.5f, 0.2f, 0);
        source.RebuildSkeletonMatrix();
        var target = GameSkeleton.CreateFromAnimationFile(targetFile, new AnimationPlayer());
        target.Translation[1] = Vector3.UnitY * 2;
        target.Translation[2] = Vector3.UnitX * 2;
        target.Translation[3] = new Vector3(0.5f, 0.3f, 0);
        target.Rotation[1] = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, -0.2f);
        target.RebuildSkeletonMatrix();
        var animation = CreateAnimation(2, 4, 1);
        foreach (var frame in animation.DynamicFrames)
        {
            for (var index = 0; index < source.BoneCount; index++)
                frame.Position[index] = source.Translation[index];
            frame.Rotation[1] = Quaternion.CreateFromYawPitchRoll(0.3f, 0.4f, -0.5f);
        }
        var bones = SkeletonBoneNodeHelper.Build(targetFile);
        var root = bones.Single();
        var joint = root.Children.Single();
        foreach (var bone in new[] { root, joint }.Concat(joint.Children))
        {
            bone.HasMapping = true;
            bone.MappedIndex = source.GetBoneIndexByName(bone.BoneName);
        }
        var service = new AnimationRemapperService(
            new AnimationGenerationSettings { ApplyRelativeScale = true }, bones);

        var result = service.ReMapAnimation(source, target, animation);

        var sourceFrame = AnimationSampler.Sample(0, 0, source, animation);
        var targetFrame = AnimationSampler.Sample(0, 0, target, result);
        var sourceVector = sourceFrame.GetSkeletonAnimatedWorld(source, 2).Translation -
            sourceFrame.GetSkeletonAnimatedWorld(source, 1).Translation;
        var targetVector = targetFrame.GetSkeletonAnimatedWorld(target, 2).Translation -
            targetFrame.GetSkeletonAnimatedWorld(target, 1).Translation;
        Assert.That(Vector3.Dot(Vector3.Normalize(sourceVector), Vector3.Normalize(targetVector)), Is.GreaterThan(0.99999f));
    }

    [TestCase(0.0f)]
    [TestCase(0.7f)]
    public void ReMapAnimation_CollapsedSourceJoint_UsesUpstreamActualParentLength(float helperMotion)
    {
        var sourceFile = CreateSkeletonFile("source", ("root", -1), ("joint", 0), ("helper", 1), ("tip", 2));
        var targetFile = CreateSkeletonFile("target", ("root", -1), ("joint", 0), ("tip", 1));
        var source = GameSkeleton.CreateFromAnimationFile(sourceFile, new AnimationPlayer());
        source.Translation[1] = Vector3.UnitY;
        source.Translation[2] = Vector3.UnitX * 0.5f;
        source.Translation[3] = Vector3.UnitX * 0.5f;
        source.Rotation[2] = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, 0.5f);
        source.RebuildSkeletonMatrix();
        var target = GameSkeleton.CreateFromAnimationFile(targetFile, new AnimationPlayer());
        target.Translation[1] = Vector3.UnitY * 2;
        target.Translation[2] = Vector3.UnitY * 3;
        target.Rotation[1] = Quaternion.CreateFromYawPitchRoll(0.3f, -0.4f, 0.2f);
        target.RebuildSkeletonMatrix();
        var animation = CreateAnimation(2, 4, 1);
        foreach (var frame in animation.DynamicFrames)
        {
            for (var index = 0; index < source.BoneCount; index++)
            {
                frame.Position[index] = source.Translation[index];
                frame.Rotation[index] = source.Rotation[index];
            }
            frame.Rotation[0] = Quaternion.CreateFromYawPitchRoll(0.2f, 0.1f, 0.3f);
            frame.Rotation[1] = Quaternion.CreateFromYawPitchRoll(-0.4f, 0.2f, 0.8f);
            frame.Rotation[2] = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, 0.5f + helperMotion);
        }
        var bones = SkeletonBoneNodeHelper.Build(targetFile);
        var root = bones.Single();
        var joint = root.Children.Single();
        foreach (var bone in new[] { root, joint, joint.Children.Single() })
        {
            bone.HasMapping = true;
            bone.MappedIndex = source.GetBoneIndexByName(bone.BoneName);
            bone.ApplyTranslation = true;
            bone.ApplyRotation = true;
        }
        var service = new AnimationRemapperService(
            new AnimationGenerationSettings { ApplyRelativeScale = true }, bones);

        var result = service.ReMapAnimation(source, target, animation);

        AssertUpstreamPose(source, target, animation, result, bones, true);
    }

    [TestCase(true)]
    [TestCase(false)]
    public void ReMapAnimation_UnmappedIntermediateJoint_KeepsBindChannelsWithoutImplicitMapping(bool applyRotation)
    {
        var sourceFile = CreateSkeletonFile("source", ("root", -1), ("leg", 0), ("ankle", 1), ("foot", 2));
        var targetFile = CreateSkeletonFile("target", ("root", -1), ("leg", 0), ("ankle", 1), ("foot", 2));
        var source = GameSkeleton.CreateFromAnimationFile(sourceFile, new AnimationPlayer());
        var target = GameSkeleton.CreateFromAnimationFile(targetFile, new AnimationPlayer());
        for (var index = 1; index < 4; index++)
        {
            source.Translation[index] = Vector3.UnitX;
            target.Translation[index] = Vector3.UnitX * 2;
        }
        source.RebuildSkeletonMatrix();
        target.RebuildSkeletonMatrix();
        var animation = CreateAnimation(2, 4, 1);
        foreach (var frame in animation.DynamicFrames)
        {
            for (var index = 1; index < 4; index++)
                frame.Position[index] = source.Translation[index];
            frame.Rotation[1] = Quaternion.CreateFromAxisAngle(Vector3.UnitY, 0.3f);
            frame.Rotation[2] = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, 1.1f);
        }
        var bones = SkeletonBoneNodeHelper.Build(targetFile);
        var root = bones.Single();
        var leg = root.Children.Single();
        var ankle = leg.Children.Single();
        var foot = ankle.Children.Single();
        foreach (var bone in new[] { root, leg, foot })
        {
            bone.HasMapping = true;
            bone.MappedIndex = source.GetBoneIndexByName(bone.BoneName);
            bone.ApplyTranslation = false;
        }
        ankle.ApplyRotation = applyRotation;
        var service = new AnimationRemapperService(
            new AnimationGenerationSettings { ApplyRelativeScale = true }, bones);

        var result = service.ReMapAnimation(source, target, animation);

        Assert.That(ankle.HasMapping, Is.False);
        AssertUpstreamPose(source, target, animation, result, bones, true);
        foreach (var frame in result.DynamicFrames)
        {
            Assert.That(frame.Position[2], Is.EqualTo(target.Translation[2]));
            AssertQuaternionEquivalent(frame.Rotation[2], target.Rotation[2]);
        }
    }

    [TestCase(false)]
    [TestCase(true)]
    public void ReMapAnimation_MultipleChildAxes_CopiesUpstreamOrientationRegardlessOfBoneOrder(bool reverseChildren)
    {
        var sourceFile = CreateSkeletonFile("source", ("root", -1), ("joint", 0), ("tip", 1), ("palm", 1));
        var targetFile = reverseChildren
            ? CreateSkeletonFile("target", ("root", -1), ("joint", 0), ("palm", 1), ("tip", 1))
            : CreateSkeletonFile("target", ("root", -1), ("joint", 0), ("tip", 1), ("palm", 1));
        var source = GameSkeleton.CreateFromAnimationFile(sourceFile, new AnimationPlayer());
        source.Translation[1] = Vector3.UnitY;
        source.Translation[2] = Vector3.UnitX;
        source.Translation[3] = Vector3.UnitY * 0.5f;
        source.RebuildSkeletonMatrix();
        var target = GameSkeleton.CreateFromAnimationFile(targetFile, new AnimationPlayer());
        var axes = Matrix.CreateFromYawPitchRoll(0.7f, -0.5f, 0.3f);
        target.Rotation[1] = Quaternion.CreateFromRotationMatrix(axes * Matrix.CreateFromYawPitchRoll(-0.4f, 0.6f, 0.5f));
        target.Translation[1] = Vector3.UnitY * 2;
        target.Translation[target.GetBoneIndexByName("tip")] = Vector3.TransformNormal(Vector3.UnitX * 3, Matrix.Transpose(axes));
        target.Translation[target.GetBoneIndexByName("palm")] = Vector3.TransformNormal(Vector3.UnitY, Matrix.Transpose(axes));
        target.RebuildSkeletonMatrix();
        var animation = CreateAnimation(2, 4, 1);
        foreach (var frame in animation.DynamicFrames)
        {
            for (var index = 0; index < source.BoneCount; index++)
                frame.Position[index] = source.Translation[index];
            frame.Rotation[0] = Quaternion.CreateFromYawPitchRoll(0.2f, 0.3f, 0.1f);
            frame.Rotation[1] = Quaternion.CreateFromYawPitchRoll(-0.4f, 0.2f, 0.8f);
        }
        var bones = SkeletonBoneNodeHelper.Build(targetFile);
        var root = bones.Single();
        foreach (var bone in new[] { root, root.Children.Single() }.Concat(root.Children.Single().Children))
        {
            bone.HasMapping = true;
            bone.MappedIndex = source.GetBoneIndexByName(bone.BoneName);
        }
        var service = new AnimationRemapperService(
            new AnimationGenerationSettings { ApplyRelativeScale = true }, bones);

        var result = service.ReMapAnimation(source, target, animation);

        AssertUpstreamPose(source, target, animation, result, bones, true);
    }

    [TestCase(0.0f)]
    [TestCase(0.00000001f)]
    [TestCase(0.000000000001f)]
    public void ReMapAnimation_NearlyZeroSourceBoneLength_UsesOriginalRatioAndHandlesExactZero(float sourceLength)
    {
        var source = CreateSkeleton("source", 2);
        source.Translation[1] = Vector3.UnitX * sourceLength;
        source.RebuildSkeletonMatrix();
        var target = CreateSkeleton("target", 2);
        target.Translation[1] = Vector3.UnitX * 2;
        target.RebuildSkeletonMatrix();
        var animation = CreateAnimation(2, 2, 1);
        foreach (var frame in animation.DynamicFrames)
        {
            frame.Rotation[0] = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, 0.7f);
            frame.Position[1] = source.Translation[1] + Vector3.UnitY * 0.25f;
        }
        var root = new SkeletonBoneNode_new("root", 0, -1) { HasMapping = true, MappedIndex = 0 };
        root.Children.Add(new SkeletonBoneNode_new("child", 1, 0) { HasMapping = true, MappedIndex = 1 });
        var service = new AnimationRemapperService(
            new AnimationGenerationSettings { ApplyRelativeScale = true }, [root]);

        var result = service.ReMapAnimation(source, target, animation);

        var expected = animation.DynamicFrames[0].Position[1] * (sourceLength == 0 ? 1 : 2 / sourceLength);
        Assert.That(float.IsFinite(result.DynamicFrames[0].Position[1].Length()), Is.True);
        Assert.That(Vector3.Distance(result.DynamicFrames[0].Position[1], expected),
            Is.LessThan(0.0001f + expected.Length() * 0.000001f));
    }

    [TestCase(90)]
    [TestCase(-90)]
    [TestCase(180)]
    public void ReMapAnimation_DifferentBoneRollAxes_CopiesUpstreamWorldOrientation(float rollAngle)
    {
        var source = CreateSkeleton("source", 3);
        source.Translation[1] = Vector3.UnitY;
        source.Translation[2] = Vector3.UnitX;
        source.RebuildSkeletonMatrix();
        var target = CreateSkeleton("target", 3);
        var axisRoll = Matrix.CreateRotationX(MathHelper.ToRadians(rollAngle));
        target.Rotation[1] = Quaternion.CreateFromRotationMatrix(axisRoll * Matrix.CreateRotationZ(-0.6f));
        target.Translation[1] = Vector3.UnitY * 2;
        target.Translation[2] = Vector3.UnitX * 3;
        target.RebuildSkeletonMatrix();
        var animation = CreateAnimation(2, 3, 1);
        foreach (var frame in animation.DynamicFrames)
        {
            frame.Position[1] = source.Translation[1];
            frame.Position[2] = source.Translation[2];
            frame.Rotation[0] = Quaternion.CreateFromYawPitchRoll(0.2f, 0.3f, 0.1f);
            frame.Rotation[1] = Quaternion.CreateFromYawPitchRoll(-0.4f, 0.2f, 0.8f);
        }
        var root = new SkeletonBoneNode_new("root", 0, -1) { HasMapping = true, MappedIndex = 0 };
        var joint = new SkeletonBoneNode_new("joint", 1, 0) { HasMapping = true, MappedIndex = 1 };
        joint.Children.Add(new SkeletonBoneNode_new("end", 2, 1) { HasMapping = true, MappedIndex = 2 });
        root.Children.Add(joint);
        var service = new AnimationRemapperService(
            new AnimationGenerationSettings { ApplyRelativeScale = true }, [root]);

        var result = service.ReMapAnimation(source, target, animation);

        AssertUpstreamPose(source, target, animation, result, [root], true);
    }

    [TestCase(90)]
    [TestCase(-90)]
    [TestCase(180)]
    public void ReMapAnimation_DifferentLocalBoneAxes_AlignsPoseInTargetRootSpace(float axisAngle)
    {
        var source = CreateSkeleton("source", 3);
        source.Rotation[0] = Quaternion.CreateFromYawPitchRoll(0.3f, 0.2f, 0.4f);
        source.Rotation[1] = Quaternion.CreateFromAxisAngle(Vector3.UnitY, -0.4f);
        source.Translation[1] = Vector3.UnitY;
        source.Translation[2] = Vector3.UnitX;
        source.RebuildSkeletonMatrix();
        var target = CreateSkeleton("target", 3);
        target.Rotation[0] = Quaternion.CreateFromYawPitchRoll(-0.2f, 0.5f, 0.7f);
        target.Rotation[1] = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, 0.6f);
        target.Translation[1] = Vector3.UnitY * 2;
        target.Translation[2] = Vector3.Transform(Vector3.UnitX * 3,
            Quaternion.CreateFromAxisAngle(Vector3.UnitZ, MathHelper.ToRadians(axisAngle)));
        target.RebuildSkeletonMatrix();
        var animation = CreateAnimation(2, 3, 1);
        foreach (var frame in animation.DynamicFrames)
        {
            frame.Position[0] = new Vector3(4, 5, 6);
            frame.Position[1] = source.Translation[1];
            frame.Position[2] = source.Translation[2];
            frame.Rotation[0] = Quaternion.CreateFromYawPitchRoll(0.7f, -0.2f, 0.5f);
            frame.Rotation[1] = Quaternion.CreateFromYawPitchRoll(-0.5f, 0.4f, -0.6f);
        }
        var root = new SkeletonBoneNode_new("root", 0, -1) { HasMapping = true, MappedIndex = 0 };
        var joint = new SkeletonBoneNode_new("joint", 1, 0) { HasMapping = true, MappedIndex = 1 };
        joint.Children.Add(new SkeletonBoneNode_new("end", 2, 1) { HasMapping = true, MappedIndex = 2 });
        root.Children.Add(joint);
        var service = new AnimationRemapperService(
            new AnimationGenerationSettings { ApplyRelativeScale = true }, [root]);

        var result = service.ReMapAnimation(source, target, animation);

        var sourceFrame = AnimationSampler.Sample(0, 0, source, animation);
        var targetFrame = AnimationSampler.Sample(0, 0, target, result);
        var sourceDirection = sourceFrame.GetSkeletonAnimatedWorld(source, 2).Translation -
            sourceFrame.GetSkeletonAnimatedWorld(source, 1).Translation;
        var targetDirection = targetFrame.GetSkeletonAnimatedWorld(target, 2).Translation -
            targetFrame.GetSkeletonAnimatedWorld(target, 1).Translation;
        var rootSpaceConversion = Matrix.Invert(sourceFrame.GetSkeletonAnimatedWorld(source, 0)) *
            targetFrame.GetSkeletonAnimatedWorld(target, 0);
        var expectedDirection = Vector3.Normalize(Vector3.TransformNormal(sourceDirection, rootSpaceConversion));
        Assert.Multiple(() =>
        {
            Assert.That(Vector3.Dot(Vector3.Normalize(targetDirection), expectedDirection), Is.GreaterThan(0.99999f));
            Assert.That(targetDirection.Length(), Is.EqualTo(3).Within(0.0001f));
            Assert.That(Vector3.Distance(result.DynamicFrames[0].Position[0], new Vector3(4, 5, 6)), Is.LessThan(0.0001f));
        });
    }

    [TestCase(false, 0.0f)]
    [TestCase(true, 0.0f)]
    [TestCase(false, 0.25f)]
    [TestCase(true, 0.25f)]
    public void ReMapAnimation_AllBonesApplyTranslationAndRotation_ScalesCompleteSourcePositionsWhenEnabled(
        bool applyRelativeScale,
        float translationDelta)
    {
        var sourceSkeleton = CreateSkeleton("source", 3);
        sourceSkeleton.Translation[1] = Vector3.UnitX;
        sourceSkeleton.Translation[2] = Vector3.UnitY;
        sourceSkeleton.RebuildSkeletonMatrix();
        var targetSkeleton = CreateSkeleton("target", 3);
        targetSkeleton.Translation[1] = Vector3.UnitX * 2;
        targetSkeleton.Translation[2] = Vector3.UnitY * 3;
        targetSkeleton.RebuildSkeletonMatrix();
        var animation = CreateAnimation(2, 3, 1.0f);
        foreach (var frame in animation.DynamicFrames)
        {
            frame.Position[0] = new Vector3(4, 5, 6);
            frame.Rotation[0] = Quaternion.CreateFromAxisAngle(Vector3.UnitY, 0.5f);
            frame.Position[1] = Vector3.UnitX + Vector3.UnitY * translationDelta;
            frame.Rotation[1] = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, 1.0f);
            frame.Position[2] = Vector3.UnitY;
            frame.Rotation[2] = Quaternion.CreateFromAxisAngle(Vector3.UnitX, 0.75f);
        }
        var root = new SkeletonBoneNode_new("root", 0, -1)
        {
            HasMapping = true,
            MappedIndex = 0,
            ApplyTranslation = true,
            ApplyRotation = true,
        };
        var child = new SkeletonBoneNode_new("child", 1, 0)
        {
            HasMapping = true,
            MappedIndex = 1,
            ApplyTranslation = true,
            ApplyRotation = true,
        };
        child.Children.Add(new SkeletonBoneNode_new("grandchild", 2, 1)
        {
            HasMapping = true,
            MappedIndex = 2,
            ApplyTranslation = true,
            ApplyRotation = true,
        });
        root.Children.Add(child);
        var service = new AnimationRemapperService(
            new AnimationGenerationSettings { ApplyRelativeScale = applyRelativeScale },
            [root]);

        var result = service.ReMapAnimation(sourceSkeleton, targetSkeleton, animation);

        var expectedChildPosition = (Vector3.UnitX + Vector3.UnitY * translationDelta) * (applyRelativeScale ? 2 : 1);
        foreach (var frame in result.DynamicFrames)
        {
            Assert.Multiple(() =>
            {
                Assert.That(Vector3.Distance(frame.Position[0], new Vector3(4, 5, 6)), Is.LessThan(0.0001f));
                Assert.That(Vector3.Distance(frame.Position[1], expectedChildPosition), Is.LessThan(0.0001f));
                Assert.That(Vector3.Distance(frame.Position[2], Vector3.UnitY * (applyRelativeScale ? 3 : 1)), Is.LessThan(0.0001f));
                for (var boneIndex = 0; boneIndex < 3; boneIndex++)
                    AssertQuaternionEquivalent(frame.Rotation[boneIndex], animation.DynamicFrames[0].Rotation[boneIndex]);
            });
        }
    }

    [Test]
    public void ReMapAnimation_TranslationDelta_UsesAnimatedTargetParentSpace()
    {
        var sourceSkeleton = CreateSkeleton("source", 2);
        sourceSkeleton.Translation[1] = Vector3.UnitX;
        sourceSkeleton.RebuildSkeletonMatrix();
        var targetSkeleton = CreateSkeleton("target", 2);
        targetSkeleton.Rotation[0] = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, MathHelper.PiOver2);
        targetSkeleton.Translation[1] = Vector3.UnitX * 2;
        targetSkeleton.RebuildSkeletonMatrix();
        var animation = CreateAnimation(2, 2, 1.0f);
        foreach (var frame in animation.DynamicFrames)
        {
            frame.Rotation[0] = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, 0.5f);
            frame.Position[1] = Vector3.UnitX + Vector3.UnitY * 0.25f;
            frame.Rotation[1] = Quaternion.CreateFromAxisAngle(Vector3.UnitX, 1.0f);
        }
        var root = new SkeletonBoneNode_new("root", 0, -1) { HasMapping = true, MappedIndex = 0 };
        root.Children.Add(new SkeletonBoneNode_new("child", 1, 0) { HasMapping = true, MappedIndex = 1 });
        var service = new AnimationRemapperService(
            new AnimationGenerationSettings { ApplyRelativeScale = false },
            [root]);

        var result = service.ReMapAnimation(sourceSkeleton, targetSkeleton, animation);

        Assert.That(Vector3.Distance(result.DynamicFrames[0].Position[1], Vector3.UnitX + Vector3.UnitY * 0.25f), Is.LessThan(0.0001f));
    }

    [Test]
    public void ReMapAnimation_TargetRootMappedToSourceChild_InheritsSourceParentTranslation()
    {
        var sourceSkeleton = CreateSkeleton("source", 2);
        sourceSkeleton.Translation[1] = Vector3.UnitX;
        sourceSkeleton.RebuildSkeletonMatrix();
        var targetSkeleton = CreateSkeleton("target", 1);
        targetSkeleton.Translation[0] = new Vector3(8, 9, 10);
        targetSkeleton.RebuildSkeletonMatrix();
        var animation = CreateAnimation(2, 2, 1.0f);
        foreach (var frame in animation.DynamicFrames)
        {
            frame.Position[0] = new Vector3(3, 4, 5);
            frame.Position[1] = Vector3.UnitX;
            frame.Rotation[1] = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, 1.0f);
        }
        var root = new SkeletonBoneNode_new("root", 0, -1) { HasMapping = true, MappedIndex = 1 };
        var service = new AnimationRemapperService(
            new AnimationGenerationSettings { ApplyRelativeScale = false },
            [root]);

        var result = service.ReMapAnimation(sourceSkeleton, targetSkeleton, animation);

        Assert.That(Vector3.Distance(result.DynamicFrames[0].Position[0], new Vector3(4, 4, 5)), Is.LessThan(0.0001f));
    }

    [Test]
    public void ReMapAnimation_UnmappedSourceIntermediateBone_PreservesItsTranslation()
    {
        var sourceSkeleton = CreateSkeleton("source", 3);
        sourceSkeleton.Translation[1] = Vector3.UnitY;
        sourceSkeleton.Translation[2] = Vector3.UnitX;
        sourceSkeleton.RebuildSkeletonMatrix();
        var targetSkeleton = CreateSkeleton("target", 2);
        targetSkeleton.Translation[1] = Vector3.UnitX * 2;
        targetSkeleton.RebuildSkeletonMatrix();
        var animation = CreateAnimation(2, 3, 1.0f);
        foreach (var frame in animation.DynamicFrames)
        {
            frame.Position[1] = Vector3.UnitY * 1.5f;
            frame.Position[2] = Vector3.UnitX;
            frame.Rotation[2] = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, 1.0f);
        }
        var root = new SkeletonBoneNode_new("root", 0, -1) { HasMapping = true, MappedIndex = 0 };
        root.Children.Add(new SkeletonBoneNode_new("child", 1, 0) { HasMapping = true, MappedIndex = 2 });
        var service = new AnimationRemapperService(
            new AnimationGenerationSettings { ApplyRelativeScale = false },
            [root]);

        var result = service.ReMapAnimation(sourceSkeleton, targetSkeleton, animation);

        Assert.That(Vector3.Distance(result.DynamicFrames[0].Position[1], Vector3.UnitX + Vector3.UnitY * 1.5f), Is.LessThan(0.0001f));
    }

    [Test]
    public void ReMapAnimation_SourceAtBindPose_CopiesSourceWorldOrientation()
    {
        var sourceSkeleton = CreateSkeleton("source", 1);
        sourceSkeleton.Rotation[0] = Quaternion.CreateFromAxisAngle(
            Vector3.UnitX,
            MathHelper.ToRadians(90));
        sourceSkeleton.RebuildSkeletonMatrix();

        var targetSkeleton = CreateSkeleton("target", 1);
        var targetBindRotation = Quaternion.CreateFromAxisAngle(
            Vector3.UnitZ,
            MathHelper.ToRadians(-90));
        targetSkeleton.Rotation[0] = targetBindRotation;
        targetSkeleton.RebuildSkeletonMatrix();

        var animation = CreateAnimation(2, 1, 1.0f);
        foreach (var frame in animation.DynamicFrames)
            frame.Rotation[0] = sourceSkeleton.Rotation[0];

        var targetRoot = new SkeletonBoneNode_new("target_root", 0, -1)
        {
            HasMapping = true,
            MappedIndex = 0,
        };
        var service = new AnimationRemapperService(
            new AnimationGenerationSettings { ApplyRelativeScale = false },
            [targetRoot]);

        var result = service.ReMapAnimation(
            sourceSkeleton,
            targetSkeleton,
            animation);

        var actual = Quaternion.Normalize(result.DynamicFrames[0].Rotation[0]);
        var expected = Quaternion.Normalize(sourceSkeleton.Rotation[0]);
        Assert.That(MathF.Abs(Quaternion.Dot(actual, expected)), Is.GreaterThan(0.9999f));
    }

    [Test]
    public void ReMapAnimation_RelativeScale_DoesNotScaleTargetBindTranslationTwice()
    {
        var sourceSkeleton = CreateSkeleton("source", 2);
        sourceSkeleton.Translation[1] = new Vector3(1, 0, 0);
        sourceSkeleton.RebuildSkeletonMatrix();
        var targetSkeleton = CreateSkeleton("target", 2);
        targetSkeleton.Translation[1] = new Vector3(2, 0, 0);
        targetSkeleton.RebuildSkeletonMatrix();
        var animation = CreateAnimation(2, 2, 1.0f);
        foreach (var frame in animation.DynamicFrames)
        {
            frame.Position[0] = sourceSkeleton.Translation[0];
            frame.Position[1] = sourceSkeleton.Translation[1];
        }

        var targetRoot = new SkeletonBoneNode_new("target_root", 0, -1)
        {
            HasMapping = true,
            MappedIndex = 0,
        };
        targetRoot.Children.Add(new SkeletonBoneNode_new("target_child", 1, 0)
        {
            HasMapping = true,
            MappedIndex = 1,
        });
        var service = new AnimationRemapperService(
            new AnimationGenerationSettings { ApplyRelativeScale = true },
            [targetRoot]);

        var result = service.ReMapAnimation(
            sourceSkeleton,
            targetSkeleton,
            animation);

        Assert.That(
            Vector3.Distance(
                result.DynamicFrames[0].Position[1],
                new Vector3(2, 0, 0)),
            Is.LessThan(0.0001f));
    }

    [Test]
    public void ReMapAnimation_SnapWorldspace_CopiesSourcePoseInCurrentTargetParentSpace()
    {
        var sourceSkeleton = CreateSkeleton("source", 2);
        sourceSkeleton.Translation[1] = new Vector3(1, 0, 0);
        sourceSkeleton.Rotation[1] = Quaternion.CreateFromAxisAngle(
            Vector3.UnitX,
            MathHelper.ToRadians(90));
        sourceSkeleton.RebuildSkeletonMatrix();

        var targetSkeleton = CreateSkeleton("target", 2);
        var targetBindTranslation = new Vector3(2, 0, 0);
        var targetBindRotation = Quaternion.CreateFromAxisAngle(
            Vector3.UnitZ,
            MathHelper.ToRadians(-90));
        targetSkeleton.Translation[1] = targetBindTranslation;
        targetSkeleton.Rotation[1] = targetBindRotation;
        targetSkeleton.RebuildSkeletonMatrix();

        var animation = CreateAnimation(2, 2, 1.0f);
        foreach (var frame in animation.DynamicFrames)
        {
            frame.Position[0] = sourceSkeleton.Translation[0];
            frame.Rotation[0] = sourceSkeleton.Rotation[0];
            frame.Position[1] = sourceSkeleton.Translation[1];
            frame.Rotation[1] = sourceSkeleton.Rotation[1];
        }

        var targetRoot = new SkeletonBoneNode_new("target_root", 0, -1)
        {
            HasMapping = true,
            MappedIndex = 0,
        };
        targetRoot.Children.Add(new SkeletonBoneNode_new("target_child", 1, 0)
        {
            HasMapping = true,
            MappedIndex = 1,
            ForceSnapToWorld = true,
        });
        var service = new AnimationRemapperService(
            new AnimationGenerationSettings { ApplyRelativeScale = false },
            [targetRoot]);

        var result = service.ReMapAnimation(
            sourceSkeleton,
            targetSkeleton,
            animation);

        var actualRotation = Quaternion.Normalize(result.DynamicFrames[0].Rotation[1]);
        Assert.Multiple(() =>
        {
            Assert.That(
                MathF.Abs(Quaternion.Dot(actualRotation, sourceSkeleton.Rotation[1])),
                Is.GreaterThan(0.9999f));
            Assert.That(
                Vector3.Distance(
                    result.DynamicFrames[0].Position[1],
                    sourceSkeleton.Translation[1]),
                Is.LessThan(0.0001f));
        });
    }

    [Test]
    public void BoneManager_TargetHasMoreBones_BuildsSettingsForEveryTargetBone()
    {
        var sourceFile = CreateSkeletonFile("source", 2, "source_bone");
        var targetFile = CreateSkeletonFile("target", 4, "target_bone");
        var lookup = new Mock<ISkeletonAnimationLookUpHelper>();
        lookup.Setup(x => x.GetSkeletonFileFromName("source")).Returns(sourceFile);
        lookup.Setup(x => x.GetSkeletonFileFromName("target")).Returns(targetFile);
        var manager = new BoneManager(
            Mock.Of<IStandardDialogs>(),
            Mock.Of<IAbstractFormFactory<BoneMappingWindow>>(),
            lookup.Object);

        manager.UpdateSourceSkeleton("source");
        manager.UpdateTargetSkeleton("target");

        Assert.Multiple(() =>
        {
            Assert.That(manager.FlatBones, Has.Count.EqualTo(4));
            Assert.That(
                manager.FlatBones.Select(x => x.BoneName),
                Is.EqualTo(new[]
                {
                    "target_bone_0",
                    "target_bone_1",
                    "target_bone_2",
                    "target_bone_3",
                }));
        });
    }

    [Test]
    public void ReMapAnimation_ThreeSourceFingerChains_DriveFiveTargetFingerChains()
    {
        var sourceFile = CreateSkeletonFile(
            "source",
            ("animroot", -1),
            ("root", 0),
            ("upperleg_left", 1),
            ("upperleg_right", 1),
            ("hand_left", 1),
            ("finger_index_left_0", 4),
            ("finger_index_left_1", 5),
            ("finger_index_left_2", 6),
            ("finger_ring_left_0", 4),
            ("finger_ring_left_1", 8),
            ("finger_ring_left_2", 9),
            ("thumb_left_0", 4),
            ("thumb_left_1", 11),
            ("thumb_left_2", 12));
        var targetFile = CreateSkeletonFile(
            "target",
            ("root", -1),
            ("pelvis", 0),
            ("thigh_l", 1),
            ("thigh_r", 1),
            ("hand_l", 1),
            ("index_01_l", 4),
            ("index_02_l", 5),
            ("index_03_l", 6),
            ("middle_01_l", 4),
            ("middle_02_l", 8),
            ("middle_03_l", 9),
            ("ring_01_l", 4),
            ("ring_02_l", 11),
            ("ring_03_l", 12),
            ("pinky_01_l", 4),
            ("pinky_02_l", 14),
            ("pinky_03_l", 15),
            ("thumb_01_l", 4),
            ("thumb_02_l", 17),
            ("thumb_03_l", 18));
        var lookup = new Mock<ISkeletonAnimationLookUpHelper>();
        lookup.Setup(x => x.GetSkeletonFileFromName("source")).Returns(sourceFile);
        lookup.Setup(x => x.GetSkeletonFileFromName("target")).Returns(targetFile);
        var manager = new BoneManager(
            Mock.Of<IStandardDialogs>(),
            Mock.Of<IAbstractFormFactory<BoneMappingWindow>>(),
            lookup.Object);
        manager.UpdateSourceSkeleton("source");
        manager.UpdateTargetSkeleton("target");
        SetBoneMappings(
            manager,
            (0, 0), (1, 1), (2, 2), (3, 3), (4, 4),
            (5, 5), (6, 6), (7, 7),
            (8, 8), (9, 9), (10, 10),
            (11, 8), (12, 9), (13, 10),
            (14, 8), (15, 9), (16, 10),
            (17, 11), (18, 12), (19, 13));

        var sourceSkeleton = GameSkeleton.CreateFromAnimationFile(
            sourceFile,
            new AnimationPlayer());
        var targetSkeleton = GameSkeleton.CreateFromAnimationFile(
            targetFile,
            new AnimationPlayer());
        var sourceAnimation = CreateAnimation(2, sourceFile.Bones.Length, 1.0f);
        var ringRotations = new[]
        {
            Quaternion.CreateFromAxisAngle(Vector3.UnitX, MathHelper.ToRadians(15)),
            Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathHelper.ToRadians(25)),
            Quaternion.CreateFromAxisAngle(Vector3.UnitZ, MathHelper.ToRadians(35)),
        };
        for (var segment = 0; segment < ringRotations.Length; segment++)
            sourceAnimation.DynamicFrames[1].Rotation[8 + segment] = ringRotations[segment];
        var service = new AnimationRemapperService(
            new AnimationGenerationSettings { ApplyRelativeScale = false },
            manager.Bones);

        var result = service.ReMapAnimation(
            sourceSkeleton,
            targetSkeleton,
            sourceAnimation);

        Assert.Multiple(() =>
        {
            for (var segment = 0; segment < ringRotations.Length; segment++)
            {
                AssertQuaternionEquivalent(
                    result.DynamicFrames[1].Rotation[8 + segment],
                    ringRotations[segment]);
                AssertQuaternionEquivalent(
                    result.DynamicFrames[1].Rotation[11 + segment],
                    ringRotations[segment]);
                AssertQuaternionEquivalent(
                    result.DynamicFrames[1].Rotation[14 + segment],
                    ringRotations[segment]);
            }
        });
    }

    [Test]
    public void ReMapAnimation_ManualMappingTransfersWeaponMotion()
    {
        var sourceFile = CreateSkeletonFile(
            "source",
            ("animroot", -1),
            ("root", 0),
            ("spine_0", 1),
            ("clav_left", 2),
            ("upperarm_left", 3),
            ("lowerarm_left", 4),
            ("hand_left", 5),
            ("upperleg_left", 1),
            ("upperleg_right", 1),
            ("unused", 1),
            ("weapon_1", 0));
        var targetFile = CreateSkeletonFile(
            "target",
            ("root", -1),
            ("pelvis", 0),
            ("Bip", 1),
            ("spine_01", 2),
            ("clavicle_l", 3),
            ("upperarm_l", 4),
            ("lowerarm_l", 5),
            ("hand_l", 6),
            ("weapon_r", 7),
            ("weapon_helper", 8),
            ("weapon_root_ji", 9));
        var lookup = new Mock<ISkeletonAnimationLookUpHelper>();
        lookup.Setup(x => x.GetSkeletonFileFromName("source")).Returns(sourceFile);
        lookup.Setup(x => x.GetSkeletonFileFromName("target")).Returns(targetFile);
        var manager = new BoneManager(
            Mock.Of<IStandardDialogs>(),
            Mock.Of<IAbstractFormFactory<BoneMappingWindow>>(),
            lookup.Object);
        manager.UpdateSourceSkeleton("source");
        manager.UpdateTargetSkeleton("target");
        var weaponRoot = BoneHelper_new.GetBoneFromId(manager.Bones, 10)!;
        weaponRoot.HasMapping = true;
        weaponRoot.MappedIndex = 10;
        SetBoneMappings(
            manager,
            (0, 0), (2, 1), (3, 2), (4, 3), (5, 4), (6, 5), (7, 6));

        var sourceSkeleton = GameSkeleton.CreateFromAnimationFile(
            sourceFile,
            new AnimationPlayer());
        var targetSkeleton = GameSkeleton.CreateFromAnimationFile(
            targetFile,
            new AnimationPlayer());
        var sourceAnimation = CreateAnimation(2, sourceFile.Bones.Length, 1.0f);
        var weaponRotation = Quaternion.CreateFromYawPitchRoll(
            MathHelper.ToRadians(20),
            MathHelper.ToRadians(35),
            MathHelper.ToRadians(-15));
        sourceAnimation.DynamicFrames[1].Rotation[10] = weaponRotation;
        var service = new AnimationRemapperService(
            new AnimationGenerationSettings { ApplyRelativeScale = false },
            manager.Bones);

        var result = service.ReMapAnimation(
            sourceSkeleton,
            targetSkeleton,
            sourceAnimation);

        Assert.Multiple(() =>
        {
            Assert.That(BoneHelper_new.GetMappedIndex(manager.Bones, 10), Is.EqualTo(10));
            AssertQuaternionEquivalent(
                result.DynamicFrames[1].Rotation[10],
                weaponRotation);
        });
    }

    [Test]
    public void ReMapAnimation_RelativeBoneIsUnmapped_SkipsAttachmentAdjustment()
    {
        var sourceSkeleton = CreateSkeleton("source", 2);
        var targetSkeleton = CreateSkeleton("target", 2);
        var animation = CreateAnimation(2, 2, 1.0f);
        var root = new SkeletonBoneNode_new("root", 0, -1);
        var attachment = new SkeletonBoneNode_new("attachment", 1, 0)
        {
            HasMapping = true,
            MappedIndex = 1,
            SelectedRelativeBone = root,
        };
        root.Children.Add(attachment);
        var service = new AnimationRemapperService(
            new AnimationGenerationSettings { ApplyRelativeScale = false },
            [root]);

        AnimationClip? result = null;
        Assert.That(
            () => result = service.ReMapAnimation(sourceSkeleton, targetSkeleton, animation),
            Throws.Nothing);
        Assert.That(result!.DynamicFrames, Has.Count.EqualTo(2));
    }

    [Test]
    public void ReMapAnimation_AttachmentOffsetUsesTargetReferenceBoneLocalSpace()
    {
        var sourceFile = CreateSkeletonFile(
            "source",
            ("root", -1),
            ("hand", 0),
            ("attachment", 1));
        SetBindPose(sourceFile, Quaternion.Identity);
        sourceFile.AnimationParts[0].DynamicFrames[0].Transforms[2] = new(1, 0, 0);
        var targetFile = CreateSkeletonFile(
            "target",
            ("root", -1),
            ("hand", 0),
            ("attachment", 1));
        SetBindPose(targetFile, Quaternion.Identity);
        var targetHandBindRotation = Quaternion.CreateFromAxisAngle(
            Vector3.UnitZ,
            MathHelper.ToRadians(90));
        targetFile.AnimationParts[0].DynamicFrames[0].Quaternion[1] = new(
            targetHandBindRotation.X,
            targetHandBindRotation.Y,
            targetHandBindRotation.Z,
            targetHandBindRotation.W);
        targetFile.AnimationParts[0].DynamicFrames[0].Transforms[2] = new(1, 0, 0);
        var sourceSkeleton = GameSkeleton.CreateFromAnimationFile(
            sourceFile,
            new AnimationPlayer());
        var targetSkeleton = GameSkeleton.CreateFromAnimationFile(
            targetFile,
            new AnimationPlayer());
        var animation = CreateAnimation(2, 3, 1.0f);
        var sourceHandRotation = Quaternion.CreateFromAxisAngle(
            Vector3.UnitZ,
            MathHelper.ToRadians(90));
        foreach (var frame in animation.DynamicFrames)
        {
            frame.Rotation[1] = sourceHandRotation;
            frame.Position[2] = Vector3.UnitX;
        }

        var root = new SkeletonBoneNode_new("root", 0, -1)
        {
            HasMapping = true,
            MappedIndex = 0,
        };
        var hand = new SkeletonBoneNode_new("hand", 1, 0)
        {
            HasMapping = true,
            MappedIndex = 1,
        };
        var attachment = new SkeletonBoneNode_new("attachment", 2, 1)
        {
            HasMapping = true,
            MappedIndex = 2,
            SelectedRelativeBone = hand,
        };
        root.Children.Add(hand);
        hand.Children.Add(attachment);
        var service = new AnimationRemapperService(
            new AnimationGenerationSettings { ApplyRelativeScale = false },
            [root]);

        var result = service.ReMapAnimation(
            sourceSkeleton,
            targetSkeleton,
            animation);

        Assert.That(
            Vector3.Distance(result.DynamicFrames[0].Position[2], Vector3.UnitX),
            Is.LessThan(0.0001f));
    }

    [Test]
    public void ReMapAnimation_RootAttachmentWithRelativeBone_DoesNotReadParentMinusOne()
    {
        var sourceSkeleton = CreateSkeleton("source", 2);
        var targetSkeleton = CreateSkeleton("target", 2);
        var animation = CreateAnimation(2, 2, 1.0f);
        var root = new SkeletonBoneNode_new("root", 0, -1)
        {
            HasMapping = true,
            MappedIndex = 0,
        };
        var child = new SkeletonBoneNode_new("child", 1, 0)
        {
            HasMapping = true,
            MappedIndex = 1,
        };
        root.Children.Add(child);
        root.SelectedRelativeBone = child;
        var service = new AnimationRemapperService(
            new AnimationGenerationSettings { ApplyRelativeScale = false },
            [root]);

        Assert.That(
            () => service.ReMapAnimation(sourceSkeleton, targetSkeleton, animation),
            Throws.Nothing);
    }

    [Test]
    public void ReMapAnimation_FreezeRotationZ_PreservesOtherRotationAxes()
    {
        var skeleton = CreateSkeleton("shared", 1);
        var animation = CreateAnimation(2, 1, 1.0f);
        var firstFrameTwist = Quaternion.CreateFromAxisAngle(
            Vector3.UnitZ,
            MathHelper.ToRadians(25));
        var secondFrameSwing = Quaternion.CreateFromAxisAngle(
            Vector3.UnitX,
            MathHelper.ToRadians(35));
        var secondFrameTwist = Quaternion.CreateFromAxisAngle(
            Vector3.UnitZ,
            MathHelper.ToRadians(70));
        animation.DynamicFrames[0].Rotation[0] = firstFrameTwist;
        animation.DynamicFrames[1].Rotation[0] = Quaternion.Normalize(
            secondFrameSwing * secondFrameTwist);
        var bone = new SkeletonBoneNode_new("root", 0, -1)
        {
            HasMapping = true,
            MappedIndex = 0,
            FreezeRotationZ = true,
        };
        var service = new AnimationRemapperService(
            new AnimationGenerationSettings { ApplyRelativeScale = false },
            [bone]);

        var result = service.ReMapAnimation(skeleton, skeleton, animation);

        var actual = Quaternion.Normalize(result.DynamicFrames[1].Rotation[0]);
        var expected = Quaternion.Normalize(secondFrameSwing * firstFrameTwist);
        Assert.That(MathF.Abs(Quaternion.Dot(actual, expected)), Is.GreaterThan(0.9999f));
    }

    [Test]
    public void ReMapAnimation_SingleFrame_PreservesFiniteFrameWithoutResampling()
    {
        var skeleton = CreateSkeleton("shared", 1);
        var animation = CreateAnimation(1, 1, 1.0f);
        animation.DynamicFrames[0].Position[0] = new Vector3(1, 2, 3);
        var service = new AnimationRemapperService(
            new AnimationGenerationSettings { ApplyRelativeScale = false },
            []);

        var result = service.ReMapAnimation(skeleton, skeleton, animation);

        Assert.Multiple(() =>
        {
            Assert.That(result.DynamicFrames, Has.Count.EqualTo(1));
            Assert.That(result.DynamicFrames[0].Position[0],
                Is.EqualTo(new Vector3(1, 2, 3)));
            Assert.That(result.PlayTimeInSec, Is.GreaterThan(0));
        });
    }

    private static GameSkeleton CreateSkeleton(string name, int boneCount)
    {
        var skeletonFile = CreateSkeletonFile(name, boneCount, "bone");
        return GameSkeleton.CreateFromAnimationFile(skeletonFile, new AnimationPlayer());
    }

    private static AnimationFile CreateSkeletonFile(
        string name,
        int boneCount,
        string boneNamePrefix)
    {
        var skeletonFile = new AnimationFile
        {
            Bones = Enumerable.Range(0, boneCount)
                .Select(index => new AnimationFile.BoneInfo
                {
                    Id = index,
                    Name = $"{boneNamePrefix}_{index}",
                    ParentId = index - 1,
                })
                .ToArray(),
        };
        skeletonFile.Header.SkeletonName = name;
        return skeletonFile;
    }

    private static AnimationFile CreateSkeletonFile(
        string name,
        params (string Name, int ParentId)[] bones)
    {
        var skeletonFile = new AnimationFile
        {
            Bones = bones
                .Select((bone, index) => new AnimationFile.BoneInfo
                {
                    Id = index,
                    Name = bone.Name,
                    ParentId = bone.ParentId,
                })
                .ToArray(),
        };
        skeletonFile.Header.SkeletonName = name;
        return skeletonFile;
    }

    private static void SetBindPose(
        AnimationFile skeletonFile,
        Quaternion rootRotation)
    {
        var frame = new AnimationFile.Frame();
        var part = new AnimationFile.AnimationPart();
        for (var boneIndex = 0; boneIndex < skeletonFile.Bones.Length; boneIndex++)
        {
            frame.Transforms.Add(new(0, boneIndex, 0));
            var rotation = boneIndex == 0
                ? rootRotation
                : Quaternion.Identity;
            frame.Quaternion.Add(new(
                rotation.X,
                rotation.Y,
                rotation.Z,
                rotation.W));
            part.TranslationMappings.Add(
                new AnimationFile.AnimationBoneMapping(boneIndex));
            part.RotationMappings.Add(
                new AnimationFile.AnimationBoneMapping(boneIndex));
        }

        part.DynamicFrames.Add(frame);
        skeletonFile.AnimationParts = [part];
    }

    private static AnimationClip CreateAnimation(int frameCount, int boneCount, float playTime)
    {
        var animation = new AnimationClip();
        for (var frameIndex = 0; frameIndex < frameCount; frameIndex++)
        {
            var frame = new AnimationClip.KeyFrame();
            for (var boneIndex = 0; boneIndex < boneCount; boneIndex++)
            {
                frame.Position.Add(Vector3.Zero);
                frame.Rotation.Add(Quaternion.Identity);
                frame.Scale.Add(Vector3.One);
            }

            animation.DynamicFrames.Add(frame);
        }

        animation.PlayTimeInSec = playTime;
        return animation;
    }

    private static void SetBoneMappings(
        BoneManager manager,
        params (int TargetIndex, int SourceIndex)[] mappings)
    {
        foreach (var (targetIndex, sourceIndex) in mappings)
        {
            var bone = BoneHelper_new.GetBoneFromId(manager.Bones, targetIndex)!;
            bone.HasMapping = true;
            bone.MappedIndex = sourceIndex;
        }
    }

    private static void AssertQuaternionEquivalent(
        Quaternion actual,
        Quaternion expected)
    {
        Assert.That(
            MathF.Abs(Quaternion.Dot(
                Quaternion.Normalize(actual),
                Quaternion.Normalize(expected))),
            Is.GreaterThan(0.9999f));
    }
}
