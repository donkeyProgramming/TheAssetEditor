using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;

namespace Test.Shared.Core.PackFiles
{
    internal class PackFileService_DeleteFolder
    {
        [Test]
        public void DeleteFolder()
        {
            // Arrange
            var pfs = new PackFileService(null);
            pfs.AddContainer(new PackFileContainer("CaPackFile") { IsCaPackFile = true });
            var container = CreateTestPack(pfs);

            // Act
            pfs.DeleteFolder(container, "Directory_0");

            // Assert
            Assert.That(container.FileList.Count, Is.EqualTo(2));
        }

        [Test]
        public void DeleteMissingFolder() // THere might be nodes in the tree view that have no files in them, that will trigger this
        {
            // Arrange
            var pfs = new PackFileService(null);
            pfs.AddContainer(new PackFileContainer("CaPackFile") { IsCaPackFile = true });
            var container = CreateTestPack(pfs);

            // Act
            pfs.DeleteFolder(container, "folderWithoutFiles");

            // Assert
            Assert.That(container.FileList.Count, Is.EqualTo(8));
        }

        [Test]
        public void DeleteFolderWithSubFolder()
        {
            // Arrange
            var pfs = new PackFileService(null);
            pfs.AddContainer(new PackFileContainer("CaPackFile") { IsCaPackFile = true });
            var container = CreateTestPack(pfs);

            // Act
            pfs.DeleteFolder(container, "Directory_0\\subfolder");

            // Assert
            Assert.That(container.FileList.Count, Is.EqualTo(4));
        }


        static PackFileContainer CreateTestPack(IPackFileService pfs)
        {
            var container = pfs.AddContainer(new PackFileContainer("Custom") { SystemFilePath = "SystemPath" }, true)!;
            var newFiles = new List<NewPackFileEntry>
            {
                new("Directory_0", new PackFile("file0.txt", null)),
                new("Directory_0", new PackFile("file1.txt", null)),
                new("Directory_0\\subfolder", new PackFile("subfile0.txt", null)),
                new("Directory_0\\subfolder", new PackFile("subfile1.txt", null)),

                new("Directory_0\\subfolder\\child", new PackFile("childFile0.txt", null)),
                new("Directory_0\\subfolder\\child", new PackFile("childFile1.txt", null)),

                new("Directory_1", new PackFile("file0.txt", null)),
                new("", new PackFile("rootFile.txt", null))
            };

            pfs.AddFilesToPack(container, newFiles);
            Assert.That(container.FileList.Count, Is.EqualTo(8));
            return container;
        }
    }
}
