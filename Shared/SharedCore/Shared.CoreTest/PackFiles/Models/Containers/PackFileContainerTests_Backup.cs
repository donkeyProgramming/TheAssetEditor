using Shared.Core.PackFiles.ErrorHandling;
using Shared.Core.PackFiles.Models;
using Shared.Core.PackFiles.Models.Containers;
using Shared.Core.PackFiles.Utility;
using Shared.Core.Settings;
using static Shared.CoreTest.PackFiles.Models.Containers.PackFileContainerTests_SaveSafety;

namespace Shared.CoreTest.PackFiles.Models.Containers
{
    // A pack edited directly keeps the version on disk as <name>.pack1 on its first save after opening or reloading
    [TestFixture]
    internal class PackFileContainerTests_Backup
    {
        private static readonly GameInformation s_wh3 = GameInformationDatabase.GetGameById(GameTypeEnum.Warhammer3);

        private string _dir = null!;
        private string _packPath = null!;
        private string _backupPath = null!;
        private byte[] _original = null!;

        [SetUp]
        public void Setup()
        {
            _dir = Path.Combine(Path.GetTempPath(), "PackBackup_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_dir);
            _packPath = Path.Combine(_dir, "mymod.pack");
            _backupPath = Path.Combine(_dir, "mymod.pack1");

            WritePack(_packPath, ("db\\units_tables\\mymod", "ORIGINAL"));
            _original = File.ReadAllBytes(_packPath);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_dir))
                Directory.Delete(_dir, true);
        }

        private static void WritePack(string path, params (string Path, string Content)[] files)
        {
            var pack = PackFileContainer.CreatePackFile("mymod", path, PackFileVersion.PFH5);
            foreach (var (filePath, content) in files)
                pack.AddOrUpdateFile(filePath, PackFile.CreateFromASCII(Path.GetFileName(filePath), content));
            pack.SaveToDisk(path, false, s_wh3);
        }

        private static void Edit(PackFileContainer pack, string content) =>
            pack.AddOrUpdateFile("text\\edit.txt", PackFile.CreateFromASCII("edit.txt", content));

        [Test]
        public void BackupPath_IsNextToThePack()
        {
            Assert.That(SaveUtility.GetPackBackupPath(_packPath), Is.EqualTo(_backupPath));
        }

        [Test]
        public void FirstSave_KeepsThePackAsItWasAsPack1()
        {
            var pack = LoadFromDisk(_packPath);
            Edit(pack, "first");

            pack.SaveToDisk(_packPath, true, s_wh3);

            Assert.That(File.ReadAllBytes(_backupPath), Is.EqualTo(_original));
            Assert.That(ReadText(LoadFromDisk(_packPath), "text\\edit.txt"), Is.EqualTo("first"));
            Assert.That(Directory.GetFiles(_dir), Has.Length.EqualTo(2), "only the pack and its .pack1");
        }

        [Test]
        public void LaterSaves_LeaveThePack1Alone()
        {
            var pack = LoadFromDisk(_packPath);
            Edit(pack, "first");
            pack.SaveToDisk(_packPath, true, s_wh3);
            Edit(pack, "second");

            pack.SaveToDisk(_packPath, true, s_wh3);

            Assert.That(File.ReadAllBytes(_backupPath), Is.EqualTo(_original));
            Assert.That(ReadText(LoadFromDisk(_packPath), "text\\edit.txt"), Is.EqualTo("second"));
        }

        [Test]
        public void OpenedAgain_FirstSaveReplacesThePack1()
        {
            var first = LoadFromDisk(_packPath);
            Edit(first, "first session");
            first.SaveToDisk(_packPath, true, s_wh3);
            var afterFirstSession = File.ReadAllBytes(_packPath);

            var second = LoadFromDisk(_packPath);
            Edit(second, "second session");
            second.SaveToDisk(_packPath, true, s_wh3);

            Assert.That(File.ReadAllBytes(_backupPath), Is.EqualTo(afterFirstSession));
        }

        [Test]
        public void OpenedButNotSaved_Pack1Untouched()
        {
            var first = LoadFromDisk(_packPath);
            Edit(first, "first session");
            first.SaveToDisk(_packPath, true, s_wh3);

            var second = LoadFromDisk(_packPath);
            Edit(second, "never saved");

            Assert.That(File.ReadAllBytes(_backupPath), Is.EqualTo(_original));
        }

