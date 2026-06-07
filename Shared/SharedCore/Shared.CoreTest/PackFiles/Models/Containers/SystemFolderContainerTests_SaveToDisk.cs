using Moq;
using Shared.Core.PackFiles.Models;
using Shared.Core.PackFiles.Models.Containers;
using Shared.Core.PackFiles.Models.FileSources;
using Shared.Core.PackFiles.Serialization;
using Shared.Core.PackFiles.Utility;
using Shared.Core.Services;
using Shared.Core.Settings;

namespace Shared.CoreTest.PackFiles.Models.Containers
{
    [TestFixture]
    internal class SystemFolderContainerTests_SaveToDisk
    {
        private string _tempDir = null!;
        private string _outputDir = null!;
        private SystemFolderContainer _container = null!;
        private Mock<IFileSystemAccess> _fileSystemAccess = null!;
        private GameInformation _gameInfo = null!;

        [SetUp]
        public void Setup()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "SysFolderSave_" + Guid.NewGuid().ToString("N"));
            _outputDir = Path.Combine(Path.GetTempPath(), "SysFolderSaveOutput_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDir);
            Directory.CreateDirectory(_outputDir);

            // Create test files
            var fileA = Path.Combine(_tempDir, "folder", "fileA.txt");
            var fileB = Path.Combine(_tempDir, "fileB.bin");
            Directory.CreateDirectory(Path.GetDirectoryName(fileA)!);
            File.WriteAllText(fileA, "content A");
            File.WriteAllBytes(fileB, new byte[] { 1, 2, 3, 4, 5 });

            _fileSystemAccess = new Mock<IFileSystemAccess>();
            _fileSystemAccess.Setup(x => x.DirectoryExists(It.IsAny<string>()))
                .Returns((string p) => Directory.Exists(p));
            _fileSystemAccess.Setup(x => x.DirectoryGetFiles(_tempDir, "*.*", SearchOption.AllDirectories))
                .Returns([fileA, fileB]);
            _fileSystemAccess.Setup(x => x.FileExists(It.IsAny<string>()))
                .Returns((string p) => File.Exists(p));
            _fileSystemAccess.Setup(x => x.FileDelete(It.IsAny<string>()))
                .Callback((string p) => File.Delete(p));
            _fileSystemAccess.Setup(x => x.FileMove(It.IsAny<string>(), It.IsAny<string>()))
                .Callback((string s, string d) => File.Move(s, d, overwrite: true));

            _container = new SystemFolderContainer(_tempDir, _fileSystemAccess.Object);
            _gameInfo = GameInformationDatabase.GetGameById(GameTypeEnum.Warhammer3);
        }

        [TearDown]
        public void TearDown()
        {
            _container.Dispose();
            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, true);
            if (Directory.Exists(_outputDir))
                Directory.Delete(_outputDir, true);
        }

        [Test]
        public void SaveToDisk_GeneratesValidPackFile()
        {
            var outputPath = Path.Combine(_outputDir, "output.pack");

            _container.SaveToDisk(outputPath, false, _gameInfo);

            Assert.That(File.Exists(outputPath), Is.True);

            // Verify the .pack can be reloaded
            using var fileStream = File.OpenRead(outputPath);
            using var reader = new BinaryReader(fileStream);
            var loaded = PackFileSerializerLoader.Load(outputPath, fileStream.Length, reader, new CaPackDuplicateFileResolver());

            Assert.That(loaded.GetFileCount(), Is.EqualTo(2));
            Assert.That(loaded.FindFile(@"folder\filea.txt"), Is.Not.Null);
            Assert.That(loaded.FindFile(@"fileb.bin"), Is.Not.Null);
        }

        [Test]
        public void SaveToDisk_ContainerRemainsActive()
        {
            var outputPath = Path.Combine(_outputDir, "output.pack");

            _container.SaveToDisk(outputPath, false, _gameInfo);

            // Container should still work normally
            Assert.That(_container.GetFileCount(), Is.EqualTo(2));
            Assert.That(_container.FindFile(@"folder\fileA.txt"), Is.Not.Null);
            Assert.That(_container.SystemFilePath, Is.EqualTo(_tempDir));
        }

        [Test]
        public void SaveToDisk_CreateBackup_BackupCreated()
        {
            var outputPath = Path.Combine(_outputDir, "output.pack");

            // First save - creates the file
            _container.SaveToDisk(outputPath, false, _gameInfo);
            Assert.That(File.Exists(outputPath), Is.True);

            // Second save with backup
            _container.SaveToDisk(outputPath, true, _gameInfo);

            // Backup folder should exist with a file
            var backupFolder = Path.Combine(_outputDir, "Backup");
            Assert.That(Directory.Exists(backupFolder), Is.True);
            var backupFiles = Directory.GetFiles(backupFolder, "output*");
            Assert.That(backupFiles.Length, Is.GreaterThan(0));
        }

        [Test]
        public void SaveToDisk_LockedFile_ThrowsIOException()
        {
            var outputPath = Path.Combine(_outputDir, "locked.pack");

            // Create and lock the file
            using var lockStream = new FileStream(outputPath, FileMode.Create, FileAccess.ReadWrite, FileShare.None);

            Assert.Throws<IOException>(() => _container.SaveToDisk(outputPath, false, _gameInfo));
        }

        [Test]
        public void SaveToDisk_FileContentPreserved()
        {
            var outputPath = Path.Combine(_outputDir, "output.pack");

            _container.SaveToDisk(outputPath, false, _gameInfo);

            using var fileStream = File.OpenRead(outputPath);
            using var reader = new BinaryReader(fileStream);
            var loaded = PackFileSerializerLoader.Load(outputPath, fileStream.Length, reader, new CaPackDuplicateFileResolver());

            var fileA = loaded.FindFile(@"folder\filea.txt");
            Assert.That(fileA, Is.Not.Null);

            var dataA = ((PackedFileSource)fileA!.DataSource).ReadData(fileStream);
            Assert.That(System.Text.Encoding.UTF8.GetString(dataA), Is.EqualTo("content A"));

            var fileB = loaded.FindFile(@"fileb.bin");
            Assert.That(fileB, Is.Not.Null);

            var dataB = ((PackedFileSource)fileB!.DataSource).ReadData(fileStream);
            Assert.That(dataB, Is.EqualTo(new byte[] { 1, 2, 3, 4, 5 }));
        }
    }
}
