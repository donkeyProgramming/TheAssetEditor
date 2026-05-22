using System.Diagnostics;
using Moq;
using Shared.Core.Services;
using Shared.Ui.BaseDialogs.PackFileTree.ContextMenu.Commands;

namespace Shared.UiTest.BaseDialogs.PackFileTree.ContextMenu.Commands
{
    [TestFixture]
    internal class OpenPackInFileExplorerCommandTests : ContextMenuCommandTestBase
    {
        [Test]
        public void ShouldAdd_ReturnsTrueForRoot()
        {
            AddPackFiles(false, "modfile", "c:\\mymod.pack", ["rootfolder\\file.txt"]);
            var viewModel = PackFileBrowser();
            var root = viewModel.Files.First();

            var command = new OpenPackInFileExplorerCommand(_packFileService, new Mock<IStandardDialogs>().Object, new Mock<IFileSystemAccess>().Object);

            Assert.That(command.ShouldAdd(root), Is.True);
        }

        [Test]
        public void IsEnabled_ReturnsTrue()
        {
            AddPackFiles(false, "modfile", "c:\\mymod.pack", ["rootfolder\\file.txt"]);
            var viewModel = PackFileBrowser();
            var root = viewModel.Files.First();

            var command = new OpenPackInFileExplorerCommand(_packFileService, new Mock<IStandardDialogs>().Object, new Mock<IFileSystemAccess>().Object);

            Assert.That(command.IsEnabled(root), Is.True);
        }

        [Test]
        public void Execute_ValidPath_StartsExplorer()
        {
            // Arrange
            AddPackFiles(false, "modfile", "c:\\temp\\pack.pack", ["rootfolder\\file.txt"]);
            var viewModel = PackFileBrowser();
            var root = viewModel.Files.First();

            var fileSystem = new Mock<IFileSystemAccess>();
            fileSystem.Setup(x => x.DirectoryExists(It.IsAny<string>())).Returns(false);
            fileSystem.Setup(x => x.PathGetDirectoryName(It.IsAny<string>())).Returns("c:\\temp");

            // Act
            var command = new OpenPackInFileExplorerCommand(_packFileService, new Mock<IStandardDialogs>().Object, fileSystem.Object);
            command.Configure(root);

            command.Execute();

            // Assert
            fileSystem.Verify(x => x.ProcessStart(It.Is<ProcessStartInfo>(p => p.FileName == "explorer.exe")), Times.Once);
        }

        [Test]
        public void Execute_NullSystemFilePath_ShowsError()
        {
            // Arrange
            AddPackFiles(false, "modfile", "", ["rootfolder\\file.txt"]);
            var viewModel = PackFileBrowser();
            var root = viewModel.Files.First();

            var dialogs = new Mock<IStandardDialogs>();

            // Act
            var command = new OpenPackInFileExplorerCommand(_packFileService, dialogs.Object, new Mock<IFileSystemAccess>().Object);
            command.Configure(root);

            command.Execute();

            // Assert
            dialogs.Verify(x => x.ShowDialogBox(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }
    }
}
