using Shared.Core.PackFiles.Models;
using Shared.Core.PackFiles.Serialization;
using Shared.Core.PackFiles.Utility;

namespace Shared.CoreTest.PackFiles.Serialization
{
    internal class PackFileContainerCacheHelperTests
    {
        private string _tempDir;
        private string _cacheFilePath;

        [SetUp]
        public void Setup()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "AssetEditorCacheTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDir);
            _cacheFilePath = Path.Combine(_tempDir, "test_cache.bin");
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, true);
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

            container.FileList["folder\\file1.txt"] = new PackFile("file1.txt", source1);
            container.FileList["folder\\file2.bin"] = new PackFile("file2.bin", source2);

            // Act
            var cacheData = PackFileContainerCacheHelper.BuildCacheData("fingerprint123", container);
            PackFileContainerCacheHelper.SaveCache(cacheData, _cacheFilePath);
            var loaded = PackFileContainerCacheHelper.LoadCache(_cacheFilePath);

            // Assert
            Assert.That(loaded, Is.Not.Null);
            Assert.That(loaded.Fingerprint, Is.EqualTo("fingerprint123"));
            Assert.That(loaded.ContainerName, Is.EqualTo("All Game Packs - TestGame"));
            Assert.That(loaded.SystemFilePath, Is.EqualTo(@"c:\game\data"));
            Assert.That(loaded.SourcePackFilePaths.Count, Is.EqualTo(2));
            Assert.That(loaded.Files.Count, Is.EqualTo(2));
        }

        [Test]
        public void RestoreFromCache_CreatesCorrectContainer()
        {
            // Arrange
            var cacheData = new CachedContainerData
            {
                Fingerprint = "fp",
                ContainerName = "Test Container",
                SystemFilePath = @"c:\game\data",
                SourcePackFilePaths = [@"c:\game\data\pack1.pack"],
                Files =
                [
                    new CachedFileEntry(
                        "folder\\file.txt",
                        "file.txt",
                        @"c:\game\data\pack1.pack",
                        Offset: 512,
                        Size: 1024,
                        IsEncrypted: false,
                        IsCompressed: false,
                        CompressionFormat.None,
                        UncompressedSize: 0),
                    new CachedFileEntry(
                        "other\\data.bin",
                        "data.bin",
                        @"c:\game\data\pack1.pack",
                        Offset: 2048,
                        Size: 4096,
                        IsEncrypted: false,
                        IsCompressed: true,
                        CompressionFormat.Lz4,
                        UncompressedSize: 8192)
                ]
            };

            // Act
            var container = PackFileContainerCacheHelper.RestoreFromCache(cacheData);

            // Assert
            Assert.That(container, Is.InstanceOf<CachedPackFileContainer>());
            Assert.That(container.Name, Is.EqualTo("Test Container"));
            Assert.That(container.IsCaPackFile, Is.True);
            Assert.That(container.SystemFilePath, Is.EqualTo(@"c:\game\data"));
            Assert.That(container.SourcePackFilePaths.Count, Is.EqualTo(1));
            Assert.That(container.FileList.Count, Is.EqualTo(2));

            var file1 = container.FileList["folder\\file.txt"];
            Assert.That(file1.Name, Is.EqualTo("file.txt"));
            var file1Source = file1.DataSource as PackedFileSource;
            Assert.That(file1Source, Is.Not.Null);
            Assert.That(file1Source.Offset, Is.EqualTo(512));
            Assert.That(file1Source.Size, Is.EqualTo(1024));
            Assert.That(file1Source.IsCompressed, Is.False);
            Assert.That(file1Source.Parent.FilePath, Is.EqualTo(@"c:\game\data\pack1.pack"));

            var file2 = container.FileList["other\\data.bin"];
            var file2Source = file2.DataSource as PackedFileSource;
            Assert.That(file2Source, Is.Not.Null);
            Assert.That(file2Source.Offset, Is.EqualTo(2048));
            Assert.That(file2Source.Size, Is.EqualTo(4096));
            Assert.That(file2Source.IsCompressed, Is.True);
            Assert.That(file2Source.CompressionFormat, Is.EqualTo(CompressionFormat.Lz4));
            Assert.That(file2Source.UncompressedSize, Is.EqualTo(8192));
        }

        [Test]
        public void RestoreFromCache_SharesPackedFileSourceParents()
        {
            var cacheData = new CachedContainerData
            {
                Fingerprint = "fp",
                ContainerName = "Test",
                SystemFilePath = @"c:\game",
                Files =
                [
                    new CachedFileEntry("a.txt", "a.txt", @"c:\pack.pack", 0, 10, false, false, CompressionFormat.None, 0),
                    new CachedFileEntry("b.txt", "b.txt", @"c:\pack.pack", 10, 20, false, false, CompressionFormat.None, 0),
                ]
            };

            var container = PackFileContainerCacheHelper.RestoreFromCache(cacheData);

            var sourceA = (PackedFileSource)container.FileList["a.txt"].DataSource;
            var sourceB = (PackedFileSource)container.FileList["b.txt"].DataSource;
            Assert.That(ReferenceEquals(sourceA.Parent, sourceB.Parent), Is.True,
                "Files from the same pack should share PackedFileSourceParent instances");
        }

        [Test]
        public void LoadCache_ReturnsNullForMissingFile()
        {
            var result = PackFileContainerCacheHelper.LoadCache(Path.Combine(_tempDir, "nonexistent.bin"));
            Assert.That(result, Is.Null);
        }

        [Test]
        public void ComputeFingerprint_DeterministicForSameInputs()
        {
            // Create fake pack files
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

            // Modify the file (change size)
            File.WriteAllText(Path.Combine(packDir, "a.pack"), "data_a_modified_longer");

            var fp2 = PackFileContainerCacheHelper.ComputeFingerprint(packDir, packFiles);

            Assert.That(fp1, Is.Not.EqualTo(fp2));
        }

        [Test]
        public void RoundTrip_FullCycle_BuildSaveLoadRestore()
        {
            // Arrange
            var container = new PackFileContainer("Full Cycle Test")
            {
                IsCaPackFile = true,
                SystemFilePath = @"c:\game\data"
            };

            var parent = new PackedFileSourceParent { FilePath = @"c:\game\data\main.pack" };
            container.SourcePackFilePaths.Add(parent.FilePath);

            container.FileList["db\\units.bin"] = new PackFile("units.bin",
                new PackedFileSource(parent, 0, 256, false, false, CompressionFormat.None, 0));
            container.FileList["text\\localisation.loc"] = new PackFile("localisation.loc",
                new PackedFileSource(parent, 256, 512, false, true, CompressionFormat.Lz4, 1024));

            // Act: build → save → load → restore
            var cacheData = PackFileContainerCacheHelper.BuildCacheData("test_fp", container);
            PackFileContainerCacheHelper.SaveCache(cacheData, _cacheFilePath);
            var loadedData = PackFileContainerCacheHelper.LoadCache(_cacheFilePath);
            var restored = PackFileContainerCacheHelper.RestoreFromCache(loadedData!);

            // Assert: restored container matches original
            Assert.That(restored.Name, Is.EqualTo("Full Cycle Test"));
            Assert.That(restored.IsCaPackFile, Is.True);
            Assert.That(restored.SystemFilePath, Is.EqualTo(@"c:\game\data"));
            Assert.That(restored.FileList.Count, Is.EqualTo(2));

            Assert.That(restored.FindFile("db\\units.bin"), Is.Not.Null);
            Assert.That(restored.FindFile("text\\localisation.loc"), Is.Not.Null);

            var restoredSource = (PackedFileSource)restored.FileList["text\\localisation.loc"].DataSource;
            Assert.That(restoredSource.Offset, Is.EqualTo(256));
            Assert.That(restoredSource.Size, Is.EqualTo(512));
            Assert.That(restoredSource.IsCompressed, Is.True);
            Assert.That(restoredSource.CompressionFormat, Is.EqualTo(CompressionFormat.Lz4));
            Assert.That(restoredSource.UncompressedSize, Is.EqualTo(1024));
        }

        [Test]
        public void LoadCache_ReturnsNullForBadMagic()
        {
            File.WriteAllBytes(_cacheFilePath, [0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00]);
            var result = PackFileContainerCacheHelper.LoadCache(_cacheFilePath);
            Assert.That(result, Is.Null);
        }

        [Test]
        public void LoadCache_ReturnsNullForWrongVersion()
        {
            using (var stream = File.Create(_cacheFilePath))
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write("AEPC"u8);
                writer.Write(999); // wrong version
            }

            var result = PackFileContainerCacheHelper.LoadCache(_cacheFilePath);
            Assert.That(result, Is.Null);
        }
    }
}