        [Test]
        public void SaveAfterReload_KeepsTheVersionAnotherProgramSaved()
        {
            var pack = LoadFromDisk(_packPath);
            Edit(pack, "first");
            pack.SaveToDisk(_packPath, true, s_wh3);

            WritePack(_packPath, ("db\\units_tables\\mymod", "RPFM EDIT"), ("text\\edit.txt", "first"));
            File.SetLastWriteTimeUtc(_packPath, DateTime.UtcNow.AddMinutes(1));
            var rpfmVersion = File.ReadAllBytes(_packPath);
            pack.ReloadFromDisk();
            Edit(pack, "after reload");

            pack.SaveToDisk(_packPath, true, s_wh3);

            Assert.That(File.ReadAllBytes(_backupPath), Is.EqualTo(rpfmVersion));
            var saved = LoadFromDisk(_packPath);
            Assert.That(ReadText(saved, "db\\units_tables\\mymod"), Is.EqualTo("RPFM EDIT"));
            Assert.That(ReadText(saved, "text\\edit.txt"), Is.EqualTo("after reload"));
        }

        [Test]
        public void WithoutCreateBackup_NoPack1()
        {
            var pack = LoadFromDisk(_packPath);
            Edit(pack, "first");

            pack.SaveToDisk(_packPath, false, s_wh3);

            Assert.That(File.Exists(_backupPath), Is.False);
        }

        [Test]
        public void NewPack_FirstSave_NothingToKeep()
        {
            var newPath = Path.Combine(_dir, "new.pack");
            var pack = PackFileContainer.CreatePackFile("new", null, PackFileVersion.PFH5);
            Edit(pack, "first");
            pack.SaveToDisk(newPath, true, s_wh3);
            Edit(pack, "second");

            pack.SaveToDisk(newPath, true, s_wh3);

            Assert.That(File.Exists(SaveUtility.GetPackBackupPath(newPath)), Is.False);
        }

        [Test]
        public void NewPack_SavedOverAnExistingFile_KeepsThatFile()
        {
            var pack = PackFileContainer.CreatePackFile("new", null, PackFileVersion.PFH5);
            Edit(pack, "new pack");

            pack.SaveToDisk(_packPath, true, s_wh3);

            Assert.That(File.ReadAllBytes(_backupPath), Is.EqualTo(_original));
        }

        [Test]
        public void Pack1CantBeReplaced_NothingWritten_NextSaveTriesAgain()
        {
            File.WriteAllBytes(_backupPath, [1, 2, 3]);
            var pack = LoadFromDisk(_packPath);
            Edit(pack, "first");

            using (new FileStream(_backupPath, FileMode.Open, FileAccess.Read, FileShare.None))
            {
                var e = Assert.Throws<PackBackupFailedException>(() => pack.SaveToDisk(_packPath, true, s_wh3));
                Assert.That(e!.BackupPath, Is.EqualTo(_backupPath));
            }

            Assert.That(File.ReadAllBytes(_packPath), Is.EqualTo(_original));
            Assert.That(File.Exists(_packPath + "_temp"), Is.False);
            Assert.That(ReadText(pack, "db\\units_tables\\mymod"), Is.EqualTo("ORIGINAL"));

            pack.SaveToDisk(_packPath, true, s_wh3);
            Assert.That(File.ReadAllBytes(_backupPath), Is.EqualTo(_original));
        }

        [Test]
        public void WriteThroughTempFile_NewFileCantBeMovedIntoPlace_OldFilePutBack()
        {
            var path = Path.Combine(_dir, "file.bin");
            File.WriteAllBytes(path, [1, 2, 3]);
            var backupPath = path + "1";

            // A second handle without delete sharing blocks the move of the temp file, after the old file has been moved aside
            FileStream? holdTemp = null;
            try
            {
                Assert.Throws<IOException>(() => SaveUtility.WriteThroughTempFile(path, writer =>
                {
                    writer.Write(new byte[] { 9, 9 });
                    writer.Flush();
                    holdTemp = new FileStream(path + "_temp", FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    return 0;
                }, backupPath));
            }
            finally
            {
                holdTemp?.Dispose();
            }

            Assert.That(File.ReadAllBytes(path), Is.EqualTo(new byte[] { 1, 2, 3 }));
            Assert.That(File.Exists(backupPath), Is.False);
        }
    }
}
