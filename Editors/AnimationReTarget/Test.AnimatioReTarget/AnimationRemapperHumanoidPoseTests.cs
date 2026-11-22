using System.IO;
using Editors.AnimatioReTarget.Editor;
using Editors.AnimatioReTarget.Editor.BoneHandling;
using Editors.AnimatioReTarget.Editor.Settings;
using GameWorld.Core.Animation;
using Microsoft.Xna.Framework;
using Shared.ByteParsing;
using Shared.GameFormats.Animation;
using Test.TestingUtility.TestUtility;

namespace Test.AnimatioReTarget;

public class AnimationRemapperHumanoidPoseTests
{
    private static readonly string[] Rigs =
        ["humanoid01", "humanoid01b", "humanoid01c", "humanoid01d", "humanoid01e"];

    private static readonly string[] LimbEnds =
    [
        "lowerarm_left", "lowerarm_right", "hand_left", "hand_right",
        "lowerleg_left", "lowerleg_right", "foot_left", "foot_right",
    ];

    private static IEnumerable<TestCaseData> AnimationCases()
    {
        var paths = new[]
        {
            "humanoid01c/2handed_axe/locomotion/hu1c_2ha_walk_01.anim",
            "humanoid01c/2handed_axe/attacks/hu1c_2ha_attack_01.anim",
            "humanoid01d/2handed_axe/locomotion/hu1d_2ha_walk_01.anim",
            "humanoid01d/2handed_axe/stand/hu1d_2ha_stand_idle_01.anim",
            "humanoid01e/celestial_general/locomotion/hu1e_celestial_general_walk_01.anim",
            "humanoid01e/celestial_general/attacks/hu1e_celestial_general_attack_01.anim",
            "humanoid01e/rider/warhorse/sword_lord/locomotion/hu1e_hr1_sword_lord_canter_01.anim",
        };
        foreach (var path in paths)
        {
            var source = path.Split('/')[0];
            foreach (var target in Rigs.Where(rig => rig != source))
            {
                foreach (var relativeScale in new[] { false, true })
                    yield return new TestCaseData(path, target, relativeScale);
            }
        }
    }

    private static IEnumerable<TestCaseData> RigPairs()
    {
        foreach (var source in Rigs)
        {
            foreach (var target in Rigs.Where(rig => rig != source))
                yield return new TestCaseData(source, target);
        }
    }

    [TestCaseSource(nameof(AnimationCases))]
    public void ReMapAnimation_RealHumanoidActions_TransfersLimbDirectionsAndRespectsLengthScaling(
        string animationPath,
        string targetName,
        bool relativeScale)
    {
        var animationFile = LoadFile($"battle/{animationPath}");
        var sourceFile = LoadSkeleton(animationFile.Header.SkeletonName);
        var targetFile = LoadSkeleton(targetName);
        var source = new GameSkeleton(sourceFile, new AnimationPlayer());
        var target = new GameSkeleton(targetFile, new AnimationPlayer());
        var animation = new AnimationClip(animationFile, source);
        var service = CreateService(source, targetFile, relativeScale);

        var result = service.ReMapAnimation(source, target, animation);

        for (var frameIndex = 0; frameIndex < animation.DynamicFrames.Count; frameIndex++)
            AssertLimbPose(source, target, animation, result, frameIndex, relativeScale);
    }

    [TestCaseSource(nameof(RigPairs))]
    public void ReMapAnimation_DifferentHumanoidRestPoses_MatchesAnimatedPoseInBothDirections(
        string sourceName,
        string targetName)
    {
        var sourceFile = LoadSkeleton(sourceName);
        var targetFile = LoadSkeleton(targetName);
        var source = new GameSkeleton(sourceFile, new AnimationPlayer());
        var target = new GameSkeleton(targetFile, new AnimationPlayer());
        var animation = new AnimationClip { PlayTimeInSec = 1 };
        for (var frameIndex = 0; frameIndex < 3; frameIndex++)
        {
            var frame = new AnimationClip.KeyFrame();
            frame.Position.AddRange(source.Translation);
            frame.Rotation.AddRange(source.Rotation);
            frame.Scale.AddRange(Enumerable.Repeat(Vector3.One, source.BoneCount));
            foreach (var boneName in new[] { "upperarm_left", "upperarm_right", "lowerarm_left", "lowerarm_right" })
            {
                var index = source.GetBoneIndexByName(boneName);
                frame.Rotation[index] = Quaternion.Normalize(
                    Quaternion.CreateFromAxisAngle(Vector3.UnitZ, frameIndex * 0.3f) *
                    frame.Rotation[index]);
            }
            animation.DynamicFrames.Add(frame);
        }
        var service = CreateService(source, targetFile, true);

        var result = service.ReMapAnimation(source, target, animation);

        for (var frameIndex = 0; frameIndex < animation.DynamicFrames.Count; frameIndex++)
            AssertLimbPose(source, target, animation, result, frameIndex, true);
    }

