using System.Threading;
using Moq;
using Shared.Core.PackFiles.Models;
using Shared.Core.Services;
using Shared.Ui.BaseDialogs.PackFileTree;
using Shared.Ui.BaseDialogs.PackFileTree.ContextMenu.Commands;

namespace Shared.UiTest.BaseDialogs.PackFileTree.ContextMenu.Commands
{
    [TestFixture]
    internal class OpenNodeInNotepadCommandTests : ContextMenuCommandTestBase
    {
        [Test]
        public void ShouldAdd_ReturnsTrueForFileNode()
        {
            var owner = CreateContainer();
            var root = new TreeNode("root", NodeType.Root, owner, null);
            var file = new TreeNode("file.txt", NodeType.File, owner, root, PackFile.CreateFromASCII("file.txt", "a"));
            var command = new OpenNodeInNotepadCommand(new Mock<IStandardDialogs>().Object, new Mock<IFileSystemAccess>().Object);

            Assert.That(command.ShouldAdd(file), Is.True);
        }

        [Test]
        public void IsEnabled_ReturnsTrue()
        {
            var owner = CreateContainer();
            var root = new TreeNode("root", NodeType.Root, owner, null);
            var file = new TreeNode("file.txt", NodeType.File, owner, root, PackFile.CreateFromASCII("file.txt", "a"));
            var command = new OpenNodeInNotepadCommand(new Mock<IStandardDialogs>().Object, new Mock<IFileSystemAccess>().Object);

            Assert.That(command.IsEnabled(file), Is.True);
        }

        [Test]
        public void Execute_AppMissing_ShowsError()
        {
            var owner = CreateContainer();
            var root = new TreeNode("root", NodeType.Root, owner, null);
            var file = new TreeNode("file.txt", NodeType.File, owner, root, PackFile.CreateFromASCII("file.txt", "a"));

            var dialogs = new Mock<IStandardDialogs>();
            var fileSystem = new Mock<IFileSystemAccess>();
            fileSystem.Setup(x => x.FileExists(It.IsAny<string>())).Returns(false);

            var command = new OpenNodeInNotepadCommand(dialogs.Object, fileSystem.Object);
            command.Execute(file);

            dialogs.Verify(x => x.ShowDialogBox(It.Is<string>(s => s.Contains("does not exist")), It.IsAny<string>()), Times.Once);
            fileSystem.Verify(x => x.ProcessStart(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Test]
        public void Execute_AppExists_WritesTempFileAndStartsProcess()
        {
            var owner = CreateContainer();
            var root = new TreeNode("root", NodeType.Root, owner, null);
            var file = new TreeNode("file.txt", NodeType.File, owner, root, PackFile.CreateFromASCII("file.txt", "abc"));

            var dialogs = new Mock<IStandardDialogs>();
            var fileSystem = new Mock<IFileSystemAccess>();
            fileSystem.Setup(x => x.FileExists(It.IsAny<string>())).Returns(true);

            var command = new OpenNodeInNotepadCommand(dialogs.Object, fileSystem.Object);
            command.Execute(file);

            fileSystem.Verify(x => x.FileWriteAllBytes(It.IsAny<string>(), It.Is<byte[]>(b => b.Length == 3)), Times.Once);
            fileSystem.Verify(x => x.ProcessStart(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }
    }
}
