using System.IO;
using System.Text.Json;
using GameWorld.Core.Test.Vfx;
using Shared.ByteParsing;
using Shared.GameFormats.Animation;
using Shared.GameFormats.RigidModel.Transforms;

namespace GameWorld.Core.Test.GameFormats
{
    // Cross-checks AE's v8 writer against the RMV2 Blender plugin (io_scene_rmv2/anim_format.py), in both directions.
    // Needs WH3_DATA and ANIM_XCHECK_DIR. Run Export, then the plugin side (anim_v8_crosscheck.py <dir> <path to anim_format.py>),
    // then Import.
    //   Export: a sample of vanilla v8 files -> <name>.orig.anim, AE's re-save <name>.ae.anim, and AE's save of an edited
    //           copy <name>.ae_edit.anim with the frames AE meant to write in <name>.ae_edit.json
    //   plugin: compares its own re-save with .orig/.ae bytes, reads .ae_edit.anim and compares with the json, and makes
    //           the same edit itself -> <name>.py_edit.anim + <name>.py_edit.json
    //   Import: AE reads <name>.py_edit.anim and compares with the plugin's json
    [Explicit]
    public class AnimationV8PluginCrossCheck
    {
        static string OutputFolder()
        {
            var folder = Environment.GetEnvironmentVariable("ANIM_XCHECK_DIR");
            Assume.That(folder != null, "set ANIM_XCHECK_DIR");
            Directory.CreateDirectory(folder!);
            return folder!;
        }

        [Test]
        public void Export()
        {
            var folder = OutputFolder();
            var (_, containers) = VanillaPacks.Load("anim.pack", "anim2.pack", "anim_3.pack");

            var all = containers.SelectMany(c => c.GetAllFiles())
                .Where(x => x.Key.EndsWith(".anim", StringComparison.OrdinalIgnoreCase))
                .Select(x => (Path: x.Key, Data: x.Value.DataSource.ReadData()))
                .Where(x => x.Data.Length >= 4 && BitConverter.ToUInt32(x.Data, 0) == 8)
                .Select(x => (x.Path, x.Data, File: AnimationFile.Create(new ByteChunk(x.Data))))
                .OrderBy(x => x.Path)
                .ToList();

            var picked = new Dictionary<string, (byte[] Data, AnimationFile File, string Why)>();
            void Pick(string why, int count, Func<AnimationFile, bool> predicate)
            {
                foreach (var x in all.Where(x => !picked.ContainsKey(x.Path) && predicate(x.File)).Take(count))
                    picked[x.Path] = (x.Data, x.File, why);
            }

            Pick("multi-part", 4, f => f.AnimationParts.Count > 1);
            Pick("static-only", 4, f => f.AnimationParts.All(p => p.DynamicFrames.Count == 0));
            Pick("ranged translations", 4, f => f.AnimationParts.Any(p => p.V8Packing!.TranslationBitRate.Contains((sbyte)3)));
            Pick("ranged rotations", 4, f => f.AnimationParts.Any(p => p.V8Packing!.RotationBitRate.Contains((sbyte)4)));
            Pick("range table longer than needed", 3, f => f.AnimationParts.Any(LongTable));
            Pick("header word not 1", 3, f => f.Header.Unknown0_alwaysOne != 1);
            Pick("unknown_v8 not 6", 3, f => f.Header.UnknownValue_v8 != 6);
            var random = new Random(8);
            foreach (var x in all.Where(x => !picked.ContainsKey(x.Path)).OrderBy(_ => random.Next()).Take(10))
                picked[x.Path] = (x.Data, x.File, "random");

            var index = new List<object>();
            foreach (var (path, (data, file, why)) in picked)
            {
                var name = Path.GetFileNameWithoutExtension(path);
                File.WriteAllBytes(Path.Combine(folder, name + ".orig.anim"), data);
                File.WriteAllBytes(Path.Combine(folder, name + ".ae.anim"), AnimationFile.ConvertToBytes(file));

                var edited = AnimationFile.Create(new ByteChunk(data));
                Edit(edited);
                File.WriteAllBytes(Path.Combine(folder, name + ".ae_edit.anim"), AnimationFile.ConvertToBytes(edited));
                File.WriteAllText(Path.Combine(folder, name + ".ae_edit.json"), JsonSerializer.Serialize(Dump(edited)));
                index.Add(new { name, path, why });
            }
            File.WriteAllText(Path.Combine(folder, "index.json"), JsonSerializer.Serialize(index));
            TestContext.Out.WriteLine($"{picked.Count} files exported to {folder}");
            foreach (var (path, (_, _, why)) in picked)
                TestContext.Out.WriteLine($"  {why}: {path}");
        }

        [Test]
        public void Import()
        {
            var folder = OutputFolder();
            var failures = new List<string>();
            var files = Directory.GetFiles(folder, "*.py_edit.anim");
            Assume.That(files, Is.Not.Empty, "run the plugin side first");
            foreach (var path in files)
            {
                var name = Path.GetFileName(path)[..^".py_edit.anim".Length];
                var file = AnimationFile.Create(new ByteChunk(File.ReadAllBytes(path)));
                var expected = JsonSerializer.Deserialize<List<PartDump>>(File.ReadAllText(Path.Combine(folder, name + ".py_edit.json")))!;
                var error = Compare(Dump(file), expected, file);
                if (error != null)
                    failures.Add($"{name}: {error}");
            }
            TestContext.Out.WriteLine($"{files.Length} plugin-saved files read, {failures.Count} differ");
            foreach (var line in failures)
                TestContext.Out.WriteLine("  " + line);
            Assert.That(failures, Is.Empty);
        }