    [TestCaseSource(nameof(AnimationCases))]
    public void ReMapAnimation_RealActions_KeepsUnmappedBindChannels(
        string animationPath,
        string targetName,
        bool relativeScale)
    {
        var animationFile = LoadFile($"battle/{animationPath}");
        var sourceFile = LoadSkeleton(animationFile.Header.SkeletonName);
        var targetFile = LoadSkeleton(targetName);
        var source = new GameSkeleton(sourceFile, new AnimationPlayer());
        var target = new GameSkeleton(targetFile, new AnimationPlayer());
        var animation = new AnimationClip(animationFile, source);
        var result = CreateService(source, targetFile, relativeScale).ReMapAnimation(source, target, animation);
        var mapped = target.BoneNames.Select(source.GetBoneIndexByName).ToArray();
        for (var frameIndex = 0; frameIndex < result.DynamicFrames.Count; frameIndex++)
        {
            for (var index = 0; index < target.BoneCount; index++)
            {
                if (mapped[index] >= 0)
                    continue;
                Assert.That(result.DynamicFrames[frameIndex].Position[index], Is.EqualTo(target.Translation[index]),
                    $"{target.BoneNames[index]}, frame {frameIndex}: unmatched local position");
                Assert.That(result.DynamicFrames[frameIndex].Rotation[index], Is.EqualTo(target.Rotation[index]),
                    $"{target.BoneNames[index]}, frame {frameIndex}: unmatched local rotation");
            }
        }
    }

    private static IEnumerable<TestCaseData> UniformAnimationCases()
    {
        foreach (var path in AnimationCases().Select(data => (string)data.Arguments[0]!).Distinct())
        {
            foreach (var scale in new[] { 0.65f, 1.7f })
                yield return new TestCaseData(path, scale);
        }
    }

    [TestCaseSource(nameof(UniformAnimationCases))]
    public void ReMapAnimation_RealActionUniformScale_MatchesUpstreamBeforeAndAfterExport(string animationPath, float scale)
    {
        var file = LoadFile($"battle/{animationPath}");
        var skeletonFile = LoadSkeleton(file.Header.SkeletonName);
        var source = new GameSkeleton(skeletonFile, new AnimationPlayer());
        var target = source.Clone();
        for (var index = 0; index < target.BoneCount; index++)
            target.Translation[index] *= scale;
        target.RebuildSkeletonMatrix();
        var result = CreateService(source, skeletonFile, true).ReMapAnimation(source, target, new AnimationClip(file, source));
        var exported = AnimationFile.Create(new ByteChunk(AnimationFile.ConvertToBytes(result.ConvertToFileFormat(target))));
        var reloaded = new AnimationClip(exported, target);
        var original = new AnimationClip(file, source);
        var expected = AnimationRemapperUpstreamCompatibilityTests.ConvertWithUpstream045(
            source, target, original, Enumerable.Range(0, target.BoneCount).ToArray(), true);
        AnimationRemapperUpstreamCompatibilityTests.AssertSamePose(target, expected, result);
        AnimationRemapperUpstreamCompatibilityTests.AssertSamePose(target, expected, reloaded);
    }

