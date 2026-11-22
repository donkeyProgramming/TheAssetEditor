using Moq;
using Shared.Core.Events;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Events;
using Shared.Core.PackFiles.Models;
using Shared.Core.PackFiles.Models.Containers;
using Shared.Core.Settings;
using static Shared.CoreTest.PackFiles.Models.Containers.PackFileContainerTests_SaveSafety;

namespace Shared.CoreTest.PackFiles.Models.Containers
{
    // A pack open in AE that another program (RPFM) then saves
    [TestFixture]
    internal class PackFileContainerTests_ReloadFromDisk
    {
        private static readonly GameInformation s_wh3 = GameInformationDatabase.GetGameById(GameTypeEnum.Warhammer3);

        private string _dir = null!;
        private string _packPath = null!;
        private int _saves;

        [SetUp]
        public void Setup()
        {
            _dir = Path.Combine(Path.GetTempPath(), "PackReload_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_dir);
            _packPath = Path.Combine(_dir, "mymod.pack");
            WritePack(("a.txt", "A"), ("b.txt", "B"), ("c.txt", "C"));
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_dir))
                Directory.Delete(_dir, true);
        }

        // Stands in for RPFM saving the pack: a whole new file at the same path
        private void WritePack(params (string Name, string Content)[] files)
        {
            var writer = PackFileContainer.CreatePackFile("mymod", _packPath, PackFileVersion.PFH5);
            foreach (var (name, content) in files)
                writer.AddOrUpdateFile(@"folder\" + name, PackFile.CreateFromASCII(name, content));
            writer.SaveToDisk(_packPath, false, s_wh3);
            // Each save gets a distinct write time, however fast the test runs
            File.SetLastWriteTimeUtc(_packPath, DateTime.UtcNow.AddMinutes(++_saves));
        }

        [Test]
        public void ReloadFromDisk_TakesTheNewVersion_KeepsUnsavedAeEditsOnTop()
        {
            var pack = LoadFromDisk(_packPath);
            var fileB = pack.FindFile(@"folder\b.txt")!;
            pack.SaveFileData(pack.FindFile(@"folder\a.txt")!, System.Text.Encoding.ASCII.GetBytes("AE EDIT"));
            pack.AddFiles([new NewPackFileEntry("folder", PackFile.CreateFromASCII("new.txt", "AE NEW"))]);

            WritePack(("a.txt", "A2"), ("b.txt", "B2"), ("d.txt", "D"));
            Assert.That(pack.HasChangedOnDisk(), Is.True);

            var kept = pack.ReloadFromDisk();

            Assert.That(pack.HasChangedOnDisk(), Is.False);
            Assert.That(ReadText(pack, @"folder\a.txt"), Is.EqualTo("AE EDIT"));
            Assert.That(ReadText(pack, @"folder\b.txt"), Is.EqualTo("B2"));
            Assert.That(pack.FindFile(@"folder\b.txt"), Is.SameAs(fileB));
            Assert.That(pack.FindFile(@"folder\c.txt"), Is.Null);
            Assert.That(ReadText(pack, @"folder\d.txt"), Is.EqualTo("D"));
            Assert.That(ReadText(pack, @"folder\new.txt"), Is.EqualTo("AE NEW"));
            Assert.That(kept.Select(x => x.Name), Is.EquivalentTo(new[] { "a.txt", "new.txt" }));
        }

        [Test]
        public void ReloadFromDisk_ThenSave_WritesTheNewVersionWithAeEdits()
        {
            var pack = LoadFromDisk(_packPath);
            pack.SaveFileData(pack.FindFile(@"folder\a.txt")!, System.Text.Encoding.ASCII.GetBytes("AE EDIT"));
            WritePack(("a.txt", "A2"), ("b.txt", "B2"), ("d.txt", "D"));

            pack.ReloadFromDisk();
            pack.SaveToDisk(_packPath, false, s_wh3);

            var saved = LoadFromDisk(_packPath);
            Assert.That(saved.GetAllFiles().Keys, Is.EquivalentTo(new[] { @"folder\a.txt", @"folder\b.txt", @"folder\d.txt" }));
            Assert.That(ReadText(saved, @"folder\a.txt"), Is.EqualTo("AE EDIT"));
            Assert.That(ReadText(saved, @"folder\b.txt"), Is.EqualTo("B2"));
        }

        [Test]
        public void PackFileService_Reload_PublishesReloadedWithTheKeptEdits()
        {
            var eventHub = new Mock<IGlobalEventHub>();
            var pfs = new PackFileService(eventHub.Object) { EnforceGameFilesMustBeLoaded = false };
            var pack = LoadFromDisk(_packPath);
            pfs.AddContainer(pack);
            var edited = pack.FindFile(@"folder\a.txt")!;
            pack.SaveFileData(edited, System.Text.Encoding.ASCII.GetBytes("AE EDIT"));
            Assert.That(pfs.HasUnsavedChanges(pack), Is.True);

            WritePack(("a.txt", "A2"));
            Assert.That(pfs.HasChangedOnDisk(pack), Is.True);

            pfs.ReloadPackContainer(pack);

            Assert.That(pfs.HasChangedOnDisk(pack), Is.False);
            eventHub.Verify(x => x.PublishGlobalEvent(It.Is<PackFileContainerReloadedEvent>(e => e.Container == pack && e.UnsavedFiles.Single() == edited)), Times.Once);
        }

        [Test]
        public void HasUnsavedChanges_AfterSave_IsFalse()
        {
            var pfs = new PackFileService(null) { EnforceGameFilesMustBeLoaded = false };
            var pack = LoadFromDisk(_packPath);
            pfs.AddContainer(pack);
            pack.SaveFileData(pack.FindFile(@"folder\a.txt")!, System.Text.Encoding.ASCII.GetBytes("AE EDIT"));

            pack.SaveToDisk(_packPath, false, s_wh3);

            Assert.That(pfs.HasUnsavedChanges(pack), Is.False);
        }
    }
}
