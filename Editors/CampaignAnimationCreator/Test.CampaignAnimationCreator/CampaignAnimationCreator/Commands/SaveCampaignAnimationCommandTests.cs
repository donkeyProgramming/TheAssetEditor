using Editor.CampaignAnimationCreator.CampaignAnimationCreator.Commands;
using GameWorld.Core.Animation;
using Microsoft.Xna.Framework;
using Moq;
using Shared.ByteParsing;
using Shared.Core.Services;
using Shared.GameFormats.Animation;
using Shared.GameFormats.RigidModel.Transforms;

namespace Test.CampaignAnimationCreator.CampaignAnimationCreator.Commands
{
    [TestFixture]
    public class SaveCampaignAnimationCommandTests
    {
        [Test]
        public void Execute_NoSkeleton_ShowsDialogAndReturnsFalse()
        {
            var fileSaveService = new Mock<IFileSaveService>();
            var dialogs = new Mock<IStandardDialogs>();
            var command = new SaveCampaignAnimationCommand(fileSaveService.Object, dialogs.Object);

            var result = command.Execute(null, CreateAnimationClip());

            Assert.That(result, Is.False);
            dialogs.Verify(x => x.ShowDialogBox("Unable to save - No skeleton provided", "Error"), Times.Once);
            fileSaveService.Verify(x => x.SaveAs(It.IsAny<string>(), It.IsAny<byte[]>()), Times.Never);
        }

        [Test]
        public void Execute_NoAnimation_ShowsDialogAndReturnsFalse()
        {
            var fileSaveService = new Mock<IFileSaveService>();
            var dialogs = new Mock<IStandardDialogs>();
            var command = new SaveCampaignAnimationCommand(fileSaveService.Object, dialogs.Object);

            var result = command.Execute(CreateSkeleton(), null);

            Assert.That(result, Is.False);
            dialogs.Verify(x => x.ShowDialogBox("Unable to save - No animation provided", "Error"), Times.Once);
            fileSaveService.Verify(x => x.SaveAs(It.IsAny<string>(), It.IsAny<byte[]>()), Times.Never);
        }

        [Test]
        public void Execute_ValidSkeletonAndAnimation_SavesAnimBytes()
        {
            var skeleton = CreateSkeleton();
            var animationClip = CreateAnimationClip();
            byte[]? savedBytes = null;

            var fileSaveService = new Mock<IFileSaveService>();
            fileSaveService
                .Setup(x => x.SaveAs(".anim", It.IsAny<byte[]>()))
                .Callback<string, byte[]>((_, bytes) => savedBytes = bytes);

            var dialogs = new Mock<IStandardDialogs>();
            var command = new SaveCampaignAnimationCommand(fileSaveService.Object, dialogs.Object);

            var result = command.Execute(skeleton, animationClip);

            Assert.That(result, Is.True);
            Assert.That(savedBytes, Is.Not.Null.And.Not.Empty);

            fileSaveService.Verify(x => x.SaveAs(".anim", It.IsAny<byte[]>()), Times.Once);
            dialogs.Verify(x => x.ShowDialogBox(It.IsAny<string>(), It.IsAny<string>()), Times.Never);

            var savedAnimation = AnimationFile.Create(new ByteChunk(savedBytes!));
            Assert.That(savedAnimation.Header.SkeletonName, Is.EqualTo(skeleton.SkeletonName));
            Assert.That(savedAnimation.Bones.Select(x => x.Name), Is.EqualTo(skeleton.BoneNames));
            Assert.That(savedAnimation.AnimationParts, Has.Count.EqualTo(1));
            Assert.That(savedAnimation.AnimationParts[0].DynamicFrames, Has.Count.EqualTo(animationClip.DynamicFrames.Count));
            Assert.That(savedAnimation.Header.AnimationTotalPlayTimeInSec, Is.EqualTo(animationClip.PlayTimeInSec).Within(0.001f));
        }

        private static AnimationClip CreateAnimationClip()
        {
            var clip = new AnimationClip();

            var frame0 = new AnimationClip.KeyFrame();
            frame0.Position.Add(new Vector3(1, 2, 3));
            frame0.Rotation.Add(Quaternion.Identity);
            frame0.Scale.Add(Vector3.One);
            frame0.Position.Add(new Vector3(4, 5, 6));
            frame0.Rotation.Add(Quaternion.CreateFromYawPitchRoll(0.1f, 0.2f, 0.3f));
            frame0.Scale.Add(Vector3.One);

            var frame1 = new AnimationClip.KeyFrame();
            frame1.Position.Add(new Vector3(7, 8, 9));
            frame1.Rotation.Add(Quaternion.CreateFromYawPitchRoll(0.4f, 0.5f, 0.6f));
            frame1.Scale.Add(Vector3.One);
            frame1.Position.Add(new Vector3(10, 11, 12));
            frame1.Rotation.Add(Quaternion.CreateFromYawPitchRoll(0.7f, 0.8f, 0.9f));
            frame1.Scale.Add(Vector3.One);

            clip.DynamicFrames.Add(frame0);
            clip.DynamicFrames.Add(frame1);
            clip.PlayTimeInSec = 0.2f;
            return clip;
        }

        private static GameSkeleton CreateSkeleton()
        {
            var skeletonFile = new AnimationFile();
            skeletonFile.Header.SkeletonName = "campaign_animation_test_skeleton";
            skeletonFile.Bones =
            [
                new AnimationFile.BoneInfo { Id = 0, Name = "root", ParentId = AnimationFile.BoneIndexNoParent },
                new AnimationFile.BoneInfo { Id = 1, Name = "animroot", ParentId = 0 }
            ];

            var frame = new AnimationFile.Frame();
            frame.Transforms.Add(new RmvVector3(Vector3.Zero));
            frame.Quaternion.Add(new RmvVector4(0, 0, 0, 1));
            frame.Transforms.Add(new RmvVector3(new Vector3(1, 2, 3)));
            frame.Quaternion.Add(new RmvVector4(0, 0, 0, 1));

            var animationPart = new AnimationFile.AnimationPart();
            animationPart.DynamicFrames.Add(frame);
            skeletonFile.AnimationParts.Add(animationPart);

            return new GameSkeleton(skeletonFile, null!);
        }
    }
}
