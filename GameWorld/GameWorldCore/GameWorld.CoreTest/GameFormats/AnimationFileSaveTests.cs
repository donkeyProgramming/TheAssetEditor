using Shared.ByteParsing;
using Shared.GameFormats.Animation;
using Shared.GameFormats.RigidModel.Transforms;

namespace GameWorld.Core.Test.GameFormats
{
    public class AnimationFileSaveTests
    {
        static AnimationFile TwoBoneFile(uint version, params string[] flags)
        {
            var file = new AnimationFile();
            file.Header.Version = version;
            file.Header.Unknown0_alwaysOne = 1;
            file.Header.FrameRate = 20;
            file.Header.SkeletonName = "test_skeleton";
            file.Header.FlagVariables.AddRange(flags);
            file.Header.AnimationTotalPlayTimeInSec = 0.1f;
            file.Bones = [new() { Id = 0, Name = "root", ParentId = -1 }, new() { Id = 1, Name = "child", ParentId = 0 }];

            var part = new AnimationFile.AnimationPart();
            part.TranslationMappings.AddRange([new(0), new(1)]);
            part.RotationMappings.AddRange([new(0), new(1)]);
            for (var f = 0; f < 3; f++)
            {
                part.DynamicFrames.Add(new AnimationFile.Frame
                {
                    Transforms = [new RmvVector3(f, 0, 0), new RmvVector3(0, 1, 0)],
                    Quaternion = [new RmvVector4(0, 0, 0, 1), new RmvVector4(0, 0, 0, 1)],
                });
            }
            file.AnimationParts.Add(part);
            return file;
        }

        // Ported from Szy-Cathay/TheAssetEditor-CN 08d0ed5dc: the writer put flags in v6, which the reader doesn't read
        [TestCase(5u)]
        [TestCase(6u)]
        [TestCase(7u)]
        public void SavedFile_ReadsBack(uint version)
        {
            var file = TwoBoneFile(version);

            var reloaded = AnimationFile.Create(new ByteChunk(AnimationFile.ConvertToBytes(file)));

            Assert.That(reloaded.Header.Version, Is.EqualTo(version));
            Assert.That(reloaded.Header.SkeletonName, Is.EqualTo("test_skeleton"));
            Assert.That(reloaded.AnimationParts.Single().DynamicFrames, Has.Count.EqualTo(3));
            Assert.That(reloaded.AnimationParts.Single().DynamicFrames[2].Transforms[0].X, Is.EqualTo(2));
        }

        [Test]
        public void Version7Flags_RoundTrip_WithTheirRealCount()
        {
            var file = TwoBoneFile(7, "flag_a", "flag_b");
            file.Header.FlagCount = 0;  // stale count must not decide what is written

            var reloaded = AnimationFile.Create(new ByteChunk(AnimationFile.ConvertToBytes(file)));

            Assert.That(reloaded.Header.FlagVariables, Is.EqualTo(new[] { "flag_a", "flag_b" }));
        }
    }
}
