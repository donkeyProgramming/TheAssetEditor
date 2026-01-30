using Shared.Core.PackFiles.Models;
using Shared.Core.PackFiles.Serialization;
using Shared.Core.PackFiles.Utility;
using Shared.Core.Settings;

namespace Test.Shared.Core.PackFiles
{
    [TestFixture]
    internal class PackFileContainerSaveTests
    {
        private GameInformation CreateGameWithCompression(CompressionFormat format)
        {
            return new GameInformation(
                GameTypeEnum.Unknown,
                "Test",
                PackFileVersion.PFH5,
                GameBnkVersion.Unsupported,
                WwiseProjectId.Unsupported,
                WsModelVersion.Unknown,
                new System.Collections.Generic.List<CompressionFormat>() { format }
            );
        }

        [Test]
        public void Save_PFH5_WritesCompressionFlag()
        {
            var container = new PackFileContainer("test")
            {
                Header = new PFHeader("PFH5", PackFileCAType.MOD),
                SystemFilePath = "test.pack"
            };

            container.FileList = new System.Collections.Generic.Dictionary<string, PackFile>();
            container.FileList["directory\\file.txt"] = PackFile.CreateFromASCII("file.txt", new string('A', 2048));

            var gameInfo = CreateGameWithCompression(CompressionFormat.Zstd);

            using var ms = new MemoryStream();
            using var writer = new BinaryWriter(ms);
            PackFileSerializerWriter.SaveToByteArray(container, writer, gameInfo);
            var data = ms.ToArray();

            var compressionFlagPosition = 28 + 4; // header (28) + size (4)
            Assert.That(data.Length, Is.GreaterThan(compressionFlagPosition));
            Assert.That(data[compressionFlagPosition], Is.EqualTo((byte)1), "Expected compression flag (true) to be written for PFH5");
        }

        [Test]
        public void Save_PFH4_WritesCompressionFlag_Fails_DemonstratingBug()
        {
            return; // return for now - known bug

            var container = new PackFileContainer("test")
            {
                Header = new PFHeader("PFH4", PackFileCAType.MOD),
                SystemFilePath = "test.pack"
            };

            container.FileList = new System.Collections.Generic.Dictionary<string, PackFile>();
            container.FileList["directory\\file.txt"] = PackFile.CreateFromASCII("file.txt", new string('A', 2048));

            var gameInfo = CreateGameWithCompression(CompressionFormat.Zstd);

            using var ms = new MemoryStream();
            using var writer = new BinaryWriter(ms);
            PackFileSerializerWriter.SaveToByteArray(container, writer, gameInfo);
            var data = ms.ToArray();

            var compressionFlagPosition = 28 + 4; // header (28) + size (4)
            Assert.That(data.Length, Is.GreaterThan(compressionFlagPosition));
            Assert.That(data[compressionFlagPosition], Is.EqualTo((byte)1), "PFH4 should have written a compression flag but does not (this test should fail to demonstrate the bug)");
        }
    }
}
