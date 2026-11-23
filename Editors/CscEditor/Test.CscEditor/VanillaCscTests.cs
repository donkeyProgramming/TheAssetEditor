using Editors.CscEditor.Data;
using Editors.CscEditor.ViewModels;
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

        [Test]
        public void EveryLegacyCamera_PositionAndFovEdits_SurviveSaveAndReload()
        {
            var checkedCameras = 0;
            foreach (var (path, bytes) in Files())
            {
                var scene = CscScene.Load(bytes);
                foreach (var camera in scene.AllElementsIncludingNested().Where(e => e.Kind == CscElementKind.Camera && e.ElementVersion == 5).ToList())
                {
                    var viewModel = new CscElementViewModel(camera, (_, _) => { });
                    Assert.That(viewModel.IsEditable, Is.True, path);
                    var previousFov = camera.CameraFov!.StaticValue;
                    var previousPosition = camera.EvaluatePosition(0);
                    viewModel.PositionX += 0.25f;
                    viewModel.CameraFov += 1;

                    var saved = CscScene.Load(CscSceneWriter.Write(scene)).FindElement(camera.Id)!;
                    Assert.Multiple(() =>
                    {
                        Assert.That(saved.EvaluatePosition(0).X, Is.EqualTo(previousPosition.X + 0.25f), path);
                        Assert.That(saved.CameraFov!.StaticValue, Is.EqualTo(previousFov + 1), path);
                        Assert.That(saved.TypeRecord!.Version, Is.EqualTo(camera.TypeRecord!.Version), path);
                    });
                    checkedCameras++;
                }
            }
            Assert.That(checkedCameras, Is.EqualTo(37), "vanilla WH3 has 37 ELEMENT v5 cameras");
        }
    }
}
