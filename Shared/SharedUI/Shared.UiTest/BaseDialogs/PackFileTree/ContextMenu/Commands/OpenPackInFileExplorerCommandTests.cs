using System.Diagnostics;
using Moq;
using Shared.Core.PackFiles;
using Shared.Core.Services;
using Shared.Ui.BaseDialogs.PackFileTree;
using Shared.Ui.BaseDialogs.PackFileTree.ContextMenu.Commands;

namespace Shared.UiTest.BaseDialogs.PackFileTree.ContextMenu.Commands
{
    [TestFixture]
    internal class OpenPackInFileExplorerCommandTests : ContextMenuCommandTestBase
    {
        [Test]
        public void ShouldAdd_ReturnsTrueForRoot()
        {
            var owner = CreateContainer();
            var service = CreatePackFileService(owner);
            var root = CreateRoot(owner);
            var command = new OpenPackInFileExplorerCommand(service.Object, new Mock<IStandardDialogs>().Object, new Mock<IFileSystemAccess>().Object);

            Assert.That(command.ShouldAdd(root), Is.True);
        }

        [Test]
        public void IsEnabled_ReturnsTrue()
        {
            var owner = CreateContainer();
            var root = CreateRoot(owner);
            var command = new OpenPackInFileExplorerCommand(CreatePackFileService(owner).Object, new Mock<IStandardDialogs>().Object, new Mock<IFileSystemAccess>().Object);

            Assert.That(command.IsEnabled(root), Is.True);
        }

        [Test]
        public void Execute_ValidPath_StartsExplorer()
        {
            var owner = CreateContainer(systemFilePath: "C:\\temp\\pack.pack");
            var service = CreatePackFileService(owner);
            var root = CreateRoot(owner);
            var fileSystem = new Mock<IFileSystemAccess>();
            fileSystem.Setup(x => x.DirectoryExists(It.IsAny<string>())).Returns(false);
            fileSystem.Setup(x => x.PathGetDirectoryName(It.IsAny<string>())).Returns("C:\\temp");

            var command = new OpenPackInFileExplorerCommand(service.Object, new Mock<IStandardDialogs>().Object, fileSystem.Object);
            command.Execute(root);

            fileSystem.Verify(x => x.ProcessStart(It.Is<ProcessStartInfo>(p => p.FileName == "explorer.exe")), Times.Once);
        }

        [Test]
        public void Execute_NullSystemFilePath_ShowsError()
        {
            var owner = CreateContainer(systemFilePath: "");
            var root = CreateRoot(owner);
            var dialogs = new Mock<IStandardDialogs>();
            var command = new OpenPackInFileExplorerCommand(CreatePackFileService(owner).Object, dialogs.Object, new Mock<IFileSystemAccess>().Object);

            command.Execute(root);

            dialogs.Verify(x => x.ShowDialogBox(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }
    }
}
