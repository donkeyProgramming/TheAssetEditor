using System.Text;
using Moq;
using Shared.Core.Events;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;

namespace Test.Shared.Core.PackFiles
{
    internal class PackFileCompressionTests
    {
        private IPackFileService _packFileService;
        private PackFileContainer _container;

        [SetUp]
        public void Setup()
        {
            var eventHub = new Mock<IGlobalEventHub>();
            _packFileService = new PackFileService(eventHub.Object);
            _container = _packFileService.CreateNewPackFileContainer("EncryptedOutput", PackFileCAType.MOD, true);

            // Use files that aren't tiny so that they can actually be compressed rather than increase in size when being compressed
            List<NewPackFileEntry> files = [
                new("Directory_0", PackFile.CreateFromASCII("file0.txt", new string('A', 1_024))),
                new("Directory_0", PackFile.CreateFromASCII("file1.txt", new string('B', 2_048))),
                new("Directory_0\\subfolder", PackFile.CreateFromASCII("subfile0.txt", new string('C', 4_096))),
                new("Directory_0\\subfolder", PackFile.CreateFromASCII("subfile1.txt", new string('D', 8_192)))
            ];

            _packFileService.AddFilesToPack(_container, files);
        }

        [Test]
        public void TestCompressAndDecompressPackFile()
        {
            var compressionFormats = Enum.GetValues(typeof(CompressionFormat)).Cast<CompressionFormat>();
            var originals = _container.FileList
                .ToDictionary(file => file.Value.Name,
                              file => file.Value.DataSource.ReadData());

            foreach (var fileName in originals.Keys)
            {
                var data = originals[fileName];

                foreach (var compressionFormat in compressionFormats)
                {
                    var compressedData = PackFileCompression.Compress(data, compressionFormat);

                    if (compressionFormat != CompressionFormat.None)
                    {
                        Assert.That(compressedData, Has.Length.LessThan(data.Length),
                            $"[{compressionFormat}] {fileName} did not reduce in size: {data.Length} --> {compressedData.Length}");
                    }

                    var decompressed = PackFileCompression.Decompress(compressedData);
                    Assert.That(decompressed, Has.Length.EqualTo(data.Length),
                        $"[{compressionFormat}] {fileName} length mismatch");

                    var expected = Encoding.UTF8.GetString(originals[fileName]);
                    var actual = Encoding.UTF8.GetString(decompressed);
                    Assert.That(actual, Is.EqualTo(expected),
                        $"[{compressionFormat}] {fileName} content mismatch after round-trip");

                    // Feed back in for the next iteration
                    data = decompressed;
                }
            }
        }
    }
}
