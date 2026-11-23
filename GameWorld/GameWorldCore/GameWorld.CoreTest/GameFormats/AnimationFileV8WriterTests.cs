using System.IO;
using GameWorld.Core.Test.Vfx;
using Shared.ByteParsing;
using Shared.GameFormats.Animation;
using Shared.GameFormats.RigidModel.Transforms;

namespace GameWorld.Core.Test.GameFormats
{
    // Ported from Szy-Cathay/TheAssetEditor-CN 2e3120e46 and e21dafe02; the packing tests follow the RMV2 Blender plugin's v8 writer
    public class AnimationFileV8WriterTests
    {
        [Test]
        public void StaticAndDynamicTracks_RoundTrip()
        {
            var actual = RoundTrip(CreateAnimation());

            Assert.Multiple(() =>
            {
                Assert.That(actual.Header.Version, Is.EqualTo(8));
                Assert.That(actual.Header.UnknownValue_v8, Is.EqualTo(6));
                Assert.That(actual.Header.FlagVariables, Is.EqualTo(new[] { "flag_a" }));
                Assert.That(actual.Bones.Select(bone => bone.Name), Is.EqualTo(new[] { "root", "child" }));
                Assert.That(actual.AnimationParts, Has.Count.EqualTo(1));
            });

            var part = actual.AnimationParts.Single();
            Assert.Multiple(() =>
            {
                Assert.That(part.TranslationMappings.Select(mapping => mapping.FileWriteValue), Is.EqualTo(new[] { 10000, 0 }));
                Assert.That(part.RotationMappings.Select(mapping => mapping.FileWriteValue), Is.EqualTo(new[] { 0, 10000 }));
                Assert.That(part.StaticFrame!.Transforms, Has.Count.EqualTo(1));
                Assert.That(part.StaticFrame.Quaternion, Has.Count.EqualTo(1));
                Assert.That(part.DynamicFrames, Has.Count.EqualTo(2));
                Assert.That(part.V8Packing!.TranslationBitRate, Is.EqualTo(new sbyte[] { -12, 12 }), "new data is written uncompressed");
                Assert.That(part.V8Packing.RotationBitRate, Is.EqualTo(new sbyte[] { 8, -8 }));
                Assert.That(part.V8Packing.Range_map_translations, Is.Empty);
            });

            AssertVector(part.StaticFrame!.Transforms.Single(), 1, 2, 3);
            AssertQuaternion(part.StaticFrame.Quaternion.Single(), 0, 0, 0, 1);
            AssertVector(part.DynamicFrames[0].Transforms.Single(), 4, 5, 6);
            AssertVector(part.DynamicFrames[1].Transforms.Single(), 7, 8, 9);
            AssertQuaternion(part.DynamicFrames[0].Quaternion.Single(), 0, 0.70710677f, 0, 0.70710677f);
            AssertQuaternion(part.DynamicFrames[1].Quaternion.Single(), 0, -0.70710677f, 0, 0.70710677f);
        }

        [Test]
        public void MultipleParts_RoundTripAllParts()
        {
            var source = CreateAnimation();
            source.AnimationParts.Add(CreatePart(10));

            var actual = RoundTrip(source);

            Assert.That(actual.AnimationParts, Has.Count.EqualTo(2));
            AssertVector(actual.AnimationParts[1].StaticFrame!.Transforms.Single(), 11, 12, 13);
            AssertVector(actual.AnimationParts[1].DynamicFrames[1].Transforms.Single(), 17, 18, 19);
        }

        [Test]
        public void DynamicMappingWithNoFrames_Throws()
        {
            var source = CreateAnimation();
            source.AnimationParts.Single().DynamicFrames.Clear();

            Assert.That(() => AnimationFile.ConvertToBytes(source), Throws.TypeOf<InvalidDataException>());
        }

        [Test]
        public void MappingIdsOutOfBoneOrder_Throws()
        {
            var source = CreateAnimation();
            var part = source.AnimationParts.Single();
            part.TranslationMappings = [new(1), new(0)];
            foreach (var frame in part.DynamicFrames)
                frame.Transforms.Add(new RmvVector3(0, 0, 0));
            part.StaticFrame = new AnimationFile.Frame { Quaternion = [new RmvVector4(0, 0, 0, 1)] };

            Assert.That(() => AnimationFile.ConvertToBytes(source), Throws.TypeOf<InvalidDataException>());
        }