    [TestCaseSource(nameof(AnimationCases))]
    public void ReMapAnimation_ManuallyMappedRootTracks_KeepUpstreamRootSpaceWithoutImplicitAttachment(
        string animationPath, string targetName, bool relativeScale)
    {
        var file = LoadFile($"battle/{animationPath}");
        var source = new GameSkeleton(LoadSkeleton(file.Header.SkeletonName), new AnimationPlayer());
        var targetFile = LoadSkeleton(targetName);
        var target = new GameSkeleton(targetFile, new AnimationPlayer());
        var animation = new AnimationClip(file, source);
        var bones = SkeletonBoneNodeHelper.Build(targetFile);
        var mapped = Enumerable.Repeat(-1, target.BoneCount).ToArray();
        foreach (var bone in Flatten(bones))
        {
            var sourceName = bone.BoneName;
            if (source.GetBoneIndexByName(sourceName) < 0)
            {
                if (sourceName.StartsWith("weapon_", StringComparison.Ordinal))
                    sourceName = $"be_prop_{int.Parse(sourceName[7..]) - 1}";
                else if (sourceName.StartsWith("be_prop_", StringComparison.Ordinal))
                    sourceName = $"weapon_{int.Parse(sourceName[8..]) + 1}";
            }
            bone.MappedIndex = source.GetBoneIndexByName(sourceName);
            bone.HasMapping = bone.MappedIndex >= 0;
            mapped[bone.BoneIndex] = bone.MappedIndex;
        }
        var result = new AnimationRemapperService(new AnimationGenerationSettings { ApplyRelativeScale = relativeScale }, bones)
            .ReMapAnimation(source, target, animation);
        var expected = AnimationRemapperUpstreamCompatibilityTests.ConvertWithUpstream045(
            source, target, animation, mapped, relativeScale);
        AnimationRemapperUpstreamCompatibilityTests.AssertSamePose(target, expected, result);
    }

    private static void AssertLimbPose(
        GameSkeleton source,
        GameSkeleton target,
        AnimationClip sourceAnimation,
        AnimationClip targetAnimation,
        int frameIndex,
        bool relativeScale)
    {
        var sourceFrame = AnimationSampler.Sample(frameIndex, 0, source, sourceAnimation);
        var targetFrame = AnimationSampler.Sample(frameIndex, 0, target, targetAnimation);
        Assert.Multiple(() =>
        {
            foreach (var name in LimbEnds)
            {
                var sourceIndex = source.GetBoneIndexByName(name);
                var targetIndex = target.GetBoneIndexByName(name);
                var sourceParent = source.GetParentBoneIndex(sourceIndex);
                var targetParent = target.GetParentBoneIndex(targetIndex);
                var sourceDirection = sourceFrame.GetSkeletonAnimatedWorld(source, sourceIndex).Translation -
                    sourceFrame.GetSkeletonAnimatedWorld(source, sourceParent).Translation;
                var targetDirection = targetFrame.GetSkeletonAnimatedWorld(target, targetIndex).Translation -
                    targetFrame.GetSkeletonAnimatedWorld(target, targetParent).Translation;
                var angle = MathHelper.ToDegrees(MathF.Acos(Math.Clamp(
                    Vector3.Dot(Vector3.Normalize(sourceDirection), Vector3.Normalize(targetDirection)), -1, 1)));
                var targetLength = Vector3.Distance(
                    target.GetWorldTransform(targetIndex).Translation,
                    target.GetWorldTransform(targetParent).Translation);
                Assert.That(angle, Is.LessThan(0.25f), $"{name}, frame {frameIndex}: direction error in degrees");
                var sourceLength = Vector3.Distance(source.GetWorldTransform(sourceIndex).Translation,
                    source.GetWorldTransform(sourceParent).Translation);
                var expectedLength = sourceDirection.Length() * (relativeScale ? targetLength / sourceLength : 1);
                Assert.That(targetDirection.Length(), Is.EqualTo(expectedLength).Within(0.001f),
                    $"{name}, frame {frameIndex}: animated segment length");
            }
        });
    }

    private static AnimationRemapperService CreateService(
        GameSkeleton source,
        AnimationFile targetFile,
        bool relativeScale)
    {
        var bones = SkeletonBoneNodeHelper.Build(targetFile);
        foreach (var bone in Flatten(bones))
        {
            bone.MappedIndex = source.GetBoneIndexByName(bone.BoneName);
            bone.HasMapping = bone.MappedIndex >= 0;
            bone.ApplyRotation = true;
            bone.ApplyTranslation = true;
        }
        return new AnimationRemapperService(
            new AnimationGenerationSettings { ApplyRelativeScale = relativeScale }, bones);
    }

    private static IEnumerable<SkeletonBoneNode_new> Flatten(IEnumerable<SkeletonBoneNode_new> bones)
    {
        foreach (var bone in bones)
        {
            yield return bone;
            foreach (var child in Flatten(bone.Children))
                yield return child;
        }
    }

    private static AnimationFile LoadSkeleton(string name) => LoadFile($"skeletons/{name}.anim");

    private static AnimationFile LoadFile(string path) => VanillaAnimations.Load(path);
}
