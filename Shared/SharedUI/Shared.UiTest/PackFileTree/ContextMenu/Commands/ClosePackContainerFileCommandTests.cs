using Moq;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.Services;
using Shared.Ui.BaseDialogs.PackFileTree;
using Shared.Ui.BaseDialogs.PackFileTree.ContextMenu.Commands;

namespace Shared.UiTest.PackFileTree.ContextMenu.Commands
{
    [TestFixture]
    internal class ClosePackContainerFileCommandTests : ContextMenuCommandTestBase
    {
        [Test]
        public void ShouldAdd_ReturnsTrueForRoot()
        {
            var owner = CreateContainer();
            var root = new TreeNode("root", NodeType.Root, owner, null);
            var file = new TreeNode("file.txt", NodeType.File, owner, root, PackFile.CreateFromASCII("file.txt", "a"));
            var command = new ClosePackContainerFileCommand(new Mock<IPackFileService>().Object, new Mock<IStandardDialogs>().Object);

            Assert.That(command.ShouldAdd(root), Is.True);
            Assert.That(command.ShouldAdd(file), Is.False);
        }

        [Test]
        public void IsEnabled_ReturnsTrue()
        {
            var owner = CreateContainer();
            var root = new TreeNode("root", NodeType.Root, owner, null);
            var command = new ClosePackContainerFileCommand(new Mock<IPackFileService>().Object, new Mock<IStandardDialogs>().Object);

            Assert.That(command.IsEnabled(root), Is.True);
        }

        [Test]
        public void Execute_ConfirmsAndUnloadsPack()
        {
            var owner = CreateContainer();
            var root = new TreeNode("root", NodeType.Root, owner, null);
            var service = new Mock<IPackFileService>();
            var dialogs = new Mock<IStandardDialogs>();
            dialogs.Setup(x => x.ShowYesNoBox(It.IsAny<string>(), It.IsAny<string>())).Returns(ShowMessageBoxResult.OK);

            var command = new ClosePackContainerFileCommand(service.Object, dialogs.Object);

            command.Execute(root);

            service.Verify(x => x.UnloadPackContainer(owner), Times.Once);
        }
    }
}
