using System.IO;
using Shared.Core.PackFiles.Models;
using Shared.Core.PackFiles.Utility;
using Test.TestingUtility.Shared;

namespace GameWorld.Core.Test.Vfx
{
    // The game's own packs, from the data folder in WH3_DATA; the test is skipped without it.
    static class VanillaPacks
    {
        public static (AssetEditorTestRunner Runner, List<IPackFileContainer> Containers) Load(params string[] packNames)
        {
            var dataFolder = Environment.GetEnvironmentVariable("WH3_DATA");
            Assume.That(dataFolder != null && Directory.Exists(dataFolder), "set WH3_DATA");

            var runner = new AssetEditorTestRunner();
            runner.CreateCaContainer();
            var loader = (IPackFileContainerLoader)runner.ServiceProvider.GetService(typeof(IPackFileContainerLoader))!;
            var containers = packNames
                .Select(x => runner.PackFileService.AddContainer(loader.CreateFromPackFile(PackFileContainerType.Normal, Path.Combine(dataFolder!, x), true))!)
                .ToList();
            return (runner, containers);
        }
    }
}
