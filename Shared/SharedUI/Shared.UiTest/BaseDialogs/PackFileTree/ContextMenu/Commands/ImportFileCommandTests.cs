using Test.TestingUtility.TestUtility;
using Moq;
using Shared.Core.Services;
using Shared.Ui.BaseDialogs.PackFileTree;
using Shared.Ui.BaseDialogs.PackFileTree.ContextMenu.Commands;
using Shared.Ui.BaseDialogs.PackFileTree.Utility;

namespace Shared.UiTest.BaseDialogs.PackFileTree.ContextMenu.Commands
{
    [TestFixture]
    internal class ImportFileCommandTests : ContextMenuCommandTestBase
    {
        [Test]
        public void ShouldAdd_ReturnsTrueForDirectoryNode()
        {
            var container = AddPackFiles(false, "modfile", "c:\\mymod.pack", ["dir\\file.txt"]);
            var viewModel = PackFileBrowser();
            var root = viewModel.Files.First();
            var dirNode = root.Children.First(x => x.NodeType == NodeType.Directory);

            var command = new ImportFileCommand(_packFileService, new Mock<IStandardDialogs>().Object, new Mock<IFileSystemAccess>().Object, MockScopedLogger.Create());

            Assert.That(command.ShouldAdd(dirNode), Is.True);
        }

        [Test]
        public void IsEnabled_ReturnsTrue()
        {
            AddPackFiles(true, "gamefile", "root", ["rootfolder\\file.txt"]);
            var viewModel = PackFileBrowser();
            var root = viewModel.Files.First();

            var command = new ImportFileCommand(_packFileService, new Mock<IStandardDialogs>().Object, new Mock<IFileSystemAccess>().Object, MockScopedLogger.Create());

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
            var fileSystem = new Mock<IFileSystemAccess>();

            // Act
            var command = new ImportFileCommand(_packFileService, dialogs.Object, fileSystem.Object, MockScopedLogger.Create());
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
            dialogs.Setup(x => x.ShowSystemOpenFileDialog(It.IsAny<bool>(), It.IsAny<string>()))
                .Returns(new SystemOpenFileDialogResult(Result: false, FilePaths: []));
            var fileSystem = new Mock<IFileSystemAccess>();

            // Act
            var command = new ImportFileCommand(_packFileService, dialogs.Object, fileSystem.Object, MockScopedLogger.Create());
            command.Configure(root);

            command.Execute();

            // Assert
            fileSystem.Verify(x => x.FileReadAllBytes(It.IsAny<string>()), Times.Never);
        }

        [Test]
        public void Execute_FileSelected_ImportsFile()
        {
            // Arrange
            var container = AddPackFiles(false, "modfile", "c:\\mymod.pack", ["rootfolder\\existing.txt"]);
            var viewModel = PackFileBrowser();
            var root = viewModel.Files.First();

            var fileBytes = new byte[] { 0x01, 0x02, 0x03 };
            var filePath = "C:\\test\\file.txt";

            var dialogs = new Mock<IStandardDialogs>();
            dialogs.Setup(x => x.ShowSystemOpenFileDialog(It.IsAny<bool>(), It.IsAny<string>()))
                .Returns(new SystemOpenFileDialogResult(Result: true, FilePaths: [filePath]));
            var fileSystem = new Mock<IFileSystemAccess>();
            fileSystem.Setup(x => x.FileReadAllBytes(filePath)).Returns(fileBytes);

            // Act
            var command = new ImportFileCommand(_packFileService, dialogs.Object, fileSystem.Object, MockScopedLogger.Create());
            command.Configure(root);

            command.Execute();

            // Assert
            fileSystem.Verify(x => x.FileReadAllBytes(filePath), Times.Once);
            var importedFile = container.FindFile("file.txt");
            Assert.That(importedFile, Is.Not.Null);
        }
    }
}
