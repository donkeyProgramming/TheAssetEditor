using Editor.CampaignAnimationCreator.CampaignAnimationCreator.Commands;
using Editors.Shared.Core.Common.ReferenceModel;
using GameWorld.Core.Animation;
using Microsoft.Xna.Framework;
using Moq;
using Shared.Core.Services;

namespace Test.CampaignAnimationCreator.CampaignAnimationCreator.Commands
{
    [TestFixture]
    public class ConvertCampaignAnimationCommandTests
    {
        [Test]
        public void Execute_NoAnimation_ShowsDialogAndReturnsFalse()
        {
            var dialogs = new Mock<IStandardDialogs>();
            var command = new ConvertCampaignAnimationCommand(dialogs.Object);

            var result = command.Execute(null, new SkeletonBoneNode { BoneIndex = 0, BoneName = "animroot" }, out var convertedAnimation);

            Assert.That(result, Is.False);
            Assert.That(convertedAnimation, Is.Null);
            dialogs.Verify(x => x.ShowDialogBox("Unable to convert animation - No animation selected", "Error"), Times.Once);
        }

        [Test]
        public void Execute_NoRootBone_ShowsDialogAndReturnsFalse()
        {
            var dialogs = new Mock<IStandardDialogs>();
            var command = new ConvertCampaignAnimationCommand(dialogs.Object);

            var result = command.Execute(CreateAnimationClip(), null, out var convertedAnimation);

            Assert.That(result, Is.False);
            Assert.That(convertedAnimation, Is.Null);
            dialogs.Verify(x => x.ShowDialogBox("Unable to convert animation - No root bone selected", "Error"), Times.Once);
        }

        [Test]
        public void Execute_NoFrames_ShowsDialogAndReturnsFalse()
        {
            var dialogs = new Mock<IStandardDialogs>();
            var command = new ConvertCampaignAnimationCommand(dialogs.Object);
            var sourceAnimation = new AnimationClip { PlayTimeInSec = 0.1f };

            var result = command.Execute(sourceAnimation, new SkeletonBoneNode { BoneIndex = 0, BoneName = "animroot" }, out var convertedAnimation);

            Assert.That(result, Is.False);
            Assert.That(convertedAnimation, Is.Null);
            dialogs.Verify(x => x.ShowDialogBox("Unable to convert animation - Animation has no frames", "Error"), Times.Once);
        }

        [Test]
        public void Execute_BoneIndexOutOfRange_ShowsDialogAndReturnsFalse()
        {
            var dialogs = new Mock<IStandardDialogs>();
            var command = new ConvertCampaignAnimationCommand(dialogs.Object);
            var sourceAnimation = CreateAnimationClip(boneCount: 1, frameCount: 1);

            var result = command.Execute(sourceAnimation, new SkeletonBoneNode { BoneIndex = 1, BoneName = "animroot" }, out var convertedAnimation);

            Assert.That(result, Is.False);
            Assert.That(convertedAnimation, Is.Null);
            dialogs.Verify(x => x.ShowDialogBox("Unable to convert animation - Bone index 1 is out of range for frame 0", "Error"), Times.Once);
        }

        [Test]
        public void Execute_ValidAnimation_ClonesAndResetsSelectedBoneAcrossAllFrames()
        {
            var dialogs = new Mock<IStandardDialogs>();
            var command = new ConvertCampaignAnimationCommand(dialogs.Object);
            var sourceAnimation = CreateAnimationClip();
            var originalAnimation = sourceAnimation.Clone();
            var rootBone = new SkeletonBoneNode { BoneIndex = 1, BoneName = "animroot" };

            var result = command.Execute(sourceAnimation, rootBone, out var convertedAnimation);

            Assert.That(result, Is.True);
            Assert.That(convertedAnimation, Is.Not.Null);
            Assert.That(convertedAnimation, Is.Not.SameAs(sourceAnimation));
            dialogs.Verify(x => x.ShowDialogBox(It.IsAny<string>(), It.IsAny<string>()), Times.Never);

            for (var frameIndex = 0; frameIndex < convertedAnimation!.DynamicFrames.Count; frameIndex++)
            {
                Assert.That(convertedAnimation.DynamicFrames[frameIndex].Position[1], Is.EqualTo(Vector3.Zero));
                Assert.That(convertedAnimation.DynamicFrames[frameIndex].Rotation[1], Is.EqualTo(Quaternion.Identity));

                Assert.That(convertedAnimation.DynamicFrames[frameIndex].Position[0], Is.EqualTo(originalAnimation.DynamicFrames[frameIndex].Position[0]));
                Assert.That(convertedAnimation.DynamicFrames[frameIndex].Rotation[0], Is.EqualTo(originalAnimation.DynamicFrames[frameIndex].Rotation[0]));

                Assert.That(sourceAnimation.DynamicFrames[frameIndex].Position[1], Is.EqualTo(originalAnimation.DynamicFrames[frameIndex].Position[1]));
                Assert.That(sourceAnimation.DynamicFrames[frameIndex].Rotation[1], Is.EqualTo(originalAnimation.DynamicFrames[frameIndex].Rotation[1]));
            }
        }

        private static AnimationClip CreateAnimationClip(int boneCount = 2, int frameCount = 2)
        {
            var clip = new AnimationClip();

            for (var frameIndex = 0; frameIndex < frameCount; frameIndex++)
            {
                var frame = new AnimationClip.KeyFrame();
                for (var boneIndex = 0; boneIndex < boneCount; boneIndex++)
                {
                    frame.Position.Add(new Vector3(frameIndex + boneIndex + 1, frameIndex + boneIndex + 2, frameIndex + boneIndex + 3));
                    frame.Rotation.Add(Quaternion.CreateFromYawPitchRoll(frameIndex + boneIndex + 0.1f, frameIndex + boneIndex + 0.2f, frameIndex + boneIndex + 0.3f));
                    frame.Scale.Add(Vector3.One);
                }

                clip.DynamicFrames.Add(frame);
            }

            clip.PlayTimeInSec = 0.2f;
            return clip;
        }
    }
}
