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
            var owner = CreateContainer();
            var service = CreatePackFileService(owner);
            var root = CreateRoot(owner);
            var directory = new TreeNode("dir", NodeType.Directory, root);
            var command = new ImportFileCommand(service.Object, new Mock<IStandardDialogs>().Object, new Mock<IFileSystemAccess>().Object);

            Assert.That(command.ShouldAdd(directory), Is.True);
        }

        [Test]
        public void IsEnabled_ReturnsTrue()
        {
            var owner = CreateContainer(isCa: true);
            var root = CreateRoot(owner);
            var command = new ImportFileCommand(CreatePackFileService(owner).Object, new Mock<IStandardDialogs>().Object, new Mock<IFileSystemAccess>().Object);

            Assert.That(command.IsEnabled(root), Is.True);
        }

        [Test]
        public void Execute_CaPackShowsErrorAndDoesNotImport()
        {
            var owner = CreateContainer(isCa: true);
            var root = CreateRoot(owner);

            var service = CreatePackFileService(owner);
            var dialogs = new Mock<IStandardDialogs>();
            var fileSystem = new Mock<IFileSystemAccess>();
            var command = new ImportFileCommand(service.Object, dialogs.Object, fileSystem.Object);

            command.Execute(root);

            dialogs.Verify(x => x.ShowDialogBox("Unable to edit CA packfile", "Error"), Times.Once);
            service.Verify(x => x.AddFilesToPack(It.IsAny<IPackFileContainer>(), It.IsAny<List<NewPackFileEntry>>()), Times.Never);
        }

        [Test]
        public void Execute_DialogCancelled_DoesNotImport()
        {
            var owner = CreateContainer(isCa: false);
            var root = CreateRoot(owner);

            var service = CreatePackFileService(owner);
            var dialogs = new Mock<IStandardDialogs>();
            dialogs.Setup(x => x.ShowSystemOpenFileDialog(It.IsAny<bool>(), It.IsAny<string>()))
                .Returns(new SystemOpenFileDialogResult(Result: false, FilePaths: []));
            var fileSystem = new Mock<IFileSystemAccess>();
            var command = new ImportFileCommand(service.Object, dialogs.Object, fileSystem.Object);

            command.Execute(root);

            service.Verify(x => x.AddFilesToPack(It.IsAny<IPackFileContainer>(), It.IsAny<List<NewPackFileEntry>>()), Times.Never);
            fileSystem.Verify(x => x.FileReadAllBytes(It.IsAny<string>()), Times.Never);
        }

        [Test]
        public void Execute_FileSelected_ImportsFileWithMockedRead()
        {
            var owner = CreateContainer(isCa: false);
            var root = CreateRoot(owner);

            var fileBytes = new byte[] { 0x01, 0x02, 0x03 };
            var filePath = "C:\\test\\file.txt";

            var service = CreatePackFileService(owner);
            var dialogs = new Mock<IStandardDialogs>();
            dialogs.Setup(x => x.ShowSystemOpenFileDialog(It.IsAny<bool>(), It.IsAny<string>()))
                .Returns(new SystemOpenFileDialogResult(Result: true, FilePaths: [filePath]));
            var fileSystem = new Mock<IFileSystemAccess>();
            fileSystem.Setup(x => x.FileReadAllBytes(filePath)).Returns(fileBytes);
            var command = new ImportFileCommand(service.Object, dialogs.Object, fileSystem.Object);

            command.Execute(root);

            fileSystem.Verify(x => x.FileReadAllBytes(filePath), Times.Once);
            service.Verify(x => x.AddFilesToPack(owner, It.Is<List<NewPackFileEntry>>(items =>
                items.Count == 1)), Times.Once);
        }
    }
}
