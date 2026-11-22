using Shared.Core.PackFiles.Utility;
using Shared.Core.Settings;

namespace Shared.CoreTest.PackFiles.Utility
{
    [TestFixture]
    internal class PackBackupServiceTests
    {
        private string _dataDir = null!;
        private string _packPath = null!;
        private ApplicationSettingsService _settings = null!;

        [SetUp]
        public void Setup()
        {
            _dataDir = Path.Combine(Path.GetTempPath(), "PackBackup_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_dataDir);
            _packPath = Path.Combine(_dataDir, "mymod.pack");
            File.WriteAllBytes(_packPath, [1, 2, 3, 4]);
            _settings = new ApplicationSettingsService(GameTypeEnum.Warhammer3);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_dataDir))
                Directory.Delete(_dataDir, true);
        }

        [Test]
        public void BackupOnOpen_CopiesThePackIntoAFolderNextToIt()
        {
            var backupPath = new PackBackupService(_settings).BackupOnOpen(_packPath);

            Assert.That(backupPath, Is.EqualTo(Path.Combine(_dataDir, "AssetEditorBackups", "mymod.pack")));
            Assert.That(File.ReadAllBytes(backupPath!), Is.EqualTo(File.ReadAllBytes(_packPath)));
        }

        [Test]
        public void BackupOnOpen_SecondOpen_ReplacesTheSingleSlot()
        {
            var service = new PackBackupService(_settings);
            service.BackupOnOpen(_packPath);
            File.WriteAllBytes(_packPath, [9, 9]);

            var backupPath = service.BackupOnOpen(_packPath);

            Assert.That(File.ReadAllBytes(backupPath!), Is.EqualTo(new byte[] { 9, 9 }));
            Assert.That(Directory.GetFiles(Path.GetDirectoryName(backupPath)!), Has.Length.EqualTo(1));
        }

        [Test]
        public void BackupOnOpen_CopyFails_PreviousBackupKept()
        {
            var service = new PackBackupService(_settings);
            var backupPath = service.BackupOnOpen(_packPath)!;

            // Another program holds the pack exclusively, so it can't be copied
            using (new FileStream(_packPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                Assert.Throws<IOException>(() => service.BackupOnOpen(_packPath));

            Assert.That(File.ReadAllBytes(backupPath), Is.EqualTo(new byte[] { 1, 2, 3, 4 }));
            Assert.That(File.Exists(backupPath + "_temp"), Is.False);
        }

        [Test]
        public void BackupOnOpen_BackupsTurnedOff_WritesNothing()
        {
            _settings.CurrentSettings.BackupPacksOnOpen = false;

            var backupPath = new PackBackupService(_settings).BackupOnOpen(_packPath);

            Assert.That(backupPath, Is.Null);
            Assert.That(Directory.Exists(Path.Combine(_dataDir, "AssetEditorBackups")), Is.False);
        }
    }
}
