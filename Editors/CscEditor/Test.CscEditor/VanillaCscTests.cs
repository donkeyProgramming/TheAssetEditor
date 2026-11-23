using Editors.CscEditor.Data;
using Shared.Core.PackFiles.Models;
using Shared.Core.PackFiles.Utility;
using Test.TestingUtility.Shared;

namespace Test.CscEditor
{
    // Every vanilla WH3 .csc, from the packs in WH3_DATA (skipped without it)
    public class VanillaCscTests
    {
        static readonly string[] Packs = ["data.pack", "data_bl.pack", "data_script.pack", "models2.pack", "models3.pack", "models4.pack"];

        static List<(string Path, byte[] Bytes)> Files()
        {
            var dataFolder = Environment.GetEnvironmentVariable("WH3_DATA");
            Assume.That(dataFolder != null && Directory.Exists(dataFolder), "set WH3_DATA");

            var runner = new AssetEditorTestRunner();
            runner.CreateCaContainer();
            var loader = (IPackFileContainerLoader)runner.ServiceProvider.GetService(typeof(IPackFileContainerLoader))!;
            var files = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
            foreach (var pack in Packs)
            {
                var container = runner.PackFileService.AddContainer(
                    loader.CreateFromPackFile(PackFileContainerType.Normal, Path.Combine(dataFolder!, pack), true))!;
                foreach (var file in runner.PackFileService.FindAllWithExtention(".csc", container))
                    files[file.FileName] = file.Pack.DataSource.ReadData();    // later packs override earlier, as in game
            }
            return files.Select(x => (x.Key, x.Value)).ToList();
        }

        [Test]
        public void EveryFile_Loads_AndSavesStably()
        {
            var files = Files();
            var failures = new List<string>();
            var rootVersions = new SortedDictionary<int, int>();
            foreach (var (path, bytes) in files)
            {
                try
                {
                    var scene = CscScene.Load(bytes);
                    rootVersions[scene.RootVersion] = rootVersions.GetValueOrDefault(scene.RootVersion) + 1;
                    var saved = CscSceneWriter.Write(scene);
                    if (!CscSceneWriter.Write(CscScene.Load(saved)).AsSpan().SequenceEqual(saved))
                        failures.Add($"{path}: second save differs from the first");
                }
                catch (Exception e)
                {
                    failures.Add($"{path}: {e.Message}");
                }
            }

            TestContext.Out.WriteLine($"{files.Count} files, ROOT versions {string.Join(", ", rootVersions.Select(x => $"v{x.Key}={x.Value}"))}");
            Assert.That(files, Has.Count.GreaterThan(2000));
            Assert.That(rootVersions.ContainsKey(6), "patch 6.x+ scenes use ROOT v6");
            Assert.That(failures, Is.Empty, string.Join("\n", failures.Take(20)));
        }
    }
}
