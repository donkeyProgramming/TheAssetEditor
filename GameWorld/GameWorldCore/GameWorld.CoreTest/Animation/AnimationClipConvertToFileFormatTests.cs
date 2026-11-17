using GameWorld.Core.Animation;
using Microsoft.Xna.Framework;
using Shared.ByteParsing;
using Shared.GameFormats.Animation;
using Shared.GameFormats.RigidModel.Transforms;

namespace GameWorld.Core.Test.Animation
{
    internal class AnimationClipConvertToFileFormatTests
    {
        static GameSkeleton CreateTwoBoneSkeleton()
        {
            var file = new AnimationFile();
            file.Header.SkeletonName = "test_skeleton";
            file.Bones = [new() { Id = 0, Name = "root", ParentId = -1 }, new() { Id = 1, Name = "child", ParentId = 0 }];
            var part = new AnimationFile.AnimationPart();
            part.DynamicFrames.Add(new AnimationFile.Frame
            {
                Transforms = [new RmvVector3(0, 0, 0), new RmvVector3(0, 1, 0)],
                Quaternion = [new RmvVector4(0, 0, 0, 1), new RmvVector4(0, 0, 0, 1)],
            });
            file.AnimationParts.Add(part);
            return new GameSkeleton(file, null);
        }

        static AnimationClip CreateClip(int frameCount, float playTime)
        {
            var clip = new AnimationClip { PlayTimeInSec = playTime };
            for (var f = 0; f < frameCount; f++)
            {
                clip.DynamicFrames.Add(new AnimationClip.KeyFrame
                {
                    Position = [new Vector3(f, 0, 0), new Vector3(0, 2 + f, 0)],
                    Rotation = [Quaternion.Identity, Quaternion.CreateFromAxisAngle(Vector3.UnitZ, 0.5f)],
                    Scale = [Vector3.One, Vector3.One],
                });
            }
            return clip;
        }

        // #159: a one-frame clip must be written the way vanilla writes poses - everything in the
        // static frame, no dynamic frames - as a single dynamic frame crashes the game.
        [Test]
        public void SingleFrameClip_IsWrittenAsStaticPose()
        {
            var skeleton = CreateTwoBoneSkeleton();

            var file = CreateClip(1, 0).ConvertToFileFormat(skeleton);
            var reloaded = AnimationFile.Create(new ByteChunk(AnimationFile.ConvertToBytes(file)));

            var part = reloaded.AnimationParts.Single();
            Assert.That(part.DynamicFrames, Is.Empty);
            Assert.That(part.StaticFrame, Is.Not.Null);
            Assert.That(part.StaticFrame!.Transforms, Has.Count.EqualTo(2));
            Assert.That(part.StaticFrame.Quaternion, Has.Count.EqualTo(2));
            Assert.That(part.TranslationMappings.All(x => x.IsStatic), Is.True);
            Assert.That(part.RotationMappings.All(x => x.IsStatic), Is.True);
            Assert.That(reloaded.Header.AnimationTotalPlayTimeInSec, Is.EqualTo(0.1f));

            var clip = new AnimationClip(reloaded, skeleton);
            Assert.That(clip.DynamicFrames, Has.Count.EqualTo(1));
            Assert.That(clip.DynamicFrames[0].Position[1], Is.EqualTo(new Vector3(0, 2, 0)));
        }

        [Test]
        public void MultiFrameClip_StaysDynamic()
        {
            var file = CreateClip(3, 0.1f).ConvertToFileFormat(CreateTwoBoneSkeleton());

            var part = file.AnimationParts.Single();
            Assert.That(part.StaticFrame, Is.Null);
            Assert.That(part.DynamicFrames, Has.Count.EqualTo(3));
            Assert.That(part.TranslationMappings.All(x => x.IsDynamic), Is.True);
        }
    }
}
