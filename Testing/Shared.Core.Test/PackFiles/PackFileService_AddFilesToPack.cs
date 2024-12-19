using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;

namespace Test.Shared.Core.PackFiles
{
    internal class PackFileService_AddFilesToPack
    {
        [Test]
        public void AddFilesToPack_MultipleFiles()
        {
            // Arrange
            var pfs = new PackFileService(null);
            pfs.AddContainer(new PackFileContainer("CaPackFile") { IsCaPackFile = true });

            var container = pfs.AddContainer(new PackFileContainer("Custom") { SystemFilePath = "SystemPath" }, true)!;

            var newFiles = new List<NewPackFileEntry>
            {
                new("Directory_0", new PackFile("file0.txt", null)),
                new("Directory_0", new PackFile("file1.txt", null)),

                new("Directory_1", new PackFile("file0.txt", null)),
                new("", new PackFile("rootFile.txt", null))
            };

            // Act
            pfs.AddFilesToPack(container, newFiles);

            // Assert
            Assert.That(container.FileList.Count, Is.EqualTo(4));
        }

        [Test]
        public void AddFilesToPack_AddToRoot()
        {
            // Arrange
            var pfs = new PackFileService(null);
            pfs.AddContainer(new PackFileContainer("CaPackFile") { IsCaPackFile = true });

            var container = pfs.AddContainer(new PackFileContainer("Custom") { SystemFilePath = "SystemPath" }, true)!;

            var newFiles = new List<NewPackFileEntry>
            {
                new("", new PackFile("rootFile.txt", null))
            };

            // Act
            pfs.AddFilesToPack(container, newFiles);

            // Assert
            Assert.That(container.FileList.Count, Is.EqualTo(1));
        }

        [Test]
        public void AddFilesToPack_AddToFolder()
        {
            // Arrange
            var pfs = new PackFileService(null);
            pfs.AddContainer(new PackFileContainer("CaPackFile") { IsCaPackFile = true });

            var container = pfs.AddContainer(new PackFileContainer("Custom") { SystemFilePath = "SystemPath"}, true)!;

            var newFiles = new List<NewPackFileEntry>
            {
                new("Directory_0", new PackFile("file0.txt", null)),
                new("Directory_0", new PackFile("file1.txt", null)),

                new("Directory_1", new PackFile("file0.txt", null)),
            };

            // Act
            pfs.AddFilesToPack(container, newFiles);

            // Assert
            Assert.That(container.FileList.Count, Is.EqualTo(3));
        }


        // Not sure if the behevior here is a bug or feature?
        [Test]
        public void AddFilesToPack_FileNameConflict_override()
        {
            // Arrange
            var pfs = new PackFileService(null);
            pfs.AddContainer(new PackFileContainer("CaPackFile") { IsCaPackFile = true });

            var container = pfs.AddContainer(new PackFileContainer("Custom") { SystemFilePath = "SystemPath" }, true)!;

            var newFiles = new List<NewPackFileEntry>
            {
                new("Directory_0", new PackFile("file0.txt", null)),
                new("Directory_0", new PackFile("file1.txt", null)),

                new("Directory_1", new PackFile("file0.txt", null)),
            };

            // Act
            pfs.AddFilesToPack(container, newFiles);
            pfs.AddFilesToPack(container, newFiles);

            // Assert
            Assert.That(container.FileList.Count, Is.EqualTo(3));
        }

        [Test]
        public void AddFilesToPack_WhiteSpaceInName()
        {
            // Arrange
            var pfs = new PackFileService(null);
            pfs.AddContainer(new PackFileContainer("CaPackFile") { IsCaPackFile = true });

            var container = pfs.AddContainer(new PackFileContainer("Custom") { SystemFilePath = "SystemPath" }, true)!;

            var newFiles = new List<NewPackFileEntry>
            {
                new("Directory_0  ", new PackFile("file0.txt   ", null)),
            };

            // Act
            pfs.AddFilesToPack(container, newFiles);

            // Assert
            Assert.That(container.FileList.Count, Is.EqualTo(1));
            Assert.That(container.FileList.First().Key.Any(char.IsWhiteSpace), Is.EqualTo(false));
            Assert.That(container.FileList.First().Value.Name.Any(char.IsWhiteSpace), Is.EqualTo(false));
        }

        [Test]
        public void AddFilesToPack_NoFileName()
        {
            // Arrange
            var pfs = new PackFileService(null);
            pfs.AddContainer(new PackFileContainer("CaPackFile") { IsCaPackFile = true });

            var container = pfs.AddContainer(new PackFileContainer("Custom") { SystemFilePath = "SystemPath" }, true)!;

            var newFiles = new List<NewPackFileEntry>
            {
                new("Directory_0", new PackFile("", null)),
            };

            // Act
            Assert.Throws<Exception>(() => pfs.AddFilesToPack(container, newFiles));
        }


        [Test]
        public void AddFilesToPack_CAPackFile()
        {
            // Arrange
            var pfs = new PackFileService(null);
            var caPack = pfs.AddContainer(new PackFileContainer("CaPackFile") { IsCaPackFile = true });

            var newFiles = new List<NewPackFileEntry>
            {
                new("Directory_0", new PackFile("file0", null)),
            };

            // Act
            Assert.Throws<Exception>(() => pfs.AddFilesToPack(caPack, newFiles));
        }
    }
}
