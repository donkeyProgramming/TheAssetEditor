using Shared.Core.PackFiles.Models;
using Shared.Core.PackFiles.Models.Containers;
using Shared.Core.PackFiles.Models.FileSources;
using Shared.Core.PackFiles.Serialization;
using Shared.Core.PackFiles.Utility;
using Shared.Core.Settings;

namespace Shared.CoreTest.PackFiles.Serialization
{
    [TestFixture]
    internal class PackFileSerializerWriterTests
    {
        private static readonly (string Path, string Content)[] CorruptionDetectionFiles =
        [
            (@"!!!packfile_corruction_detection\packfile_corruction_detection_1.txt", "This file is here to validate that the packfile has not been corrupted while saving. This is check 1"),
            (@"packfile_corruction_detection\packfile_corruction_detection_2.txt", "This file is here to validate that the packfile has not been corrupted while saving. This is check 2"),
            (@"zzzz_packfile_corruction_detection\packfile_corruction_detection_3.txt", "This file is here to validate that the packfile has not been corrupted while saving. This is check 3")
        ];

        [Test]
        public void SaveToByteArray_IgnoresFilesConfiguredInContainerSettings()
        {
            var gameInfo = GameInformationDatabase.GetGameById(GameTypeEnum.Warhammer3);
            var outputContainerName = @"c:\fullpath\to\ignored-test.pack";
            var container = PackFileContainer.CreatePackFile("test", "test.pack", PackFileVersion.PFH5);

            container.AddOrUpdateFile("folder\\keep.txt", PackFile.CreateFromASCII("keep.txt", "KEEP"));
            container.AddOrUpdateFile("folder\\ignore.txt", PackFile.CreateFromASCII("ignore.txt", "IGNORE"));
            container.PackFileSettings.IgnoredFilesWhenSerializing.Add("Folder/IGNORE.txt");

            using var writeMs = new MemoryStream();
            using var writer = new BinaryWriter(writeMs);
            PackFileSerializerWriter.SaveToByteArray(outputContainerName, container, writer, gameInfo);

            using var readMs = new MemoryStream(writeMs.ToArray());
            using var reader = new BinaryReader(readMs);
            var loadedPack = PackFileSerializerLoader.Load(outputContainerName, readMs.Length, reader, new CaPackDuplicateFileResolver());

            Assert.That(loadedPack.GetFileCount(), Is.EqualTo(1));
            Assert.That(loadedPack.FindFile("folder\\keep.txt"), Is.Not.Null);
            Assert.That(loadedPack.FindFile("folder\\ignore.txt"), Is.Null);
            Assert.That(container.FindFile("folder\\ignore.txt"), Is.Not.Null);
        }

        [Test]
        public void SaveToByteArray_CorruptionDetectionEnabled_AddsDetectionFiles()
        {
            var gameInfo = GameInformationDatabase.GetGameById(GameTypeEnum.Warhammer3);
            var outputContainerName = @"c:\fullpath\to\corruption-detection-test.pack";
            var container = PackFileContainer.CreatePackFile("test", "test.pack", PackFileVersion.PFH5);
            container.PackFileSettings.EnablePackFileCorruptionDetection = true;
            container.AddOrUpdateFile("folder\\keep.txt", PackFile.CreateFromASCII("keep.txt", "KEEP"));

            Assert.That(container.PackFileSettings.EnablePackFileCorruptionDetection, Is.True);

            using var writeMs = new MemoryStream();
            using var writer = new BinaryWriter(writeMs);
            PackFileSerializerWriter.SaveToByteArray(outputContainerName, container, writer, gameInfo);

            var loadedPack = LoadFromMemory(outputContainerName, writeMs.ToArray());

            Assert.That(loadedPack.FindFile("folder\\keep.txt"), Is.Not.Null);
            AssertCorruptionDetectionFiles(loadedPack, writeMs);
        }

