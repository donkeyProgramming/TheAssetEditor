using Editors.CampaignAnimationSetEditor.Services;
using GameWorld.Core.Animation;
using Microsoft.Xna.Framework;
using Moq;
using Shared.Core.Services;
using Shared.GameFormats.Animation;
using Shared.GameFormats.RigidModel.Transforms;

namespace Test.CampaignAnimationSetEditor
{
    // Based on Szy-Cathay/TheAssetEditor-CN d5370f9f1, changed to follow CA's own campaign animations
    [TestFixture]
    public class FreezeRootBoneAxisSwapTests
    {
        // carriage01's animroot holds this on every frame, battle and campaign alike
        static readonly Quaternion AxisSwap = new(0, 0.70710677f, 0.70710677f, 0);
        static readonly Quaternion PelvisRotation = Quaternion.CreateFromYawPitchRoll(0.3f, 0.2f, 0.1f);

        // animroot (0, no parent) -> pelvis (1) -> spine (2)
        static GameSkeleton Skeleton()
        {
            var file = new AnimationFile();
            file.Header.SkeletonName = "freeze_axis_test";
            file.Bones =
            [
                new() { Id = 0, Name = "animroot", ParentId = AnimationFile.BoneIndexNoParent },
                new() { Id = 1, Name = "pelvis", ParentId = 0 },
                new() { Id = 2, Name = "spine", ParentId = 1 },
            ];
            var part = new AnimationFile.AnimationPart();
            part.DynamicFrames.Add(new AnimationFile.Frame
            {
                Transforms = [new RmvVector3(0, 0, 0), new RmvVector3(0, 0, 1), new RmvVector3(0, 0, 0.5f)],
                Quaternion = [new RmvVector4(0, 0, 0, 1), new RmvVector4(0, 0, 0, 1), new RmvVector4(0, 0, 0, 1)],
            });
            file.AnimationParts.Add(part);
            return new GameSkeleton(file, null);
        }

        // The root moves forward every frame and holds `rootRotation(frame)`
        static AnimationClip Clip(Func<int, Quaternion> rootRotation)
        {
            var clip = new AnimationClip { PlayTimeInSec = 0.2f };
            for (var f = 0; f < 3; f++)
            {
                var frame = new AnimationClip.KeyFrame();
                frame.Position.AddRange([new Vector3(0, f * 0.5f, 0), new Vector3(0, 0, 1), new Vector3(0, 0, 0.5f)]);
                frame.Rotation.AddRange([rootRotation(f), PelvisRotation, Quaternion.Identity]);
                frame.Scale.AddRange([Vector3.One, Vector3.One, Vector3.One]);
                clip.DynamicFrames.Add(frame);
            }
            return clip;
        }

        static AnimationClip Freeze(AnimationClip clip, GameSkeleton? skeleton)
        {
            var command = new FreezeRootBoneCommand(new Mock<IStandardDialogs>().Object);
            Assert.That(command.Execute(clip, 0, "animroot", out var frozen, skeleton), Is.True);
            return frozen!;
        }

        [Test]
        public void FixedAxisSwapOnTheRoot_IsKept_AndMovementRemoved()
        {
            var frozen = Freeze(Clip(_ => AxisSwap), Skeleton());

            Assert.That(frozen.DynamicFrames.Select(x => x.Position[0]), Is.All.EqualTo(Vector3.Zero));
            Assert.That(frozen.DynamicFrames.Select(x => Quaternion.Dot(x.Rotation[0], AxisSwap)), Is.All.EqualTo(1).Within(1e-5f));
            Assert.That(frozen.DynamicFrames.Select(x => x.Rotation[1]), Is.All.EqualTo(PelvisRotation));
        }

        [Test]
        public void MovingHeadingOnTheRoot_IsStillRemoved()
        {
            var frozen = Freeze(Clip(f => Quaternion.CreateFromAxisAngle(Vector3.UnitY, f * 0.2f)), Skeleton());

            Assert.That(frozen.DynamicFrames.Select(x => x.Rotation[0]), Is.All.EqualTo(Quaternion.Identity));
        }

        [Test]
        public void FixedHeadingThatKeepsYUp_IsStillRemoved()
        {
            var frozen = Freeze(Clip(_ => Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathHelper.PiOver2)), Skeleton());

            Assert.That(frozen.DynamicFrames.Select(x => x.Rotation[0]), Is.All.EqualTo(Quaternion.Identity));
        }

        [Test]
        public void WithoutASkeleton_TheRootIsJustZeroed()
        {
            var frozen = Freeze(Clip(_ => AxisSwap), null);

            Assert.That(frozen.DynamicFrames.Select(x => x.Rotation[0]), Is.All.EqualTo(Quaternion.Identity));
        }
    }
}
