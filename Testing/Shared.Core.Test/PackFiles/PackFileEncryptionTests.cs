using System.Text;
using Moq;
using Shared.Core.Events;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;

namespace Test.Shared.Core.PackFiles
{
    internal class PackFileEncryptionTests
    {
        private IPackFileService _packFileService;
        private PackFileContainer _container;

        [SetUp]
        public void Setup()
        {
            var eventHub = new Mock<IGlobalEventHub>();
            _packFileService = new PackFileService(eventHub.Object);
            _container = _packFileService.CreateNewPackFileContainer("EncryptedOutput", PackFileCAType.MOD, true);

            // Add files to the container
            List<NewPackFileEntry> files = [
                new("Directory_0", PackFile.CreateFromASCII("file0.txt", "Hello World!")),
                new("Directory_0", PackFile.CreateFromASCII("file1.txt", "Test File 1")),
                new("Directory_0\\subfolder", PackFile.CreateFromASCII("subfile0.txt", "Subfolder Data")),
                new("Directory_0\\subfolder", PackFile.CreateFromASCII("subfile1.txt", "Another File"))
            ];

            _packFileService.AddFilesToPack(_container, files);
        }

        [Test]
        public void TestEncryptAndDecryptPackFile()
        {
            // Encrypt each file in the pack
            foreach (var file in _container.FileList)
            {
                var originalData = file.Value.DataSource.ReadData();
                var encryptedData = PackFileEncryption.Encrypt(originalData);
                file.Value.DataSource = new MemorySource(encryptedData);
            }

            // Assert the file count is what we expect
            Assert.That(_container.FileList.Count, Is.EqualTo(4), "Unexpected number of files in the container.");

            // Verify encryption and decryption
            foreach (var file in _container.FileList)
            {
                var encryptedData = file.Value.DataSource.ReadData();
                var decryptedContent = PackFileEncryption.Decrypt(encryptedData);
                var originalContent = Encoding.UTF8.GetString(decryptedContent);

                switch (file.Value.Name)
                {
                    case "file0.txt":
                        Assert.That(originalContent, Is.EqualTo("Hello World!"));
                        break;
                    case "file1.txt":
                        Assert.That(originalContent, Is.EqualTo("Test File 1"));
                        break;
                    case "subfile0.txt":
                        Assert.That(originalContent, Is.EqualTo("Subfolder Data"));
                        break;
                    case "subfile1.txt":
                        Assert.That(originalContent, Is.EqualTo("Another File"));
                        break;
                    default:
                        Assert.Fail($"Unexpected file {file.Value.Name}");
                        break;
                }
            }
        }
    }
}