        [Test]
        public void RangedPacking_IsKept_AndRefittedOnlyWhereValuesLeaveTheRange()
        {
            var source = CreateAnimation();
            var part = source.AnimationParts.Single();
            part.V8Packing = new AnimationFile.AnimationV8OptimizationData(2)
            {
                TranslationBitRate = [-12, 3],
                RotationBitRate = [4, -8],
                // Longer than the last ranged bone, as CA writes about a third of its tables
                Range_map_translations = [(new RmvVector3(0, 0, 0), new RmvVector3(0, 0, 0)), (new RmvVector3(10, 10, 10), new RmvVector3(0, 0, 0)), (new RmvVector3(1, 1, 1), new RmvVector3(2, 2, 2))],
                Range_map_quaternion = [(new RmvVector4(1, 1, 1, 1), new RmvVector4(0, 0, 0, 0))],
            };

            var first = RoundTrip(source).AnimationParts.Single();
            Assert.Multiple(() =>
            {
                Assert.That(first.V8Packing!.TranslationBitRate, Is.EqualTo(new sbyte[] { -12, 3 }));
                Assert.That(first.V8Packing.RotationBitRate, Is.EqualTo(new sbyte[] { 4, -8 }));
                Assert.That(first.V8Packing.Range_map_translations, Is.EqualTo(part.V8Packing.Range_map_translations), "the table is kept whole, values fit");
                Assert.That(first.V8Packing.Range_map_quaternion, Is.EqualTo(part.V8Packing.Range_map_quaternion));
            });
            AssertVector(first.DynamicFrames[1].Transforms.Single(), 7, 8, 9, 10 / 127f);

            // Push bone 1's translation past its +-10 range: that bone's range is refitted, the rest of the table stays
            part.DynamicFrames[1].Transforms[0] = new RmvVector3(30, 8, 9);
            var second = RoundTrip(source).AnimationParts.Single();
            var (scale, centre) = second.V8Packing!.Range_map_translations[1];
            Assert.Multiple(() =>
            {
                Assert.That(second.V8Packing.Range_map_translations, Has.Length.EqualTo(3));
                Assert.That(second.V8Packing.Range_map_translations[2], Is.EqualTo(part.V8Packing.Range_map_translations[2]));
                Assert.That(scale.X, Is.EqualTo(13).Within(1e-5));
                Assert.That(centre.X, Is.EqualTo(17).Within(1e-5));
            });
            AssertVector(second.DynamicFrames[1].Transforms.Single(), 30, 8, 9, 13 / 127f);
        }

        [Test]
        public void PartWithNoDynamicFrames_KeepsItsDeclaredFrameCount()
        {
            var source = CreateAnimation();
            var part = source.AnimationParts.Single();
            part.TranslationMappings = [new(10000), new(10001)];
            part.RotationMappings = [new(10000), new(10001)];
            part.StaticFrame = new AnimationFile.Frame
            {
                Transforms = [new RmvVector3(1, 2, 3), new RmvVector3(4, 5, 6)],
                Quaternion = [new RmvVector4(0, 0, 0, 1), new RmvVector4(0, 0, 0, 1)],
            };
            part.DynamicFrames.Clear();
            part.DeclaredFrameCount = 81;

            var actual = RoundTrip(source).AnimationParts.Single();

            Assert.That(actual.DeclaredFrameCount, Is.EqualTo(81));
            Assert.That(actual.DynamicFrames, Is.Empty);
        }

        [Test]
        public void HeaderWordAfterVersion_IsKept()
        {
            var source = CreateAnimation();
            source.Header.Unknown0_alwaysOne = 0;   // one vanilla v8 bigcat03b idle has 0

            Assert.That(RoundTrip(source).Header.Unknown0_alwaysOne, Is.EqualTo(0));
        }

        [Test]
        public void NewHeader_DefaultsUnknownValueV8ToSix()
        {
            Assert.That(new AnimationFile.AnimationHeader().UnknownValue_v8, Is.EqualTo(6));
        }

        // Needs the game installed (WH3_DATA). Every vanilla v8 animation must save back byte for byte.
        [Test]
        public void Warhammer3_EveryVersion8Animation_SavesBackUnchanged()
        {
            var (_, containers) = VanillaPacks.Load("anim.pack", "anim2.pack", "anim_3.pack", "data.pack", "data_3.pack", "data_bl.pack");

            var checkedFiles = 0;
            var mismatches = new List<string>();
            foreach (var container in containers)
            {
                foreach (var (path, file) in container.GetAllFiles())
                {
                    if (!path.EndsWith(".anim", StringComparison.OrdinalIgnoreCase))
                        continue;
                    var original = file.DataSource.ReadData();
                    if (original.Length < 4 || BitConverter.ToUInt32(original, 0) != 8)
                        continue;

                    checkedFiles++;
                    var loaded = AnimationFile.Create(new ByteChunk(original));
                    var saved = AnimationFile.ConvertToBytes(loaded);
                    if (!saved.AsSpan().SequenceEqual(original))
                        mismatches.Add($"{container.Name}/{path}: {Describe(original, saved)}, frames {(SameFrames(loaded, AnimationFile.Create(new ByteChunk(saved))) ? "identical" : "DIFFER")}");
                }
            }

            TestContext.Out.WriteLine($"{checkedFiles} version 8 animations, {mismatches.Count} differ");
            foreach (var line in mismatches)
                TestContext.Out.WriteLine("  " + line);
            Assert.That(checkedFiles, Is.GreaterThan(0));
            Assert.That(mismatches, Is.Empty);
        }

