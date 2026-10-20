using System.IO;
using Shared.Core.PackFiles.Models;
using Shared.Core.PackFiles.Utility;
using Test.TestingUtility.Shared;

namespace Test.VfxEditor
{
    // Every file in the game's vfx_desc.pack, from the data folder in WH3_DATA; the test is skipped without it.
    static class VanillaVfx
    {
        public static List<(string Path, byte[] Bytes)> Files()
        {
            var dataFolder = Environment.GetEnvironmentVariable("WH3_DATA");
            Assume.That(dataFolder != null && Directory.Exists(dataFolder), "set WH3_DATA");

            var runner = new AssetEditorTestRunner();
            runner.CreateCaContainer();
            var loader = (IPackFileContainerLoader)runner.ServiceProvider.GetService(typeof(IPackFileContainerLoader))!;
            var container = runner.PackFileService.AddContainer(
                loader.CreateFromPackFile(PackFileContainerType.Normal, Path.Combine(dataFolder!, "vfx_desc.pack"), true))!;
            return runner.PackFileService.FindAllWithExtention(".xml", container)
                .Select(f => (f.FileName, f.Pack.DataSource.ReadData()))
                .ToList();
        }
    }
}
