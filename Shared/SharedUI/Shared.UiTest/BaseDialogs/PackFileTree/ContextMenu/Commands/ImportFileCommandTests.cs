using System.Collections.Generic;
using System.IO;
using System.Threading;
using Moq;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.Services;
using Shared.Ui.BaseDialogs.PackFileTree;
using Shared.Ui.BaseDialogs.PackFileTree.ContextMenu.Commands;

namespace Shared.UiTest.BaseDialogs.PackFileTree.ContextMenu.Commands
{
    [TestFixture]
    internal class ImportFileCommandTests : ContextMenuCommandTestBase
    {
        [Test]
        public void ShouldAdd_ReturnsTrueForDirectoryNode()
        {
            var command = new ImportFileCommand(new Mock<IPackFileService>().Object, new Mock<IStandardDialogs>().Object, new Mock<IFileSystemAccess>().Object);
            Assert.That(command.ShouldAdd(new TreeNode("dir", NodeType.Directory, CreateContainer(), null), null), Is.True);
        }

        [Test]
        public void IsEnabled_ReturnsTrue()
        {
            var owner = CreateContainer(isCa: true);
            var root = new TreeNode("root", NodeType.Root, owner, null);
            var command = new ImportFileCommand(new Mock<IPackFileService>().Object, new Mock<IStandardDialogs>().Object, new Mock<IFileSystemAccess>().Object);

            Assert.That(command.IsEnabled(root, null), Is.True);
        }

        [Test]
        public void Execute_CaPackShowsErrorAndDoesNotImport()
        {
            var owner = CreateContainer(isCa: true);
            var root = new TreeNode("root", NodeType.Root, owner, null);

            var service = new Mock<IPackFileService>();
            var dialogs = new Mock<IStandardDialogs>();
            var fileSystem = new Mock<IFileSystemAccess>();
            var command = new ImportFileCommand(service.Object, dialogs.Object, fileSystem.Object);

            command.Execute(root, null);

            dialogs.Verify(x => x.ShowDialogBox("Unable to edit CA packfile", "Error"), Times.Once);
            service.Verify(x => x.AddFilesToPack(It.IsAny<IPackFileContainer>(), It.IsAny<List<NewPackFileEntry>>()), Times.Never);
        }

        [Test]
        public void Execute_DialogCancelled_DoesNotImport()
        {
            var owner = CreateContainer(isCa: false);
            var root = new TreeNode("root", NodeType.Root, owner, null);

            var service = new Mock<IPackFileService>();
            var dialogs = new Mock<IStandardDialogs>();
            dialogs.Setup(x => x.ShowSystemOpenFileDialog(It.IsAny<bool>(), It.IsAny<string>()))
                .Returns(new SystemOpenFileDialogResult(Result: false, FilePaths: []));
            var fileSystem = new Mock<IFileSystemAccess>();
            var command = new ImportFileCommand(service.Object, dialogs.Object, fileSystem.Object);

            command.Execute(root, null);

            service.Verify(x => x.AddFilesToPack(It.IsAny<IPackFileContainer>(), It.IsAny<List<NewPackFileEntry>>()), Times.Never);
            fileSystem.Verify(x => x.FileReadAllBytes(It.IsAny<string>()), Times.Never);
        }

        [Test]
        public void Execute_FileSelected_ImportsFileWithMockedRead()
        {
            var owner = CreateContainer(isCa: false);
            var root = new TreeNode("root", NodeType.Root, owner, null);

            var fileBytes = new byte[] { 0x01, 0x02, 0x03 };
            var filePath = "C:\\test\\file.txt";

            var service = new Mock<IPackFileService>();
            var dialogs = new Mock<IStandardDialogs>();
            dialogs.Setup(x => x.ShowSystemOpenFileDialog(It.IsAny<bool>(), It.IsAny<string>()))
                .Returns(new SystemOpenFileDialogResult(Result: true, FilePaths: [filePath]));
            var fileSystem = new Mock<IFileSystemAccess>();
            fileSystem.Setup(x => x.FileReadAllBytes(filePath)).Returns(fileBytes);
            var command = new ImportFileCommand(service.Object, dialogs.Object, fileSystem.Object);

            command.Execute(root, null);

            fileSystem.Verify(x => x.FileReadAllBytes(filePath), Times.Once);
            service.Verify(x => x.AddFilesToPack(owner, It.Is<List<NewPackFileEntry>>(items =>
                items.Count == 1)), Times.Once);
        }
    }
}
