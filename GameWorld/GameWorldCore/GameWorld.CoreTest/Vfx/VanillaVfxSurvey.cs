using System.IO;
using GameWorld.Core.Vfx;
using Shared.GameFormats.Vfx;

namespace GameWorld.Core.Test.Vfx
{
    // Needs the game installed (WH3_DATA). Parses every vanilla vfx file, resolves every effect and reads
    // every emitter; prints the counts, the problems, the gaps and candidates for a set of effects to compare against CA's editor.
    public class VanillaVfxSurvey
    {
        const string Firebox = "wh3_dlc23_warmachine_steam_engine_firebox";

        [Test, Explicit]
        public void Warhammer3_AllVanillaVfx_ParseResolveAndRead()
        {
            var (runner, containers) = VanillaPacks.Load("vfx_desc.pack");
            var resolver = new VfxResolver(runner.PackFileService);
            var files = containers[0].GetAllFiles().Where(x => x.Key.EndsWith(".xml", StringComparison.OrdinalIgnoreCase)).OrderBy(x => x.Key).ToList();

            var unparsed = new List<string>();
            var problems = new List<string>();
            var gaps = new Dictionary<string, int>();
            var emitters = 0;
            var candidates = new Dictionary<string, List<string>>
            {
                ["looping smoke"] = [], ["embers"] = [], ["burst"] = [], ["velocity-aligned sparks"] = [], ["fixed-speed flipbook"] = [],
            };
            foreach (var (path, file) in files)
            {
                try { VfxFile.Parse(file.DataSource.ReadData()); }
                catch (Exception e) { unparsed.Add($"{path}: {e.Message}"); continue; }

                if (path.Count(c => c == '\\' || c == '/') != 1)
                    continue;   // only vfx/<name>.xml is reachable by an EFFECT tag's name
                var name = Path.GetFileNameWithoutExtension(path);
                var resolved = resolver.Resolve(name, new Random(1));
                problems.AddRange(resolved.Problems.Select(x => $"{name}: {x}"));
                var settings = resolved.Emitters.Select(EmitterSettings.From).ToList();
                emitters += settings.Count;
                foreach (var gap in settings.SelectMany(x => x.Skipped))
                    gaps[gap] = gaps.GetValueOrDefault(gap) + 1;
                Classify(name, settings, candidates);
            }

            TestContext.Out.WriteLine($"{files.Count} files; {emitters} enabled emitters resolved");
            TestContext.Out.WriteLine($"files that do not parse: {unparsed.Count}");
            foreach (var line in unparsed.Take(20)) TestContext.Out.WriteLine("  " + line);
            TestContext.Out.WriteLine($"reference problems: {problems.Count}");
            foreach (var line in problems.Take(40)) TestContext.Out.WriteLine("  " + line);
            TestContext.Out.WriteLine("gaps, by emitter count:");
            foreach (var (gap, count) in gaps.OrderByDescending(x => x.Value)) TestContext.Out.WriteLine($"  {count,7}  {gap}");
            foreach (var (category, names) in candidates) TestContext.Out.WriteLine($"{category}: {string.Join(", ", names)}");

            var firebox = resolver.Resolve(Firebox, new Random(1));
            var fireboxFile = VfxFile.Parse(runner.PackFileService.FindFile(VfxResolver.PathOf(Firebox))!.DataSource.ReadData());
            // Two references: lib_warmachines (the furnace) and lib_lightsource (its orange light).
            var switchedOn = fireboxFile.References.SelectMany(r => r.Overrides.Values).Where(x => x.Enable).Select(x => x.Name).ToList();
            TestContext.Out.WriteLine($"firebox: {string.Join(", ", firebox.Emitters.Select(x => x.Emitter.Id))}");
            Assert.Multiple(() =>
            {
                Assert.That(unparsed, Is.Empty, "vanilla files that do not parse");
                Assert.That(firebox.Emitters.Select(x => x.Emitter.Id), Is.EquivalentTo(switchedOn), "the firebox draws exactly the emitters its reference switches on");
            });
        }

        static void Classify(string name, List<EmitterSettings> settings, Dictionary<string, List<string>> candidates)
        {
            var drawn = settings.Where(x => x.Draws).ToList();
            if (!name.StartsWith("wh3_") || drawn.Count == 0)
                return;
            void Add(string category, bool matches)
            {
                if (matches && candidates[category].Count < 3)
                    candidates[category].Add(name);
            }
            Add("looping smoke", drawn.Any(x => x.InfiniteLife && x.Premultiplied && x.AdditiveMul == 0 && x.Animated));
            Add("embers", drawn.Any(x => x.InfiniteLife && x.AdditiveMul >= 1 && x.Orientation == QuadOrientation.CameraFacing));
            Add("burst", drawn.All(x => !x.InfiniteLife && x.LifeTime.Max < 1));
            Add("velocity-aligned sparks", drawn.Any(x => x.Orientation == QuadOrientation.VelocityAligned && x.AdditiveMul >= 1));
            Add("fixed-speed flipbook", drawn.Any(x => x.Animated && x.FixedSpeed));
        }
    }
}
