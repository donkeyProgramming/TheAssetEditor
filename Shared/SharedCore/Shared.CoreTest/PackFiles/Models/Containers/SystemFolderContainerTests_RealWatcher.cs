using System.Diagnostics;
using Shared.Core.PackFiles.Models.Containers;
using Shared.Core.PackFiles.Utility;
using Shared.Core.Services;

namespace Shared.CoreTest.PackFiles.Models.Containers
{
    // A folder project with Windows' own watcher, not a mock. Image editors save over a file by putting a new copy in its place
    // (a temp file written, then the original deleted or renamed away, then the temp renamed in), so the folder sees the name
    // vanish and come back within milliseconds, inside one batch.
    [TestFixture]
    internal class SystemFolderContainerTests_RealWatcher
    {
        string _dir = null!;

        [SetUp]
        public void Setup()
        {
            _dir = Path.Combine(Path.GetTempPath(), "SysFolderRealWatcher_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_dir);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_dir))
                Directory.Delete(_dir, true);
        }

        [TestCase("deleted, then written again")]
        [TestCase("temp written, original deleted, temp renamed in")]
        [TestCase("original renamed away, temp renamed in, backup deleted")]
        [TestCase("File.Replace")]
        public void AFileSavedOverByAnotherProgram_StaysListed(string how)
        {
            var png = Path.Combine(_dir, "tex.png");
            var temp = Path.Combine(_dir, "~ps8F2A.tmp");
            var backup = Path.Combine(_dir, "tex.png.bak");
            File.WriteAllBytes(png, [1, 1, 1]);

            using var container = new SystemFolderContainer(_dir, new FileSystemAccess(), new FileSystemWatcherWrapper(), syncContext: null);
            Assert.That(container.ContainsFile("tex.png"), Is.True);

            byte[] saved = [2, 2, 2, 2];
            switch (how)
            {
                case "deleted, then written again":
                    File.Delete(png);
                    File.WriteAllBytes(png, saved);
                    break;
                case "temp written, original deleted, temp renamed in":
                    File.WriteAllBytes(temp, saved);
                    File.Delete(png);
                    File.Move(temp, png);
                    break;
                case "original renamed away, temp renamed in, backup deleted":
                    File.WriteAllBytes(temp, saved);
                    File.Move(png, backup);
                    File.Move(temp, png);
                    File.Delete(backup);
                    break;
                case "File.Replace":
                    File.WriteAllBytes(temp, saved);
                    File.Replace(temp, png, null);
                    break;
            }

            // A file written after the save: once it is listed, every event the save raised has been applied.
            File.WriteAllBytes(Path.Combine(_dir, "zz_marker.txt"), [0]);
            var clock = Stopwatch.StartNew();
            while (!container.ContainsFile("zz_marker.txt") && clock.ElapsedMilliseconds < 5000)
                Thread.Sleep(20);
            Assert.That(container.ContainsFile("zz_marker.txt"), Is.True, "the watcher never applied the marker");

            Assert.That(container.ContainsFile("tex.png"), Is.True, "the saved file dropped off the list");
            Assert.That(container.FindFile("tex.png")!.DataSource.ReadData(), Is.EqualTo(saved));
            Assert.That(container.FindFile("tex.png")!.DataSource.Size, Is.EqualTo(saved.Length), "the entry kept the old file's size");
            Assert.That(container.ContainsFile("~ps8F2A.tmp"), Is.False);
            Assert.That(container.ContainsFile("tex.png.bak"), Is.False);
        }
    }
}
