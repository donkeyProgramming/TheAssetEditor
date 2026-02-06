using Shared.Core.PackFiles.Models;
using Shared.Core.PackFiles.Serialization;
using Shared.Core.PackFiles.Utility;
using Shared.Core.Settings;

namespace Test.Shared.Core.PackFiles.Serialization
{
    [TestFixture]
    internal class PackFileSerializerWriterTests
    {
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
            var packFileHeader = PackFileVersionConverter.ToString(outputPackFileVersion);
            var container = new PackFileContainer("test")
            {
                Header = new PFHeader(packFileHeader, PackFileCAType.MOD),
                SystemFilePath = "test.pack"
            };

            foreach (var fileInfo in expectedFileInfo)
                container.FileList[fileInfo.FilePath] = PackFile.CreateFromASCII(fileInfo.FileName, new string(fileInfo.Content, fileInfo.Length));

            using var writeMs = new MemoryStream();
            using var writer = new BinaryWriter(writeMs);
            PackFileSerializerWriter.SaveToByteArray(outputContainerName, container, writer, gameInfo);
            var data = writeMs.ToArray();

            // Asser that the internal file references have been updated
            foreach (var fileInfo in expectedFileInfo)
            {
                var dataSourceInstance = container.FileList[fileInfo.FilePath].DataSource as PackedFileSource;
                Assert.That(dataSourceInstance, Is.Not.Null);
                Assert.That(dataSourceInstance.Parent.FilePath, Is.EqualTo(outputContainerName));
            }

            //  Load the file and assert
            using var readBackMs = new MemoryStream(data);
            var reader = new BinaryReader(readBackMs);
            var loadedPackFile = PackFileSerializerLoader.Load(outputContainerName, data.LongLength, reader, new CaPackDuplicateFileResolver());

            for (var i = 0; i < expectedFileInfo.Count; i++)
            {
                var expectedFileInfoInstance = expectedFileInfo[i];
                var packFile = loadedPackFile.FileList[expectedFileInfoInstance.FilePath.ToLower()];

                // Bypass the filesystem lookup and go directly to stream
                var packFileConentet = (packFile.DataSource as PackedFileSource).ReadData(readBackMs);
                var parentName = (packFile.DataSource as PackedFileSource).Parent.FilePath;

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
    }
}