        [Test]
        public void SaveToByteArray_CorruptionDetectionEnabled_ReplacesExistingDetectionFiles()
        {
            var gameInfo = GameInformationDatabase.GetGameById(GameTypeEnum.Warhammer3);
            var outputContainerName = @"c:\fullpath\to\corruption-detection-replace-test.pack";
            var container = PackFileContainer.CreatePackFile("test", "test.pack", PackFileVersion.PFH5);
            container.PackFileSettings.EnablePackFileCorruptionDetection = true;
            container.AddOrUpdateFile(CorruptionDetectionFiles[0].Path, PackFile.CreateFromASCII(Path.GetFileName(CorruptionDetectionFiles[0].Path), "stale"));

            Assert.That(container.PackFileSettings.EnablePackFileCorruptionDetection, Is.True);

            using var writeMs = new MemoryStream();
            using var writer = new BinaryWriter(writeMs);
            PackFileSerializerWriter.SaveToByteArray(outputContainerName, container, writer, gameInfo);

            var loadedPack = LoadFromMemory(outputContainerName, writeMs.ToArray());

            AssertCorruptionDetectionFiles(loadedPack, writeMs);
        }

        [TestCase(GameTypeEnum.Warhammer3, PackFileVersion.PFH4, "folder//filex.txt", CompressionFormat.Zstd, CompressionFormat.None, true)]
        [TestCase(GameTypeEnum.Warhammer3, PackFileVersion.PFH5, "folder//filex.txt", CompressionFormat.Zstd, CompressionFormat.Zstd, false)]
        [TestCase(GameTypeEnum.Warhammer3, PackFileVersion.PFH5, "folder//filex.txt", CompressionFormat.Lzma1, CompressionFormat.Zstd, true)]
        [TestCase(GameTypeEnum.Warhammer3, PackFileVersion.PFH4, "folder//filex", CompressionFormat.None, CompressionFormat.None, false)]
        [TestCase(GameTypeEnum.Warhammer3, PackFileVersion.PFH4, "folder//filex", CompressionFormat.Lz4, CompressionFormat.None, true)]
        [TestCase(GameTypeEnum.Rome2, PackFileVersion.PFH4, "folder//filex.txt", CompressionFormat.Lz4, CompressionFormat.None, true)]
        [TestCase(GameTypeEnum.Rome2, PackFileVersion.PFH4, "folder//filex.txt", CompressionFormat.None, CompressionFormat.None, false)]
        // Rome 2 cases
        public void DetermineFileCompression(
            GameTypeEnum game,
            PackFileVersion outputPackFileVersion, 
            string fileName, 
            CompressionFormat currentFileCompression, 
            CompressionFormat expected_Compression, 
            bool expected_deserializeBeforeWrite)
        {
            var gameInfo = GameInformationDatabase.GetGameById(game);

            var res = PackFileSerializerWriter.DetermineFileCompression(outputPackFileVersion, gameInfo, fileName, currentFileCompression);
            Assert.That(res.DecompressBeforeSaving, Is.EqualTo(expected_deserializeBeforeWrite));
            Assert.That(res.IntendedCompressionFormat, Is.EqualTo(expected_Compression));
        }

