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
    internal class RenameNodeCommandTests : ContextMenuCommandTestBase
    {
        [Test]
        public void ShouldAdd_ReturnsTrueForFile()
        {
            var owner = CreateContainer();
            var root = new TreeNode("root", NodeType.Root, owner, null);
            var packFile = PackFile.CreateFromASCII("file.txt", "a");
            var file = new TreeNode("file.txt", NodeType.File, owner, root);
            var command = new RenameNodeCommand(new Mock<IPackFileService>().Object, new Mock<IStandardDialogs>().Object);

            Assert.That(command.ShouldAdd(file, packFile), Is.True);
        }

        [Test]
        public void IsEnabled_ReturnsTrue()
        {
            var owner = CreateContainer();
            var root = new TreeNode("root", NodeType.Root, owner, null);
            var packFile = PackFile.CreateFromASCII("file.txt", "a");
            var file = new TreeNode("file.txt", NodeType.File, owner, root);
            var command = new RenameNodeCommand(new Mock<IPackFileService>().Object, new Mock<IStandardDialogs>().Object);

            Assert.That(command.IsEnabled(file, packFile), Is.True);
        }

        [Test]
        public void Execute_RenamesFile()
        {
            var owner = CreateContainer();
            var root = new TreeNode("root", NodeType.Root, owner, null);
            var packFile = PackFile.CreateFromASCII("file.txt", "a");
            var file = new TreeNode("file.txt", NodeType.File, owner, root);
            root.AddChild(file);

            var service = new Mock<IPackFileService>();
            var dialogs = new Mock<IStandardDialogs>();
            dialogs.Setup(x => x.ShowTextInputDialog("Rename file", file.Name)).Returns(new TextInputDialogResult(true, "renamed.txt"));
            var command = new RenameNodeCommand(service.Object, dialogs.Object);

            command.Execute(file, packFile);

            service.Verify(x => x.RenameFile(owner, packFile, "renamed.txt"), Times.Once);
        }
    }
}
