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

        // #159
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

        // Ported from Szy-Cathay/TheAssetEditor-CN 2e3120e46: still tracks go in the static frame
        [Test]
        public void Version8_StillTracksAreStatic_MovingTracksDynamic()
        {
            var file = CreateClip(3, 0.1f).ConvertToFileFormat(CreateTwoBoneSkeleton(), 8, flagVariables: ["flag_a"]);
            var reloaded = AnimationFile.Create(new ByteChunk(AnimationFile.ConvertToBytes(file)));

            var part = reloaded.AnimationParts.Single();
            Assert.Multiple(() =>
            {
                Assert.That(reloaded.Header.Version, Is.EqualTo(8));
                Assert.That(reloaded.Header.UnknownValue_v8, Is.EqualTo(6));
                Assert.That(reloaded.Header.FlagVariables, Is.EqualTo(new[] { "flag_a" }));
                // root and child both move in translation; both rotations hold still
                Assert.That(part.TranslationMappings.Select(x => x.FileWriteValue), Is.EqualTo(new[] { 0, 1 }));
                Assert.That(part.RotationMappings.Select(x => x.FileWriteValue), Is.EqualTo(new[] { 10000, 10001 }));
                Assert.That(part.DynamicFrames, Has.Count.EqualTo(3));
                Assert.That(part.DynamicFrames[2].Transforms[1].Y, Is.EqualTo(4));
            });
        }

        [Test]
        public void Version8_SingleFrame_IsStaticOnlyPose()
        {
            var file = CreateClip(1, 0).ConvertToFileFormat(CreateTwoBoneSkeleton(), 8);
            var reloaded = AnimationFile.Create(new ByteChunk(AnimationFile.ConvertToBytes(file)));

            var part = reloaded.AnimationParts.Single();
            Assert.Multiple(() =>
            {
                Assert.That(part.DynamicFrames, Is.Empty);
                Assert.That(part.StaticFrame!.Transforms, Has.Count.EqualTo(2));
                Assert.That(reloaded.Header.AnimationTotalPlayTimeInSec, Is.EqualTo(0.1f));
                Assert.That(part.DeclaredFrameCount, Is.EqualTo(3));
            });
        }

        [Test]
        public void Version8_ClipThatHoldsStill_KeepsOneDynamicTrack()
        {
            var clip = CreateClip(3, 0.1f);
            foreach (var frame in clip.DynamicFrames)
                frame.Position = [Vector3.Zero, new Vector3(0, 2, 0)];

            var part = clip.ConvertToFileFormat(CreateTwoBoneSkeleton(), 8).AnimationParts.Single();

            Assert.That(part.TranslationMappings[0].IsDynamic, Is.True);
            Assert.That(part.DynamicFrames, Has.Count.EqualTo(3));
        }

        [Test]
        public void Version8_WithSource_KeepsItsNoneTracksAndPacking()
        {
            var skeleton = CreateTwoBoneSkeleton();
            var source = CreateClip(3, 0.1f).ConvertToFileFormat(skeleton, 8, unknownValueV8: 5);
            // child rotation untouched by the animation: "none", so it plays the skeleton's bind pose
            source.AnimationParts[0].RotationMappings[1] = new AnimationFile.AnimationBoneMapping(-1);
            source.AnimationParts[0].RotationMappings[0] = new AnimationFile.AnimationBoneMapping(10000);
            source.AnimationParts[0].StaticFrame!.Quaternion.RemoveAt(1);
            source = AnimationFile.Create(new ByteChunk(AnimationFile.ConvertToBytes(source)));

            var clip = new AnimationClip(source, skeleton);
            var saved = clip.ConvertToFileFormat(skeleton, 8, source: source);

            Assert.Multiple(() =>
            {
                Assert.That(saved.Header.UnknownValue_v8, Is.EqualTo(5));
                Assert.That(saved.AnimationParts[0].RotationMappings[1].HasValue, Is.False);
                Assert.That(saved.AnimationParts[0].V8Packing, Is.SameAs(source.AnimationParts[0].V8Packing));
                Assert.That(AnimationFile.ConvertToBytes(saved), Is.EqualTo(AnimationFile.ConvertToBytes(source)));
            });
        }
    }
}