        // The same edit the plugin script makes: translations x1.5 + 0.1 (pushes ranged ones out of their range),
        // rotations x0.9 (stays in range, so they re-encode through the file's range)
        static void Edit(AnimationFile file)
        {
            foreach (var part in file.AnimationParts)
            {
                foreach (var frame in (part.StaticFrame != null ? [part.StaticFrame] : new List<AnimationFile.Frame>()).Concat(part.DynamicFrames))
                {
                    frame.Transforms = frame.Transforms.Select(t => new RmvVector3((float)(t.X * 1.5 + 0.1), (float)(t.Y * 1.5 + 0.1), (float)(t.Z * 1.5 + 0.1))).ToList();
                    frame.Quaternion = frame.Quaternion.Select(q => new RmvVector4((float)(q.X * 0.9), (float)(q.Y * 0.9), (float)(q.Z * 0.9), (float)(q.W * 0.9))).ToList();
                }
            }
        }

        static bool LongTable(AnimationFile.AnimationPart part)
        {
            var packing = part.V8Packing!;
            var lastT = Array.FindLastIndex(packing.TranslationBitRate, r => r == 3 || r == -3);
            var lastR = Array.FindLastIndex(packing.RotationBitRate, r => r == 4 || r == -4);
            return packing.Range_map_translations.Length > lastT + 1 || packing.Range_map_quaternion.Length > lastR + 1;
        }

        public record FrameDump(List<double[]> T, List<double[]> R);
        public record PartDump(FrameDump? Static, List<FrameDump> Dynamic);

        static List<PartDump> Dump(AnimationFile file) => file.AnimationParts.Select(p => new PartDump(
            p.StaticFrame == null ? null : DumpFrame(p.StaticFrame),
            p.DynamicFrames.Select(DumpFrame).ToList())).ToList();

        static FrameDump DumpFrame(AnimationFile.Frame f) => new(
            f.Transforms.Select(t => new double[] { t.X, t.Y, t.Z }).ToList(),
            f.Quaternion.Select(q => new double[] { q.X, q.Y, q.Z, q.W }).ToList());

        // Within one quantisation step of the channel's encoding: half a range step for ranged channels, 1/32767 for
        // int16 quaternions, float32 rounding for raw translations
        static string? Compare(List<PartDump> actual, List<PartDump> expected, AnimationFile file)
        {
            if (actual.Count != expected.Count)
                return $"{actual.Count} parts, plugin has {expected.Count}";
            for (var p = 0; p < actual.Count; p++)
            {
                var packing = file.AnimationParts[p].V8Packing!;
                if ((actual[p].Static == null) != (expected[p].Static == null))
                    return $"part {p}: static frame presence differs";
                if (actual[p].Dynamic.Count != expected[p].Dynamic.Count)
                    return $"part {p}: {actual[p].Dynamic.Count} frames, plugin has {expected[p].Dynamic.Count}";

                var pairs = actual[p].Dynamic.Zip(expected[p].Dynamic).Select(x => (x.First, x.Second, true)).ToList();
                if (actual[p].Static != null)
                    pairs.Add((actual[p].Static!, expected[p].Static!, false));
                foreach (var (a, e, isDynamic) in pairs)
                {
                    var tol = Tolerances(packing.TranslationBitRate, isDynamic, 3, b => new double[] { packing.Range_map_translations[b].Min.X, packing.Range_map_translations[b].Min.Y, packing.Range_map_translations[b].Min.Z });
                    var error = Within(a.T, e.T, tol, $"part {p} translation") ;
                    if (error != null) return error;
                    var rtol = Tolerances(packing.RotationBitRate, isDynamic, 4, b => new double[] { packing.Range_map_quaternion[b].Min.X, packing.Range_map_quaternion[b].Min.Y, packing.Range_map_quaternion[b].Min.Z, packing.Range_map_quaternion[b].Min.W });
                    error = Within(a.R, e.R, rtol, $"part {p} rotation");
                    if (error != null) return error;
                }
            }
            return null;
        }

        static List<double[]?> Tolerances(sbyte[] rates, bool isDynamic, int ranged, Func<int, double[]> scale)
        {
            var output = new List<double[]?>();
            for (var bone = 0; bone < rates.Length; bone++)
            {
                var rate = isDynamic ? rates[bone] : -rates[bone];
                if (rate <= 0)
                    continue;
                output.Add(rate == ranged ? scale(bone).Select(s => Math.Abs(s) / 127.0 * 0.5 + 1e-6).ToArray() : null);
            }
            return output;
        }

        static string? Within(List<double[]> actual, List<double[]> expected, List<double[]?> tolerance, string what)
        {
            if (actual.Count != expected.Count)
                return $"{what}: {actual.Count} channels, plugin has {expected.Count}";
            for (var i = 0; i < actual.Count; i++)
            {
                for (var c = 0; c < actual[i].Length; c++)
                {
                    var tol = tolerance[i]?[c] ?? Math.Max(1.0 / 32767.0, Math.Abs(expected[i][c]) * 1e-6);
                    if (Math.Abs(actual[i][c] - expected[i][c]) > tol)
                        return $"{what} channel {i}[{c}]: AE {actual[i][c]}, plugin {expected[i][c]}";
                }
            }
            return null;
        }
    }
}
