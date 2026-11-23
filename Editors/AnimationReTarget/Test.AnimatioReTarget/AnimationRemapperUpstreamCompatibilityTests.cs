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

public class AnimationRemapperUpstreamCompatibilityTests
{
    private static readonly string[] Rigs =
        ["humanoid01", "humanoid01b", "humanoid01c", "humanoid01d", "humanoid01e"];

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
            foreach (var target in Rigs.Where(rig => rig != path.Split('/')[0]))
            {
                foreach (var scale in new[] { false, true })
                    yield return new TestCaseData(path, target, scale);
            }
        }
    }

    [TestCaseSource(nameof(AnimationCases))]
    public void ReMapAnimation_RealActions_MatchesUpstream045ForEveryBone(
        string animationPath, string targetName, bool relativeScale)
    {
        var file = Load($"battle/{animationPath}");
        var source = new GameSkeleton(Load($"skeletons/{file.Header.SkeletonName}.anim"), new AnimationPlayer());
        var targetFile = Load($"skeletons/{targetName}.anim");
        var target = new GameSkeleton(targetFile, new AnimationPlayer());
        var animation = new AnimationClip(file, source);
        var bones = SkeletonBoneNodeHelper.Build(targetFile);
        foreach (var bone in Flatten(bones))
        {
            bone.MappedIndex = source.GetBoneIndexByName(bone.BoneName);
            bone.HasMapping = bone.MappedIndex >= 0;
        }
        var mappings = target.BoneNames.Select(source.GetBoneIndexByName).ToArray();
        var expected = ConvertWithUpstream045(source, target, animation, mappings, relativeScale);
        var actual = new AnimationRemapperService(
            new AnimationGenerationSettings { ApplyRelativeScale = relativeScale }, bones)
            .ReMapAnimation(source, target, animation);

        for (var frame = 0; frame < animation.DynamicFrames.Count; frame++)
        {
            var expectedPose = AnimationSampler.Sample(frame, 0, target, expected);
            var actualPose = AnimationSampler.Sample(frame, 0, target, actual);
            for (var index = 0; index < target.BoneCount; index++)
            {
                var expectedWorld = expectedPose.GetSkeletonAnimatedWorld(target, index);
                var actualWorld = actualPose.GetSkeletonAnimatedWorld(target, index);
                Assert.That(Vector3.Distance(expectedWorld.Translation, actualWorld.Translation),
                    Is.LessThan(0.001f), $"{target.BoneNames[index]}, frame {frame}: upstream world position");
                foreach (var axis in new[] { Vector3.UnitX, Vector3.UnitY, Vector3.UnitZ })
                {
                    Assert.That(Vector3.Distance(Vector3.TransformNormal(axis, expectedWorld), Vector3.TransformNormal(axis, actualWorld)),
                        Is.LessThan(0.001f), $"{target.BoneNames[index]}, frame {frame}: upstream world orientation");
                }
            }
        }
    }

    // Reference equations from v0.45, commit 2b0719a8bb6b927869f08ffa2099657535fb1309.
    // Keep this independent of the production remapper's bind/basis calculations.
    internal static AnimationClip ConvertWithUpstream045(GameSkeleton source, GameSkeleton target,
        AnimationClip animation, int[] mappings, bool relativeScale)
        => ConvertWithChannels(source, target, animation, mappings, relativeScale,
            Enumerable.Repeat(true, target.BoneCount).ToArray(), Enumerable.Repeat(true, target.BoneCount).ToArray());

    internal static AnimationClip ConvertWithChannels(GameSkeleton source, GameSkeleton target,
        AnimationClip animation, int[] mappings, bool relativeScale, bool[] applyTranslation, bool[] applyRotation)
    {
        var result = new AnimationClip { PlayTimeInSec = animation.PlayTimeInSec };
        foreach (var _ in animation.DynamicFrames)
        {
            result.DynamicFrames.Add(new AnimationClip.KeyFrame
            {
                Position = target.Translation.ToList(),
                Rotation = target.Rotation.ToList(),
                Scale = Enumerable.Repeat(Vector3.One, target.BoneCount).ToList(),
            });
        }
        for (var frame = 0; frame < animation.DynamicFrames.Count; frame++)
        {
            var sourcePose = AnimationSampler.Sample(frame, 0, source, animation);
            for (var index = 0; index < target.BoneCount; index++)
            {
                if (mappings[index] < 0)
                    continue;
                var targetPose = AnimationSampler.Sample(frame, 0, target, result);
                var transform = sourcePose.GetSkeletonAnimatedWorld(source, mappings[index]);
                var parent = target.GetParentBoneIndex(index);
                if (parent >= 0)
                    transform *= Matrix.Invert(targetPose.GetSkeletonAnimatedWorld(target, parent));
                transform.Decompose(out _, out var rotation, out var position);
                if (applyRotation[index])
                    result.DynamicFrames[frame].Rotation[index] = rotation;
                if (applyTranslation[index])
                    result.DynamicFrames[frame].Position[index] = position;
            }
        }
        if (relativeScale)
        {
            for (var index = 0; index < target.BoneCount; index++)
            {
                if (mappings[index] < 0 || !applyTranslation[index])
                    continue;
                var sourceParent = source.GetParentBoneIndex(mappings[index]);
                var targetParent = target.GetParentBoneIndex(index);
                if (sourceParent < 0 || targetParent < 0)
                    continue;
                var sourceLength = Vector3.Distance(source.GetWorldTransform(mappings[index]).Translation, source.GetWorldTransform(sourceParent).Translation);
                var targetLength = Vector3.Distance(target.GetWorldTransform(index).Translation, target.GetWorldTransform(targetParent).Translation);
                var ratio = sourceLength == 0 || targetLength == 0 ? 1 : targetLength / sourceLength;
                foreach (var frame in result.DynamicFrames)
                    frame.Position[index] *= ratio;
            }
        }
        return result;
    }

    internal static void AssertSamePose(GameSkeleton target, AnimationClip expected, AnimationClip actual, float tolerance = 0.001f)
    {
        Assert.That(actual.DynamicFrames.Count, Is.EqualTo(expected.DynamicFrames.Count));
        for (var frame = 0; frame < expected.DynamicFrames.Count; frame++)
        {
            var expectedPose = AnimationSampler.Sample(frame, 0, target, expected);
            var actualPose = AnimationSampler.Sample(frame, 0, target, actual);
            for (var index = 0; index < target.BoneCount; index++)
            {
                var expectedWorld = expectedPose.GetSkeletonAnimatedWorld(target, index);
                var actualWorld = actualPose.GetSkeletonAnimatedWorld(target, index);
                Assert.That(float.IsFinite(actualWorld.Determinant()), Is.True, target.BoneNames[index]);
                Assert.That(Vector3.Distance(expectedWorld.Translation, actualWorld.Translation),
                    Is.LessThan(tolerance), $"{target.BoneNames[index]}, frame {frame}: upstream world position");
                foreach (var axis in new[] { Vector3.UnitX, Vector3.UnitY, Vector3.UnitZ })
                {
                    Assert.That(Vector3.Distance(Vector3.TransformNormal(axis, expectedWorld), Vector3.TransformNormal(axis, actualWorld)),
                        Is.LessThan(tolerance), $"{target.BoneNames[index]}, frame {frame}: upstream world orientation");
                }
            }
        }
    }

    private static IEnumerable<SkeletonBoneNode_new> Flatten(IEnumerable<SkeletonBoneNode_new> nodes)
    {
        foreach (var node in nodes)
        {
            yield return node;
            foreach (var child in Flatten(node.Children))
                yield return child;
        }
    }

    private static AnimationFile Load(string path) => VanillaAnimations.Load(path);
}
