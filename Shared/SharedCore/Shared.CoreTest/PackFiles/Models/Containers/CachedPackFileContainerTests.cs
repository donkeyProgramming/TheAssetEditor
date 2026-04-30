using Microsoft.EntityFrameworkCore;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.PackFiles.Models.Containers;
using Shared.Core.PackFiles.Models.FileSources;
using Shared.Core.PackFiles.Serialization;
using Shared.Core.PackFiles.Serialization.CacheDatabase;
using Shared.Core.PackFiles.Utility;
using Shared.Core.Settings;

namespace Shared.CoreTest.PackFiles.Models.Containers
{
    internal class CachedPackFileContainer_ReadOnly
    {
        private string _tempDir;
        private string _dbFilePath;
        private CachedPackFileContainer _container;

        [SetUp]
        public void Setup()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "CachedContainerTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDir);
            _dbFilePath = Path.Combine(_tempDir, "test.db");

            // Build a container with test data and save to DB
            var sourceContainer = new PackFileContainer("TestCache")
            {
                IsCaPackFile = true,
                SystemFilePath = @"c:\game\data"
            };
            sourceContainer.SourcePackFilePaths.Add(@"c:\game\data\pack1.pack");

            var parent = new PackedFileSourceParent { FilePath = @"c:\game\data\pack1.pack" };
            sourceContainer.AddOrUpdateFile("folder\\file.txt", new PackFile("file.txt",
                new PackedFileSource(parent, 100, 200, false, false, CompressionFormat.None, 0)));
            sourceContainer.AddOrUpdateFile("other\\data.bin", new PackFile("data.bin",
                new PackedFileSource(parent, 300, 400, false, true, CompressionFormat.Lz4, 800)));
            sourceContainer.AddOrUpdateFile("audio\\sound.wem", new PackFile("sound.wem",
                new PackedFileSource(parent, 700, 500, false, false, CompressionFormat.None, 0)));

            var dbOptions = PackFileContainerCacheHelper.CreateDbOptions(_dbFilePath);
            PackFileContainerCacheHelper.SaveCache("test_fp", sourceContainer, dbOptions);

            // Load the cached container (lazy, no files in memory)
            _container = PackFileContainerCacheHelper.LoadContainerFromCache(dbOptions, "test_fp")!;
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, true);
        }

        [Test]
        public void IsCaPackFile_AlwaysTrue()
        {
            Assert.That(_container.IsCaPackFile, Is.True);
        }

        [Test]
        public void IsCaPackFile_SetterDoesNotChangeValue()
        {
            _container.IsCaPackFile = false;
            Assert.That(_container.IsCaPackFile, Is.True);
        }

        [Test]
        public void GetFileCount_ReturnsCorrectCount()
        {
            Assert.That(_container.GetFileCount(), Is.EqualTo(3));
        }

        [Test]
        public void FindFile_ReturnsFile()
        {
            var result = _container.FindFile("folder\\file.txt");
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Name, Is.EqualTo("file.txt"));
        }

        [Test]
        public void FindFile_ReturnsNullForMissing()
        {
            var result = _container.FindFile("missing\\path.txt");
            Assert.That(result, Is.Null);
        }

        [Test]
        public void FindFile_NormalizesPath()
        {
            var result = _container.FindFile("FOLDER/FILE.TXT");
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public void FindFile_ReturnsCorrectDataSource()
        {
            var result = _container.FindFile("folder\\file.txt");
            var source = result!.DataSource as PackedFileSource;
            Assert.That(source, Is.Not.Null);
            Assert.That(source.Offset, Is.EqualTo(100));
            Assert.That(source.Size, Is.EqualTo(200));
        }

        [Test]
        public void ContainsFile_ReturnsTrueForExisting()
        {
            Assert.That(_container.ContainsFile("folder\\file.txt"), Is.True);
        }

        [Test]
        public void ContainsFile_ReturnsFalseForMissing()
        {
            Assert.That(_container.ContainsFile("missing.txt"), Is.False);
        }

        [Test]
        public void GetFullPath_ReturnsPath()
        {
            var file = _container.FindFile("folder\\file.txt")!;
            var path = _container.GetFullPath(file);
            Assert.That(path, Is.EqualTo("folder\\file.txt"));
        }

        [Test]
        public void GetFullPath_ReturnsNullForMissing()
        {
            var unknownFile = new PackFile("unknown.txt", null);
            var path = _container.GetFullPath(unknownFile);
            Assert.That(path, Is.Null);
        }

        [Test]
        public void FindAllWithExtention_ReturnsMatching()
        {
            var results = _container.FindAllWithExtention(".wem");
            Assert.That(results.Count, Is.EqualTo(1));
            Assert.That(results[0].FileName, Does.EndWith("sound.wem"));
        }

        [Test]
        public void FindAllWithExtention_ReturnsEmptyForNoMatch()
        {
            var results = _container.FindAllWithExtention(".xyz");
            Assert.That(results, Is.Empty);
        }

        [Test]
        public void GetAllFiles_ReturnsAllFiles()
        {
            var all = _container.GetAllFiles();
            Assert.That(all.Count, Is.EqualTo(3));
            Assert.That(all.ContainsKey("folder\\file.txt"), Is.True);
            Assert.That(all.ContainsKey("other\\data.bin"), Is.True);
            Assert.That(all.ContainsKey("audio\\sound.wem"), Is.True);
        }

        [Test]
        public void AddFiles_Throws()
        {
            var newFiles = new List<NewPackFileEntry>
            {
                new("folder", new PackFile("new.txt", null))
            };
            Assert.Throws<InvalidOperationException>(() => _container.AddFiles(newFiles));
        }

        [Test]
        public void DeleteFile_Throws()
        {
            var file = _container.FindFile("folder\\file.txt")!;
            Assert.Throws<InvalidOperationException>(() => _container.DeleteFile(file));
        }

        [Test]
        public void DeleteFolder_Throws()
        {
            Assert.Throws<InvalidOperationException>(() => _container.DeleteFolder("folder"));
        }

        [Test]
        public void MoveFile_Throws()
        {
            var file = _container.FindFile("folder\\file.txt")!;
            Assert.Throws<InvalidOperationException>(() => _container.MoveFile(file, "other"));
        }

        [Test]
        public void RenameDirectory_Throws()
        {
            Assert.Throws<InvalidOperationException>(() => _container.RenameDirectory("folder", "renamed"));
        }

        [Test]
        public void RenameFile_Throws()
        {
            var file = _container.FindFile("folder\\file.txt")!;
            Assert.Throws<InvalidOperationException>(() => _container.RenameFile(file, "renamed.txt"));
        }

        [Test]
        public void SaveFileData_Throws()
        {
            var file = _container.FindFile("folder\\file.txt")!;
            Assert.Throws<InvalidOperationException>(() => _container.SaveFileData(file, [1, 2, 3]));
        }

        [Test]
        public void SaveToDisk_Throws()
        {
            var gameInfo = GameInformationDatabase.GetGameById(GameTypeEnum.Warhammer3);
            Assert.Throws<InvalidOperationException>(() => _container.SaveToDisk("path", false, gameInfo));
        }

        [Test]
        public void AddOrUpdateFile_Throws()
        {
            Assert.Throws<InvalidOperationException>(() =>
                _container.AddOrUpdateFile("test\\new.txt", new PackFile("new.txt", null)));
        }
    }
}