        static bool SameFrames(AnimationFile a, AnimationFile b)
        {
            static IEnumerable<float> Values(AnimationFile file) => file.AnimationParts.SelectMany(p => (p.StaticFrame != null ? [p.StaticFrame] : new List<AnimationFile.Frame>()).Concat(p.DynamicFrames))
                .SelectMany(f => f.Transforms.SelectMany(t => new[] { t.X, t.Y, t.Z }).Concat(f.Quaternion.SelectMany(q => new[] { q.X, q.Y, q.Z, q.W })));
            return Values(a).SequenceEqual(Values(b));
        }

        static string Describe(byte[] original, byte[] saved)
        {
            if (original.Length != saved.Length)
                return $"length {original.Length} -> {saved.Length}";
            var offsets = Enumerable.Range(0, original.Length).Where(i => original[i] != saved[i]).ToList();
            var largest = offsets.Max(i => Math.Abs((sbyte)original[i] - (sbyte)saved[i]));
            return $"{offsets.Count} bytes differ (first at {offsets[0]}, largest step {largest})";
        }

        static AnimationFile RoundTrip(AnimationFile file) => AnimationFile.Create(new ByteChunk(AnimationFile.ConvertToBytes(file)));

        static AnimationFile CreateAnimation()
        {
            var animation = new AnimationFile
            {
                Header = new AnimationFile.AnimationHeader
                {
                    Version = 8,
                    Unknown0_alwaysOne = 1,
                    FrameRate = 20,
                    SkeletonName = "test_skeleton",
                    FlagVariables = ["flag_a"],
                    AnimationTotalPlayTimeInSec = 0.1f,
                },
                Bones =
                [
                    new AnimationFile.BoneInfo { Id = 0, Name = "root", ParentId = AnimationFile.BoneIndexNoParent },
                    new AnimationFile.BoneInfo { Id = 1, Name = "child", ParentId = 0 },
                ],
            };
            animation.AnimationParts.Add(CreatePart(0));
            return animation;
        }

        static AnimationFile.AnimationPart CreatePart(float offset)
        {
            var part = new AnimationFile.AnimationPart
            {
                TranslationMappings = [new(10000), new(0)],
                RotationMappings = [new(0), new(10000)],
                StaticFrame = new AnimationFile.Frame
                {
                    Transforms = [new RmvVector3(1 + offset, 2 + offset, 3 + offset)],
                    Quaternion = [new RmvVector4(0, 0, 0, 1)],
                },
            };
            part.DynamicFrames.Add(new AnimationFile.Frame
            {
                Transforms = [new RmvVector3(4 + offset, 5 + offset, 6 + offset)],
                Quaternion = [new RmvVector4(0, 0.70710677f, 0, 0.70710677f)],
            });
            part.DynamicFrames.Add(new AnimationFile.Frame
            {
                Transforms = [new RmvVector3(7 + offset, 8 + offset, 9 + offset)],
                Quaternion = [new RmvVector4(0, -0.70710677f, 0, 0.70710677f)],
            });
            return part;
        }

        static void AssertVector(RmvVector3 actual, float x, float y, float z, float tolerance = 0.0001f)
        {
            Assert.Multiple(() =>
            {
                Assert.That(actual.X, Is.EqualTo(x).Within(tolerance));
                Assert.That(actual.Y, Is.EqualTo(y).Within(tolerance));
                Assert.That(actual.Z, Is.EqualTo(z).Within(tolerance));
            });
        }

        static void AssertQuaternion(RmvVector4 actual, float x, float y, float z, float w)
        {
            Assert.Multiple(() =>
            {
                Assert.That(actual.X, Is.EqualTo(x).Within(0.0001f));
                Assert.That(actual.Y, Is.EqualTo(y).Within(0.0001f));
                Assert.That(actual.Z, Is.EqualTo(z).Within(0.0001f));
                Assert.That(actual.W, Is.EqualTo(w).Within(0.0001f));
            });
        }
    }
}
