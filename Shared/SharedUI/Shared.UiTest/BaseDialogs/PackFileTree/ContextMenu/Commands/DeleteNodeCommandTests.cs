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
    internal class DeleteNodeCommandTests : ContextMenuCommandTestBase
    {
        [Test]
        public void ShouldAdd_ReturnsTrueForFile()
        {
            var owner = CreateContainer();
            var root = CreateRoot(owner);
            var packFile = PackFile.CreateFromASCII("file.txt", "a");
            var file = new TreeNode("file.txt", NodeType.File, root);
            var service = CreatePackFileService(owner, packFile);
            var command = new DeleteNodeCommand(service.Object, new Mock<IStandardDialogs>().Object);

            Assert.That(command.ShouldAdd(file), Is.True);
        }

        [Test]
        public void IsEnabled_ReturnsTrue()
        {
            var owner = CreateContainer();
            var root = CreateRoot(owner);
            var packFile = PackFile.CreateFromASCII("file.txt", "a");
            var file = new TreeNode("file.txt", NodeType.File, root);
            var command = new DeleteNodeCommand(CreatePackFileService(owner, packFile).Object, new Mock<IStandardDialogs>().Object);

            Assert.That(command.IsEnabled(file), Is.True);
        }

        [Test]
        public void Execute_DeletesFileAfterConfirmation()
        {
            var owner = CreateContainer();
            var root = CreateRoot(owner);
            var packFile = PackFile.CreateFromASCII("file.txt", "a");
            var file = new TreeNode("file.txt", NodeType.File, root);
            root.AddChild(file);

            var service = CreatePackFileService(owner, packFile);
            var dialogs = new Mock<IStandardDialogs>();
            dialogs.Setup(x => x.ShowYesNoBox(It.IsAny<string>(), It.IsAny<string>())).Returns(ShowMessageBoxResult.OK);

            var command = new DeleteNodeCommand(service.Object, dialogs.Object);

            command.Execute(file);

            service.Verify(x => x.DeleteFile(owner, packFile), Times.Once);
        }
    }
}
