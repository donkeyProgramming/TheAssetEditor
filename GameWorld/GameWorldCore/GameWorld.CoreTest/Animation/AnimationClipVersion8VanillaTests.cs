using GameWorld.Core.Animation;
using GameWorld.Core.Test.Vfx;
using Shared.ByteParsing;
using Shared.GameFormats.Animation;

namespace GameWorld.Core.Test.Animation
{
    // Needs the game installed (WH3_DATA). Vanilla v8 files loaded into a clip and saved from it as v8, with the file
    // as the source, come back as they were. The clip holds floats only, so it can't carry the original byte of an
    // extreme-range channel where several bytes decode to the same float; those files must still decode to the same frames.
    public class AnimationClipVersion8VanillaTests
    {
        [Test]
        public void Warhammer3_Version8Animations_SaveBackThroughAClip()
        {
            var (runner, containers) = VanillaPacks.Load("anim.pack", "anim2.pack", "anim_3.pack");
            var skeletons = new Dictionary<string, GameSkeleton?>();

            var files = containers.SelectMany(c => c.GetAllFiles())
                .Where(x => x.Key.EndsWith(".anim", StringComparison.OrdinalIgnoreCase) && !x.Key.Contains("skeletons", StringComparison.OrdinalIgnoreCase))
                .OrderBy(x => x.Key)
                .Where((_, i) => i % 25 == 0)
                .ToList();

            int checkedFiles = 0, identical = 0, sameFrames = 0;
            var failures = new List<string>();
            foreach (var (path, packFile) in files)
            {
                var original = packFile.DataSource.ReadData();
                if (original.Length < 4 || BitConverter.ToUInt32(original, 0) != 8)
                    continue;
                var file = AnimationFile.Create(new ByteChunk(original));
                if (!skeletons.TryGetValue(file.Header.SkeletonName, out var skeleton))
                {
                    var skeletonFile = runner.PackFileService.FindFile($"animations\\skeletons\\{file.Header.SkeletonName}.anim");
                    skeletons[file.Header.SkeletonName] = skeleton = skeletonFile == null ? null : new GameSkeleton(AnimationFile.Create(skeletonFile), null);
                }
                if (skeleton == null || skeleton.BoneCount != file.Bones.Length)
                    continue;

                checkedFiles++;
                try
                {
                    var saved = AnimationFile.ConvertToBytes(new AnimationClip(file, skeleton).ConvertToFileFormat(skeleton, 8, source: file));
                    if (saved.AsSpan().SequenceEqual(original))
                        identical++;
                    else if (SameFrames(file, AnimationFile.Create(new ByteChunk(saved))))
                        sameFrames++;
                    else
                        failures.Add($"{path}: frames differ");
                }
                catch (Exception e)
                {
                    failures.Add($"{path}: {e.Message}");
                }
            }

            TestContext.Out.WriteLine($"{checkedFiles} files: {identical} byte-identical, {sameFrames} differ only in bytes that decode to the same frames, {failures.Count} failed");
            foreach (var line in failures.Take(50))
                TestContext.Out.WriteLine("  " + line);
            Assert.That(checkedFiles, Is.GreaterThan(100));
            Assert.That(failures, Is.Empty);
        }

        static bool SameFrames(AnimationFile a, AnimationFile b)
        {
            static IEnumerable<float> Values(AnimationFile file) => file.AnimationParts
                .SelectMany(p => (p.StaticFrame != null ? [p.StaticFrame] : new List<AnimationFile.Frame>()).Concat(p.DynamicFrames))
                .SelectMany(f => f.Transforms.SelectMany(t => new[] { t.X, t.Y, t.Z }).Concat(f.Quaternion.SelectMany(q => new[] { q.X, q.Y, q.Z, q.W })));
            return a.AnimationParts.Count == b.AnimationParts.Count && Values(a).SequenceEqual(Values(b));
        }
    }
}
