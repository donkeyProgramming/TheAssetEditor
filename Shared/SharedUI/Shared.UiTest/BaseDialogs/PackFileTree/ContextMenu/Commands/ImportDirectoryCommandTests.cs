using Test.TestingUtility.TestUtility;
using System.IO;
using Moq;
using Shared.Core.Services;
using Shared.Ui.BaseDialogs.PackFileTree;
using Shared.Ui.BaseDialogs.PackFileTree.ContextMenu.Commands;

namespace Shared.UiTest.BaseDialogs.PackFileTree.ContextMenu.Commands
{
    [TestFixture]
    internal class ImportDirectoryCommandTests : ContextMenuCommandTestBase
    {
        [Test]
        public void ShouldAdd_ReturnsTrueForDirectoryNode()
        {
            var container = AddPackFiles(false, "modfile", "c:\\mymod.pack", ["dir\\file.txt"]);
            var viewModel = PackFileBrowser();
            var root = viewModel.Files.First();
            var dirNode = root.Children.First(x => x.NodeType == NodeType.Directory);

            var command = new ImportDirectoryCommand(_packFileService, new Mock<IStandardDialogs>().Object, new Mock<IFileSystemAccess>().Object, MockScopedLogger.Create());

            Assert.That(command.ShouldAdd(dirNode), Is.True);
        }

        [Test]
        public void IsEnabled_ReturnsTrue()
        {
            AddPackFiles(true, "gamefile", "root", ["rootfolder\\file.txt"]);
            var viewModel = PackFileBrowser();
            var root = viewModel.Files.First();

            var command = new ImportDirectoryCommand(_packFileService, new Mock<IStandardDialogs>().Object, new Mock<IFileSystemAccess>().Object, MockScopedLogger.Create());

            Assert.That(command.IsEnabled(root), Is.True);
        }

        [Test]
        public void Execute_CaPackShowsErrorAndDoesNotImport()
        {
            // Arrange
            AddPackFiles(true, "gamefile", "root", ["rootfolder\\file.txt"]);
            var viewModel = PackFileBrowser();
            var root = viewModel.Files.First();

            var dialogs = new Mock<IStandardDialogs>();

            // Act
            var command = new ImportDirectoryCommand(_packFileService, dialogs.Object, new Mock<IFileSystemAccess>().Object, MockScopedLogger.Create());
            command.Configure(root);

            command.Execute();

            // Assert
            dialogs.Verify(x => x.ShowDialogBox("Unable to edit CA packfile", "Error"), Times.Once);
        }

        [Test]
        public void Execute_DialogCancelled_DoesNotImport()
        {
            // Arrange
            AddPackFiles(false, "modfile", "c:\\mymod.pack", ["rootfolder\\file.txt"]);
            var viewModel = PackFileBrowser();
            var root = viewModel.Files.First();

            var dialogs = new Mock<IStandardDialogs>();
            dialogs.Setup(x => x.ShowSystemFolderBrowserDialog())
                .Returns(new SystemBrowseFolderDialogResult(Result: false, FolderPath: string.Empty));
            var fileSystem = new Mock<IFileSystemAccess>();

            // Act
            var command = new ImportDirectoryCommand(_packFileService, dialogs.Object, fileSystem.Object, MockScopedLogger.Create());
            command.Configure(root);

            command.Execute();

            // Assert
            fileSystem.Verify(x => x.FileReadAllBytes(It.IsAny<string>()), Times.Never);
        }

        [Test]
        public void Execute_DirectorySelected_ImportsFiles()
        {
            // Arrange
            var container = AddPackFiles(false, "modfile", "c:\\mymod.pack", ["rootfolder\\existing.txt"]);
            var viewModel = PackFileBrowser();
            var root = viewModel.Files.First();

            var folderPath = "C:\\test\\folder";
            var file1Path = "C:\\test\\folder\\file1.txt";
            var file2Path = "C:\\test\\folder\\subdir\\file2.txt";
            var file1Bytes = new byte[] { 0x01, 0x02 };
            var file2Bytes = new byte[] { 0x03, 0x04, 0x05 };

            var dialogs = new Mock<IStandardDialogs>();
            dialogs.Setup(x => x.ShowSystemFolderBrowserDialog())
                .Returns(new SystemBrowseFolderDialogResult(Result: true, FolderPath: folderPath));
            var fileSystem = new Mock<IFileSystemAccess>();
            fileSystem.Setup(x => x.CreateDirectoryInfo(folderPath)).Returns(new DirectoryInfo(folderPath));
            fileSystem.Setup(x => x.DirectoryGetFiles(folderPath, "*", SearchOption.AllDirectories))
                .Returns([file1Path, file2Path]);
            fileSystem.Setup(x => x.PathGetFileName("file1.txt")).Returns("file1.txt");
            fileSystem.Setup(x => x.PathGetFileName("subdir\\file2.txt")).Returns("file2.txt");
            fileSystem.Setup(x => x.FileReadAllBytes(file1Path)).Returns(file1Bytes);
            fileSystem.Setup(x => x.FileReadAllBytes(file2Path)).Returns(file2Bytes);

            // Act
            var command = new ImportDirectoryCommand(_packFileService, dialogs.Object, fileSystem.Object, MockScopedLogger.Create());
            command.Configure(root);

            command.Execute();

            // Assert
            fileSystem.Verify(x => x.FileReadAllBytes(file1Path), Times.Once);
            fileSystem.Verify(x => x.FileReadAllBytes(file2Path), Times.Once);
            var importedFile1 = container.FindFile("folder\\file1.txt");
            var importedFile2 = container.FindFile("folder\\subdir\\file2.txt");
            Assert.That(importedFile1, Is.Not.Null);
            Assert.That(importedFile2, Is.Not.Null);
        }
    }
}
