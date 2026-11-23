using System.IO;
using Shared.Core.PackFiles.Models;
using Shared.Core.PackFiles.Utility;
using Shared.GameFormats.Animation;
using Test.TestingUtility.Shared;

namespace Test.AnimatioReTarget
{
    // Vanilla WH3 animations and skeletons, read from the packs in WH3_DATA (tests skip without it).
    // Paths are relative to animations\, as in "skeletons/humanoid01.anim".
    static class VanillaAnimations
    {
        static AssetEditorTestRunner? s_runner;

        public static AnimationFile Load(string path)
        {
            var runner = Runner();
            var file = runner.PackFileService.FindFile("animations\\" + path.Replace('/', '\\'));
            Assert.That(file, Is.Not.Null, $"animations/{path} not found in the vanilla packs");
            return AnimationFile.Create(file!);
        }

        static AssetEditorTestRunner Runner()
        {
            var dataFolder = Environment.GetEnvironmentVariable("WH3_DATA");
            Assume.That(dataFolder != null && Directory.Exists(dataFolder), "set WH3_DATA");
            if (s_runner != null)
                return s_runner;

            var runner = new AssetEditorTestRunner();
            runner.CreateCaContainer();
            var loader = (IPackFileContainerLoader)runner.ServiceProvider.GetService(typeof(IPackFileContainerLoader))!;
            foreach (var pack in new[] { "anim.pack", "anim2.pack", "anim_3.pack" })
                runner.PackFileService.AddContainer(loader.CreateFromPackFile(PackFileContainerType.Normal, Path.Combine(dataFolder!, pack), true));
            return s_runner = runner;
        }
    }
}
