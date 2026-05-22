using Moq;
using Shared.Core.Services;
using Shared.Ui.BaseDialogs.PackFileTree;
using Shared.Ui.BaseDialogs.PackFileTree.ContextMenu.Commands;
using Shared.Ui.BaseDialogs.PackFileTree.Utility;

namespace Shared.UiTest.BaseDialogs.PackFileTree.ContextMenu.Commands
{
    [TestFixture]
    internal class CopyToEditablePackCommandTests : ContextMenuCommandTestBase
    {
        [Test]
        public void ShouldAdd_ReturnsTrueWhenEditablePackExists()
        {
            var source = AddPackFiles(false, "source", "c:\\source.pack", ["folder\\file.txt"]);
            var target = AddPackFiles(false, "target", "c:\\target.pack", []);
            _packFileService.SetEditablePack(target);

            var viewModel = PackFileBrowser();
            var node = TreeNodeHelper.FindNode(viewModel, source, "folder\\file.txt");

            var command = new CopyToEditablePackCommand(_packFileService, new Mock<IStandardDialogs>().Object);

            Assert.That(command.ShouldAdd(node), Is.True);
        }

        [Test]
        public void IsEnabled_ReturnsTrue()
        {
            var source = AddPackFiles(false, "source", "c:\\source.pack", ["folder\\file.txt"]);
            var viewModel = PackFileBrowser();
            var node = TreeNodeHelper.FindNode(viewModel, source, "folder\\file.txt");

            var command = new CopyToEditablePackCommand(_packFileService, new Mock<IStandardDialogs>().Object);

            Assert.That(command.IsEnabled(node), Is.True);
        }

        [Test]
        public void Execute_CopiesChildFilesToEditablePack()
        {
            // Arrange
            var source = AddPackFiles(false, "source", "c:\\source.pack", ["folder\\file.txt"]);
            var target = AddPackFiles(false, "target", "c:\\target.pack", []);
            _packFileService.SetEditablePack(target);

            var viewModel = PackFileBrowser();
            var root = viewModel.Files.First(x => (x as RootTreeNode)!.Owner == source);
            var folder = root.Children.First(x => x.NodeType == NodeType.Directory);

            var dialogs = new Mock<IStandardDialogs>();
            var waitCursor = new Mock<IWaitCursor>();
            dialogs.Setup(x => x.ShowWaitCursor()).Returns(waitCursor.Object);

            // Act
            var command = new CopyToEditablePackCommand(_packFileService, dialogs.Object);
            command.Configure(folder);

            command.Execute();

            // Assert
            var copiedFile = target.FindFile("folder\\file.txt");
            Assert.That(copiedFile, Is.Not.Null);
        }
    }
}
