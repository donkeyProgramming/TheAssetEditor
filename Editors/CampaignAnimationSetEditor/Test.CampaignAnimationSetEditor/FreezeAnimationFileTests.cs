using Editors.CampaignAnimationSetEditor.Services;
using GameWorld.Core.Animation;
using Microsoft.Xna.Framework;
using Moq;
using Shared.ByteParsing;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.PackFiles.Models.FileSources;
using Shared.Core.Services;
using Shared.GameFormats.Animation;
using Shared.GameFormats.RigidModel.Transforms;

namespace Test.CampaignAnimationSetEditor
{
    /// <summary>The set editor's "Freeze Animroot in .anim" button, which replaced the standalone
    /// Campaign Animation Tool.</summary>
    [TestFixture]
    public class FreezeAnimationFileTests
    {
        const string SkeletonName = "freeze_test_skeleton";

        [Test]
        public void FreezeAnimationFile_ZeroesAnimrootOnEveryFrameAndKeepsOtherBones()
        {
            var skeletonPackFile = new PackFile($"{SkeletonName}.anim", new MemorySource(AnimationFile.ConvertToBytes(CreateSkeletonFile())));
            var skeleton = new GameSkeleton(CreateSkeletonFile(), new AnimationPlayer());
            var sourceClip = CreateMovingClip();
            var sourceBytes = AnimationFile.ConvertToBytes(sourceClip.ConvertToFileFormat(skeleton));
            var sourcePackFile = new PackFile("walk.anim", new MemorySource(sourceBytes));

            var packFileService = new Mock<IPackFileService>();
            packFileService.Setup(x => x.FindFile($"animations/skeletons/{SkeletonName}.anim", null)).Returns(skeletonPackFile);
            var dialogs = new Mock<IStandardDialogs>();
            var service = new CampaignAnimationSetGeneratorService(packFileService.Object, new FreezeRootBoneCommand(dialogs.Object));

            var frozenBytes = service.FreezeAnimationFile(sourcePackFile);

            Assert.That(frozenBytes, Is.Not.Null);
            dialogs.Verify(x => x.ShowDialogBox(It.IsAny<string>(), It.IsAny<string>()), Times.Never);

            var frozenClip = new AnimationClip(AnimationFile.Create(new ByteChunk(frozenBytes!)), skeleton);
            Assert.That(frozenClip.DynamicFrames, Has.Count.EqualTo(sourceClip.DynamicFrames.Count));
            for (var frameIndex = 0; frameIndex < frozenClip.DynamicFrames.Count; frameIndex++)
            {
                var frame = frozenClip.DynamicFrames[frameIndex];
                Assert.That(frame.Position[1], Is.EqualTo(Vector3.Zero));
                Assert.That(frame.Rotation[1], Is.EqualTo(Quaternion.Identity));
                Assert.That(Vector3.Distance(frame.Position[0], sourceClip.DynamicFrames[frameIndex].Position[0]), Is.LessThan(0.001f));
            }
        }

        [Test]
        public void FreezeAnimationFile_ThrowsWhenTheSkeletonIsMissing()
        {
            var skeleton = new GameSkeleton(CreateSkeletonFile(), new AnimationPlayer());
            var sourceBytes = AnimationFile.ConvertToBytes(CreateMovingClip().ConvertToFileFormat(skeleton));
            var sourcePackFile = new PackFile("walk.anim", new MemorySource(sourceBytes));

            var service = new CampaignAnimationSetGeneratorService(new Mock<IPackFileService>().Object, new FreezeRootBoneCommand(new Mock<IStandardDialogs>().Object));

            var exception = Assert.Throws<Exception>(() => service.FreezeAnimationFile(sourcePackFile));
            Assert.That(exception!.Message, Does.Contain(SkeletonName));
        }

        /// <summary>animroot (bone 1) moves forward each frame, like a battle walk cycle.</summary>
        static AnimationClip CreateMovingClip()
        {
            var clip = new AnimationClip();
            for (var frameIndex = 0; frameIndex < 3; frameIndex++)
            {
                var frame = new AnimationClip.KeyFrame();
                frame.Position.Add(new Vector3(0, 1, 0));
                frame.Rotation.Add(Quaternion.Identity);
                frame.Scale.Add(Vector3.One);
                frame.Position.Add(new Vector3(0, 0, frameIndex * 0.5f));
                frame.Rotation.Add(Quaternion.CreateFromYawPitchRoll(frameIndex * 0.1f, 0, 0));
                frame.Scale.Add(Vector3.One);
                clip.DynamicFrames.Add(frame);
            }

            clip.PlayTimeInSec = 0.3f;
            return clip;
        }

        static AnimationFile CreateSkeletonFile()
        {
            var skeletonFile = new AnimationFile();
            skeletonFile.Header.SkeletonName = SkeletonName;
            skeletonFile.Header.Version = 7;
            skeletonFile.Header.FrameRate = 20;
            skeletonFile.Bones =
            [
                new AnimationFile.BoneInfo { Id = 0, Name = "root", ParentId = AnimationFile.BoneIndexNoParent },
                new AnimationFile.BoneInfo { Id = 1, Name = "animroot", ParentId = 0 }
            ];

            var frame = new AnimationFile.Frame();
            frame.Transforms.Add(new RmvVector3(Vector3.Zero));
            frame.Quaternion.Add(new RmvVector4(0, 0, 0, 1));
            frame.Transforms.Add(new RmvVector3(Vector3.Zero));
            frame.Quaternion.Add(new RmvVector4(0, 0, 0, 1));

            var animationPart = new AnimationFile.AnimationPart();
            animationPart.DynamicFrames.Add(frame);
            animationPart.RotationMappings.Add(new AnimationFile.AnimationBoneMapping(0));
            animationPart.RotationMappings.Add(new AnimationFile.AnimationBoneMapping(1));
            animationPart.TranslationMappings.Add(new AnimationFile.AnimationBoneMapping(0));
            animationPart.TranslationMappings.Add(new AnimationFile.AnimationBoneMapping(1));
            skeletonFile.AnimationParts.Add(animationPart);

            return skeletonFile;
        }
    }
}
