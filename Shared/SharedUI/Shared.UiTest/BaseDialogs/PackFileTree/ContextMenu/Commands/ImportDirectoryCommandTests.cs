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
    internal class ImportDirectoryCommandTests : ContextMenuCommandTestBase
    {
        [Test]
        public void ShouldAdd_ReturnsTrueForDirectoryNode()
        {
            var command = new ImportDirectoryCommand(new Mock<IPackFileService>().Object, new Mock<IStandardDialogs>().Object, new Mock<IFileSystemAccess>().Object);
            Assert.That(command.ShouldAdd(new TreeNode("dir", NodeType.Directory, CreateContainer(), null)), Is.True);
        }

        [Test]
        public void IsEnabled_ReturnsTrue()
        {
            var owner = CreateContainer(isCa: true);
            var root = new TreeNode("root", NodeType.Root, owner, null);
            var command = new ImportDirectoryCommand(new Mock<IPackFileService>().Object, new Mock<IStandardDialogs>().Object, new Mock<IFileSystemAccess>().Object);

            Assert.That(command.IsEnabled(root), Is.True);
        }

        [Test]
        public void Execute_CaPackShowsErrorAndDoesNotImport()
        {
            var owner = CreateContainer(isCa: true);
            var root = new TreeNode("root", NodeType.Root, owner, null);

            var service = new Mock<IPackFileService>();
            var dialogs = new Mock<IStandardDialogs>();
            var command = new ImportDirectoryCommand(service.Object, dialogs.Object, new Mock<IFileSystemAccess>().Object);

            command.Execute(root);

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
            dialogs.Setup(x => x.ShowSystemFolderBrowserDialog())
                .Returns(new SystemBrowseFolderDialogResult(Result: false, FolderPath: string.Empty));
            var fileSystem = new Mock<IFileSystemAccess>();
            var command = new ImportDirectoryCommand(service.Object, dialogs.Object, fileSystem.Object);

            command.Execute(root);

            service.Verify(x => x.AddFilesToPack(It.IsAny<IPackFileContainer>(), It.IsAny<List<NewPackFileEntry>>()), Times.Never);
            fileSystem.Verify(x => x.FileReadAllBytes(It.IsAny<string>()), Times.Never);
        }

        [Test]
        public void Execute_DirectorySelected_ImportsFilesWithMockedReads()
        {
            var owner = CreateContainer(isCa: false);
            var root = new TreeNode("root", NodeType.Root, owner, null);

            var folderPath = "C:\\test\\folder";
            var file1Path = "C:\\test\\folder\\file1.txt";
            var file2Path = "C:\\test\\folder\\subdir\\file2.txt";
            var file1Bytes = new byte[] { 0x01, 0x02 };
            var file2Bytes = new byte[] { 0x03, 0x04, 0x05 };

            var service = new Mock<IPackFileService>();
            var dialogs = new Mock<IStandardDialogs>();
            dialogs.Setup(x => x.ShowSystemFolderBrowserDialog())
                .Returns(new SystemBrowseFolderDialogResult(Result: true, FolderPath: folderPath));
            var fileSystem = new Mock<IFileSystemAccess>();
            fileSystem.Setup(x => x.CreateDirectoryInfo(folderPath)).Returns(new DirectoryInfo(folderPath));
            fileSystem.Setup(x => x.DirectoryGetFiles(folderPath, "*", SearchOption.AllDirectories))
                .Returns([file1Path, file2Path]);
            fileSystem.Setup(x => x.PathGetFileName(file1Path)).Returns("file1.txt");
            fileSystem.Setup(x => x.PathGetFileName(file2Path)).Returns("file2.txt");
            fileSystem.Setup(x => x.FileReadAllBytes(file1Path)).Returns(file1Bytes);
            fileSystem.Setup(x => x.FileReadAllBytes(file2Path)).Returns(file2Bytes);
            var command = new ImportDirectoryCommand(service.Object, dialogs.Object, fileSystem.Object);

            command.Execute(root);

            fileSystem.Verify(x => x.FileReadAllBytes(file1Path), Times.Once);
            fileSystem.Verify(x => x.FileReadAllBytes(file2Path), Times.Once);
            service.Verify(x => x.AddFilesToPack(owner, It.Is<List<NewPackFileEntry>>(items =>
                items.Count == 2)), Times.Once);
        }
    }
}
