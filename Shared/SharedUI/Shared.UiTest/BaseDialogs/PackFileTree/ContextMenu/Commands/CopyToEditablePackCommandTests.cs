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
    internal class CopyToEditablePackCommandTests : ContextMenuCommandTestBase
    {
        [Test]
        public void ShouldAdd_ReturnsTrueWhenEditablePackExists()
        {
            var source = CreateContainer(name: "source");
            var target = CreateContainer(name: "target");
            var service = new Mock<IPackFileService>();
            service.Setup(x => x.GetEditablePack()).Returns(target);
            var command = new CopyToEditablePackCommand(service.Object, new Mock<IStandardDialogs>().Object);
            var node = new TreeNode("folder", NodeType.Directory, source, null);

            Assert.That(command.ShouldAdd(node, null), Is.True);
        }

        [Test]
        public void IsEnabled_ReturnsTrue()
        {
            var source = CreateContainer(name: "source");
            var node = new TreeNode("folder", NodeType.Directory, source, null);
            var command = new CopyToEditablePackCommand(new Mock<IPackFileService>().Object, new Mock<IStandardDialogs>().Object);

            Assert.That(command.IsEnabled(node, null), Is.True);
        }

        [Test]
        public void Execute_CopiesChildFilesToEditablePack()
        {
            var source = CreateContainer(name: "source");
            var target = CreateContainer(name: "target");
            var service = new Mock<IPackFileService>();
            service.Setup(x => x.GetEditablePack()).Returns(target);

            var dialogs = new Mock<IStandardDialogs>();
            var waitCursor = new Mock<IWaitCursor>();
            dialogs.Setup(x => x.ShowWaitCursor()).Returns(waitCursor.Object);

            var root = new TreeNode("root", NodeType.Root, source, null);
            var folder = new TreeNode("folder", NodeType.Directory, source, root);
            root.AddChild(folder);
            var file = new TreeNode("file.txt", NodeType.File, source, folder);
            folder.AddChild(file);

            var command = new CopyToEditablePackCommand(service.Object, dialogs.Object);

            command.Execute(folder, null);

            service.Verify(x => x.CopyFileFromOtherPackFile(source, It.IsAny<string>(), target), Times.Once);
        }
    }
}
