using Microsoft.EntityFrameworkCore;
using Shared.Core.PackFiles.Models;
using Shared.Core.PackFiles.Models.Containers;
using Shared.Core.PackFiles.Models.FileSources;
using Shared.Core.PackFiles.Serialization.CacheDatabase;
using Shared.Core.PackFiles.Utility;

namespace Shared.CoreTest.PackFiles.Serialization
{
    internal class PackFileContainerCacheHelperTests
    {
        private string _tempDir;
        private string _dbFilePath;

        [SetUp]
        public void Setup()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "AssetEditorCacheTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDir);
            _dbFilePath = Path.Combine(_tempDir, "test_cache.db");
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, true);
        }

        private DbContextOptions<CacheDbContext> CreateTestDbOptions()
        {
            return PackFileContainerCacheHelper.CreateDbOptions(_dbFilePath);
        }

        [Test]
        public void RoundTrip_PreservesMetadata()
        {
            // Arrange
            var container = new PackFileContainer("All Game Packs - TestGame")
            {
                IsCaPackFile = true,
                SystemFilePath = @"c:\game\data"
            };

            container.SourcePackFilePaths.Add(@"c:\game\data\pack1.pack");
            container.SourcePackFilePaths.Add(@"c:\game\data\pack2.pack");

            var parent1 = new PackedFileSourceParent { FilePath = @"c:\game\data\pack1.pack" };
            var parent2 = new PackedFileSourceParent { FilePath = @"c:\game\data\pack2.pack" };

            var source1 = new PackedFileSource(parent1, 100, 500, false, false, CompressionFormat.None, 0);
            var source2 = new PackedFileSource(parent2, 200, 1000, false, true, CompressionFormat.Lz4, 2000);

            container.AddOrUpdateFile("folder\\file1.txt", new PackFile("file1.txt", source1));
            container.AddOrUpdateFile("folder\\file2.bin", new PackFile("file2.bin", source2));

            // Act
            var dbOptions = CreateTestDbOptions();
            PackFileContainerCacheHelper.SaveCache("fingerprint123", container, dbOptions);
            var loaded = PackFileContainerCacheHelper.LoadContainerFromCache(dbOptions, "fingerprint123");

            // Assert
            Assert.That(loaded, Is.Not.Null);
            Assert.That(loaded.Name, Is.EqualTo("All Game Packs - TestGame"));
            Assert.That(loaded.SystemFilePath, Is.EqualTo(@"c:\game\data"));
            Assert.That(loaded.SourcePackFilePaths.Count, Is.EqualTo(2));
            Assert.That(loaded.GetFileCount(), Is.EqualTo(2));
        }

        [Test]
        public void LoadCache_ReturnsCorrectFileData()
        {
            // Arrange
            var container = new PackFileContainer("Test Container")
            {
                IsCaPackFile = true,
                SystemFilePath = @"c:\game\data"
            };

            container.SourcePackFilePaths.Add(@"c:\game\data\pack1.pack");

            var parent = new PackedFileSourceParent { FilePath = @"c:\game\data\pack1.pack" };
            container.AddOrUpdateFile("folder\\file.txt", new PackFile("file.txt",
                new PackedFileSource(parent, 512, 1024, false, false, CompressionFormat.None, 0)));
            container.AddOrUpdateFile("other\\data.bin", new PackFile("data.bin",
                new PackedFileSource(parent, 2048, 4096, false, true, CompressionFormat.Lz4, 8192)));

            var dbOptions = CreateTestDbOptions();
            PackFileContainerCacheHelper.SaveCache("fp", container, dbOptions);

            // Act
            var loaded = PackFileContainerCacheHelper.LoadContainerFromCache(dbOptions, "fp");

            // Assert
            Assert.That(loaded, Is.Not.Null);
            Assert.That(loaded.GetFileCount(), Is.EqualTo(2));

            var file1 = loaded.FindFile("folder\\file.txt");
            Assert.That(file1, Is.Not.Null);
            Assert.That(file1.Name, Is.EqualTo("file.txt"));
            var file1Source = file1.DataSource as PackedFileSource;
            Assert.That(file1Source, Is.Not.Null);
            Assert.That(file1Source.Offset, Is.EqualTo(512));
            Assert.That(file1Source.Size, Is.EqualTo(1024));
            Assert.That(file1Source.IsCompressed, Is.False);
            Assert.That(file1Source.Parent.FilePath, Is.EqualTo(@"c:\game\data\pack1.pack"));

            var file2 = loaded.FindFile("other\\data.bin");
            var file2Source = file2.DataSource as PackedFileSource;
            Assert.That(file2Source, Is.Not.Null);
            Assert.That(file2Source.Offset, Is.EqualTo(2048));
            Assert.That(file2Source.Size, Is.EqualTo(4096));
            Assert.That(file2Source.IsCompressed, Is.True);
            Assert.That(file2Source.CompressionFormat, Is.EqualTo(CompressionFormat.Lz4));
            Assert.That(file2Source.UncompressedSize, Is.EqualTo(8192));
        }

        [Test]
        public void LoadCache_PreservesSourcePackFilePath()
        {
            var container = new PackFileContainer("Test")
            {
                SystemFilePath = @"c:\game"
            };

            var parent = new PackedFileSourceParent { FilePath = @"c:\pack.pack" };
            container.AddOrUpdateFile("a.txt", new PackFile("a.txt",
                new PackedFileSource(parent, 0, 10, false, false, CompressionFormat.None, 0)));
            container.AddOrUpdateFile("b.txt", new PackFile("b.txt",
                new PackedFileSource(parent, 10, 20, false, false, CompressionFormat.None, 0)));

            var dbOptions = CreateTestDbOptions();
            PackFileContainerCacheHelper.SaveCache("fp", container, dbOptions);
            var loaded = PackFileContainerCacheHelper.LoadContainerFromCache(dbOptions, "fp");

            var sourceA = (PackedFileSource)loaded!.FindFile("a.txt")!.DataSource;
            var sourceB = (PackedFileSource)loaded!.FindFile("b.txt")!.DataSource;
            Assert.That(sourceA.Parent.FilePath, Is.EqualTo(@"c:\pack.pack"));
            Assert.That(sourceB.Parent.FilePath, Is.EqualTo(@"c:\pack.pack"));
        }

        [Test]
        public void LoadCache_ReturnsNullForMissingFile()
        {
            var result = PackFileContainerCacheHelper.LoadContainerFromCache(
                Path.Combine(_tempDir, "nonexistent.db"), "fp");
            Assert.That(result, Is.Null);
        }

        [Test]
        public void LoadCache_ReturnsNullForWrongFingerprint()
        {
            var container = new PackFileContainer("Test")
            {
                SystemFilePath = @"c:\game"
            };

            var dbOptions = CreateTestDbOptions();
            PackFileContainerCacheHelper.SaveCache("correct_fp", container, dbOptions);

            var result = PackFileContainerCacheHelper.LoadContainerFromCache(dbOptions, "wrong_fp");
            Assert.That(result, Is.Null);
        }

        [Test]
        public void ComputeFingerprint_DeterministicForSameInputs()
        {
            var packDir = Path.Combine(_tempDir, "gamedata");
            Directory.CreateDirectory(packDir);
            File.WriteAllText(Path.Combine(packDir, "a.pack"), "data_a");
            File.WriteAllText(Path.Combine(packDir, "b.pack"), "data_b");
            var packFiles = new List<string> { "a.pack", "b.pack" };

            var fp1 = PackFileContainerCacheHelper.ComputeFingerprint(packDir, packFiles);
            var fp2 = PackFileContainerCacheHelper.ComputeFingerprint(packDir, packFiles);

            Assert.That(fp1, Is.EqualTo(fp2));
        }

        [Test]
        public void ComputeFingerprint_ChangesWhenFileChanges()
        {
            var packDir = Path.Combine(_tempDir, "gamedata2");
            Directory.CreateDirectory(packDir);
            File.WriteAllText(Path.Combine(packDir, "a.pack"), "data_a");
            var packFiles = new List<string> { "a.pack" };

            var fp1 = PackFileContainerCacheHelper.ComputeFingerprint(packDir, packFiles);

            File.WriteAllText(Path.Combine(packDir, "a.pack"), "data_a_modified_longer");

            var fp2 = PackFileContainerCacheHelper.ComputeFingerprint(packDir, packFiles);

            Assert.That(fp1, Is.Not.EqualTo(fp2));
        }

        [Test]
        public void RoundTrip_FullCycle()
        {
            // Arrange
            var container = new PackFileContainer("Full Cycle Test")
            {
                IsCaPackFile = true,
                SystemFilePath = @"c:\game\data"
            };

            var parent = new PackedFileSourceParent { FilePath = @"c:\game\data\main.pack" };
            container.SourcePackFilePaths.Add(parent.FilePath);

            container.AddOrUpdateFile("db\\units.bin", new PackFile("units.bin",
                new PackedFileSource(parent, 0, 256, false, false, CompressionFormat.None, 0)));
            container.AddOrUpdateFile("text\\localisation.loc", new PackFile("localisation.loc",
                new PackedFileSource(parent, 256, 512, false, true, CompressionFormat.Lz4, 1024)));

            // Act: save ? load
            var dbOptions = CreateTestDbOptions();
            PackFileContainerCacheHelper.SaveCache("test_fp", container, dbOptions);
            var restored = PackFileContainerCacheHelper.LoadContainerFromCache(dbOptions, "test_fp");

            // Assert
            Assert.That(restored, Is.Not.Null);
            Assert.That(restored.Name, Is.EqualTo("Full Cycle Test"));
            Assert.That(restored.IsCaPackFile, Is.True);
            Assert.That(restored.SystemFilePath, Is.EqualTo(@"c:\game\data"));
            Assert.That(restored.GetFileCount(), Is.EqualTo(2));

            Assert.That(restored.FindFile("db\\units.bin"), Is.Not.Null);
            Assert.That(restored.FindFile("text\\localisation.loc"), Is.Not.Null);

            var restoredSource = (PackedFileSource)restored.FindFile("text\\localisation.loc")!.DataSource;
            Assert.That(restoredSource.Offset, Is.EqualTo(256));
            Assert.That(restoredSource.Size, Is.EqualTo(512));
            Assert.That(restoredSource.IsCompressed, Is.True);
            Assert.That(restoredSource.CompressionFormat, Is.EqualTo(CompressionFormat.Lz4));
            Assert.That(restoredSource.UncompressedSize, Is.EqualTo(1024));
        }

        [Test]
        public void TryLoadFromCache_ReturnsContainerWhenValid()
        {
            var container = new PackFileContainer("TryLoad Test")
            {
                SystemFilePath = @"c:\game\data"
            };

            var parent = new PackedFileSourceParent { FilePath = @"c:\game\data\pack.pack" };
            container.AddOrUpdateFile("test\\file.txt", new PackFile("file.txt",
                new PackedFileSource(parent, 0, 100, false, false, CompressionFormat.None, 0)));

            var dbOptions = CreateTestDbOptions();
            PackFileContainerCacheHelper.SaveCache("fp_try", container, dbOptions);

            var result = PackFileContainerCacheHelper.TryLoadFromCache(_dbFilePath, "fp_try");
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Name, Is.EqualTo("TryLoad Test"));
        }

        [Test]
        public void TryLoadFromCache_ReturnsNullForMissingFile()
        {
            var result = PackFileContainerCacheHelper.TryLoadFromCache(
                Path.Combine(_tempDir, "does_not_exist.db"), "fp");
            Assert.That(result, Is.Null);
        }

        [Test]
        public void TryLoadFromCache_ReturnsNullForCorruptFile()
        {
            File.WriteAllBytes(_dbFilePath, [0xFF, 0xFE, 0x00, 0x01]);
            var result = PackFileContainerCacheHelper.TryLoadFromCache(_dbFilePath, "fp");
            Assert.That(result, Is.Null);
        }

        [Test]
        public void SaveCache_PreservesEncryptedFlag()
        {
            var container = new PackFileContainer("Encrypted Test")
            {
                SystemFilePath = @"c:\game"
            };

            var parent = new PackedFileSourceParent { FilePath = @"c:\game\encrypted.pack" };
            container.AddOrUpdateFile("secret\\data.bin", new PackFile("data.bin",
                new PackedFileSource(parent, 0, 500, isEncrypted: true, isCompressed: false, CompressionFormat.None, 0)));

            var dbOptions = CreateTestDbOptions();
            PackFileContainerCacheHelper.SaveCache("fp", container, dbOptions);
            var loaded = PackFileContainerCacheHelper.LoadContainerFromCache(dbOptions, "fp");

            var source = (PackedFileSource)loaded!.FindFile("secret\\data.bin")!.DataSource;
            Assert.That(source.IsEncrypted, Is.True);
        }

        [Test]
        public void SaveCache_EmptyContainer_RoundTrips()
        {
            var container = new PackFileContainer("Empty Pack")
            {
                SystemFilePath = @"c:\game\empty"
            };

            var dbOptions = CreateTestDbOptions();
            PackFileContainerCacheHelper.SaveCache("fp_empty", container, dbOptions);
            var loaded = PackFileContainerCacheHelper.LoadContainerFromCache(dbOptions, "fp_empty");

            Assert.That(loaded, Is.Not.Null);
            Assert.That(loaded.Name, Is.EqualTo("Empty Pack"));
            Assert.That(loaded.GetFileCount(), Is.EqualTo(0));
        }

        [Test]
        public void SaveCache_OverwritesExistingCache()
        {
            var parent = new PackedFileSourceParent { FilePath = @"c:\game\pack.pack" };
            var dbOptions = CreateTestDbOptions();

            // Save first version
            var container1 = new PackFileContainer("Version1") { SystemFilePath = @"c:\game" };
            container1.AddOrUpdateFile("old.txt", new PackFile("old.txt",
                new PackedFileSource(parent, 0, 10, false, false, CompressionFormat.None, 0)));
            PackFileContainerCacheHelper.SaveCache("fp1", container1, dbOptions);

            // Save second version (same db path)
            var container2 = new PackFileContainer("Version2") { SystemFilePath = @"c:\game" };
            container2.AddOrUpdateFile("new.txt", new PackFile("new.txt",
                new PackedFileSource(parent, 0, 20, false, false, CompressionFormat.None, 0)));
            PackFileContainerCacheHelper.SaveCache("fp2", container2, dbOptions);

            // Old fingerprint should fail
            var oldResult = PackFileContainerCacheHelper.LoadContainerFromCache(dbOptions, "fp1");
            Assert.That(oldResult, Is.Null);

            // New fingerprint should work
            var newResult = PackFileContainerCacheHelper.LoadContainerFromCache(dbOptions, "fp2");
            Assert.That(newResult, Is.Not.Null);
            Assert.That(newResult.Name, Is.EqualTo("Version2"));
            Assert.That(newResult.GetFileCount(), Is.EqualTo(1));
            Assert.That(newResult.FindFile("new.txt"), Is.Not.Null);
        }

        [Test]
        public void SaveCache_StoresFolderPathCorrectly()
        {
            var container = new PackFileContainer("FolderPath Test")
            {
                SystemFilePath = @"c:\game"
            };

            var parent = new PackedFileSourceParent { FilePath = @"c:\game\pack.pack" };
            container.AddOrUpdateFile("a\\folder_marker.txt", new PackFile("folder_marker.txt",
                new PackedFileSource(parent, 0, 10, false, false, CompressionFormat.None, 0)));
            container.AddOrUpdateFile("a\\b\\c\\file.txt", new PackFile("file.txt",
                new PackedFileSource(parent, 10, 10, false, false, CompressionFormat.None, 0)));
            container.AddOrUpdateFile("root_file.txt", new PackFile("root_file.txt",
                new PackedFileSource(parent, 20, 20, false, false, CompressionFormat.None, 0)));

            var dbOptions = CreateTestDbOptions();
            PackFileContainerCacheHelper.SaveCache("fp", container, dbOptions);

            // Verify via the CachedPackFileContainer's GetDirectoryContent
            var loaded = PackFileContainerCacheHelper.LoadContainerFromCache(dbOptions, "fp");
            Assert.That(loaded, Is.Not.Null);

            var rootContent = PackFileServiceUtility.SplitDirectoryEntries(loaded, "");
            Assert.That(rootContent.Files.Any(f => f.FileName == "root_file.txt"), Is.True);
            Assert.That(rootContent.SubFolders, Does.Contain("a"));

            var deepContent = PackFileServiceUtility.SplitDirectoryEntries(loaded, "a\\b\\c");
            Assert.That(deepContent.Files.Any(f => f.FileName == "file.txt"), Is.True);
        }

        [Test]
        public void SaveCache_SkipsNonPackedFileSources()
        {
            var container = new PackFileContainer("Mixed Sources")
            {
                SystemFilePath = @"c:\game"
            };

            var parent = new PackedFileSourceParent { FilePath = @"c:\game\pack.pack" };
            container.AddOrUpdateFile("packed.txt", new PackFile("packed.txt",
                new PackedFileSource(parent, 0, 10, false, false, CompressionFormat.None, 0)));
            container.AddOrUpdateFile("memory.txt", new PackFile("memory.txt",
                new MemorySource([1, 2, 3])));

            var dbOptions = CreateTestDbOptions();
            PackFileContainerCacheHelper.SaveCache("fp", container, dbOptions);
            var loaded = PackFileContainerCacheHelper.LoadContainerFromCache(dbOptions, "fp");

            Assert.That(loaded, Is.Not.Null);
            Assert.That(loaded.GetFileCount(), Is.EqualTo(1));
            Assert.That(loaded.FindFile("packed.txt"), Is.Not.Null);
            Assert.That(loaded.FindFile("memory.txt"), Is.Null);
        }

        [Test]
        public void ComputeFingerprint_IgnoresMissingPackFiles()
        {
            var packDir = Path.Combine(_tempDir, "partial");
            Directory.CreateDirectory(packDir);
            File.WriteAllText(Path.Combine(packDir, "exists.pack"), "data");

            var packFiles = new List<string> { "exists.pack", "missing.pack" };
            var fp = PackFileContainerCacheHelper.ComputeFingerprint(packDir, packFiles);

            Assert.That(fp, Is.Not.Null.And.Not.Empty);

            // Same result regardless of missing file in list
            var fp2 = PackFileContainerCacheHelper.ComputeFingerprint(packDir, new List<string> { "exists.pack", "missing.pack" });
            Assert.That(fp, Is.EqualTo(fp2));
        }

        [Test]
        public void ComputeFingerprint_OrderIndependent()
        {
            var packDir = Path.Combine(_tempDir, "order_test");
            Directory.CreateDirectory(packDir);
            File.WriteAllText(Path.Combine(packDir, "alpha.pack"), "aaa");
            File.WriteAllText(Path.Combine(packDir, "beta.pack"), "bbb");
            File.WriteAllText(Path.Combine(packDir, "gamma.pack"), "ccc");

            var fp1 = PackFileContainerCacheHelper.ComputeFingerprint(packDir,
                new List<string> { "gamma.pack", "alpha.pack", "beta.pack" });
            var fp2 = PackFileContainerCacheHelper.ComputeFingerprint(packDir,
                new List<string> { "alpha.pack", "beta.pack", "gamma.pack" });
            var fp3 = PackFileContainerCacheHelper.ComputeFingerprint(packDir,
                new List<string> { "beta.pack", "gamma.pack", "alpha.pack" });

            Assert.That(fp1, Is.EqualTo(fp2));
            Assert.That(fp2, Is.EqualTo(fp3));
        }

        [Test]
        public void GetCacheFilePath_SanitizesInvalidChars()
        {
            var path = PackFileContainerCacheHelper.GetCacheFilePath(@"c:\game", "Game:Name/With<Bad>Chars", "abc123");
            var fileName = Path.GetFileName(path);

            Assert.That(fileName.IndexOfAny(Path.GetInvalidFileNameChars()), Is.EqualTo(-1));
            Assert.That(path.EndsWith(".db"), Is.True);
        }
    }
}