        [Test]
        [TestCase(GameTypeEnum.Rome2, PackFileVersion.PFH4, false)]
        [TestCase(GameTypeEnum.Warhammer3, PackFileVersion.PFH4, false)]
        [TestCase(GameTypeEnum.Warhammer3, PackFileVersion.PFH5, true)]
        public void PackFileSerializerWriterTests_GameWithoutCompression(
            GameTypeEnum game,
            PackFileVersion outputPackFileVersion,
            bool expectFileCompression)
        {
            // Arrange
            var gameInfo = GameInformationDatabase.GetGameById(game);
            var expectedFileInfo = new List<(string FilePath, string FileName, int Length, char Content, bool IsCompressable)>
            {
                ("directory\\fileA.txt", "fileA.txt", 512, 'A', true),
                ("directory\\fileB.txt", "fileB.txt", 1024, 'B', true),
                ("directory\\fileC.txt", "fileC.txt", 2048, 'C', true),
                ("directory\\fileD", "fileD", 512, 'D', false),
                ("\"directory\\\\db\\\\TableTest\"", "TableTest", 128, 'E', false),
            };

            // Create packfile with the above files
            var outputContainerName = @"c:\fullpath\to\packfile.pack";
            var container = PackFileContainer.CreatePackFile("test", "test.pack", outputPackFileVersion);

            foreach (var fileInfo in expectedFileInfo)
                container.AddOrUpdateFile(fileInfo.FilePath, PackFile.CreateFromASCII(fileInfo.FileName, new string(fileInfo.Content, fileInfo.Length)));

            using var writeMs = new MemoryStream();
            using var writer = new BinaryWriter(writeMs);
            var sourceUpdates = PackFileSerializerWriter.SaveToByteArray(outputContainerName, container, writer, gameInfo);
            var data = writeMs.ToArray();

            // The writer hands back the new sources for the caller to apply once the file is in place,
            // and leaves the container's files reading from where they were
            foreach (var fileInfo in expectedFileInfo)
            {
                var containerFile = container.FindFile(fileInfo.FilePath);
                Assert.That(containerFile, Is.Not.Null);
                Assert.That(containerFile!.DataSource, Is.InstanceOf<MemorySource>());

                var update = sourceUpdates.SingleOrDefault(x => ReferenceEquals(x.PackFile, containerFile));
                Assert.That(update, Is.Not.Null);
                Assert.That(update!.NewSource.Parent.FilePath, Is.EqualTo(outputContainerName));
            }

            //  Load the file and assert
            using var readBackMs = new MemoryStream(data);
            var reader = new BinaryReader(readBackMs);
            var loadedPackFile = PackFileSerializerLoader.Load(outputContainerName, data.LongLength, reader, new CaPackDuplicateFileResolver());

            for (var i = 0; i < expectedFileInfo.Count; i++)
            {
                var expectedFileInfoInstance = expectedFileInfo[i];
                var packFile = loadedPackFile.FindFile(expectedFileInfoInstance.FilePath.ToLower());
                Assert.That(packFile, Is.Not.Null);

                // Bypass the filesystem lookup and go directly to stream
                var packedSource = packFile!.DataSource as PackedFileSource;
                Assert.That(packedSource, Is.Not.Null);
                var packFileConentet = packedSource!.ReadData(readBackMs);
                var parentName = packedSource.Parent.FilePath;

                // Assert that parent file has been updated correctly
                Assert.That(parentName.ToLower(), Is.EqualTo(outputContainerName.ToLower()));

                // Assert content is correct
                Assert.That(packFileConentet.Length, Is.EqualTo(expectedFileInfoInstance.Length));
                Assert.That(packFileConentet, Is.EqualTo(new string(expectedFileInfoInstance.Content, expectedFileInfoInstance.Length)));

                if (expectedFileInfoInstance.IsCompressable && expectFileCompression)
                {
                    Assert.That(packFile.DataSource.CompressionFormat, Is.Not.EqualTo(CompressionFormat.None));
                    Assert.That(packFile.DataSource.Size, Is.LessThan(expectedFileInfoInstance.Length));
                }
                else
                {
                    Assert.That(packFile.DataSource.CompressionFormat, Is.EqualTo(CompressionFormat.None));
                    Assert.That(packFile.DataSource.Size, Is.EqualTo(expectedFileInfoInstance.Length));
                }
            }

        }

        private static PackFileContainer LoadFromMemory(string outputContainerName, byte[] data)
        {
            using var readMs = new MemoryStream(data);
            using var reader = new BinaryReader(readMs);
            return PackFileSerializerLoader.Load(outputContainerName, data.LongLength, reader, new CaPackDuplicateFileResolver());
        }

        private static void AssertCorruptionDetectionFiles(PackFileContainer loadedPack, Stream? sourceStream = null)
        {
            foreach (var detectionFile in CorruptionDetectionFiles)
            {
                var packFile = loadedPack.FindFile(detectionFile.Path);
                Assert.That(packFile, Is.Not.Null, $"Expected corruption detection file '{detectionFile.Path}'. Loaded files: {string.Join(", ", loadedPack.GetAllFiles().Keys)}");
                var data = sourceStream != null && packFile!.DataSource is PackedFileSource packedFileSource
                    ? packedFileSource.ReadData(sourceStream)
                    : packFile!.DataSource.ReadData();
                Assert.That(System.Text.Encoding.UTF8.GetString(data), Is.EqualTo(detectionFile.Content));
            }
        }
    }
}
