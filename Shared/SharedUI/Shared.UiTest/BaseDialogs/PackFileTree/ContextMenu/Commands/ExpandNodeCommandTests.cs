using Shared.Ui.BaseDialogs.PackFileTree;
using Shared.Ui.BaseDialogs.PackFileTree.ContextMenu.Commands;
using Shared.Ui.BaseDialogs.PackFileTree.Utility;

namespace Shared.UiTest.BaseDialogs.PackFileTree.ContextMenu.Commands
{
    [TestFixture]
    internal class ExpandNodeCommandTests : ContextMenuCommandTestBase
    {
        [Test]
        public void ShouldAdd_ReturnsTrueForFolderAndFalseForFile()
        {
            var container = AddPackFiles(false, "modfile", "c:\\mymod.pack", ["folder\\file.txt"]);
            var viewModel = PackFileBrowser();
            var root = viewModel.Files.First();
            var folder = root.Children.First(x => x.NodeType == NodeType.Directory);
            var file = TreeNodeHelper.FindNode(viewModel, container, "folder\\file.txt");

            var command = new ExpandNodeCommand();

            Assert.That(command.ShouldAdd(folder), Is.True);
            Assert.That(command.ShouldAdd(file), Is.False);
        }

        [Test]
        public void IsEnabled_ReturnsTrue()
        {
            AddPackFiles(false, "modfile", "c:\\mymod.pack", ["folder\\file.txt"]);
            var viewModel = PackFileBrowser();
            var root = viewModel.Files.First();
            var folder = root.Children.First(x => x.NodeType == NodeType.Directory);

            var command = new ExpandNodeCommand();

            Assert.That(command.IsEnabled(folder), Is.True);
        }

        [Test]
        public void Execute_ExpandsAllNodes()
        {
            AddPackFiles(false, "modfile", "c:\\mymod.pack", ["folder\\file.txt"]);
            var viewModel = PackFileBrowser();
            var root = viewModel.Files.First();

            root.IsNodeExpanded = false;
            root.Children.First().IsNodeExpanded = false;

            var command = new ExpandNodeCommand();
            command.Configure(root);

            command.Execute();

            Assert.That(root.IsNodeExpanded, Is.True);
            Assert.That(root.Children.First().IsNodeExpanded, Is.True);
        }

        [Test]
        public void Execute_ExpandsNestedChildren()
        {
            AddPackFiles(false, "modfile", "c:\\mymod.pack", ["folder\\nested\\file.txt"]);
            var viewModel = PackFileBrowser();
            var root = viewModel.Files.First();
            var folder = root.Children.First();
            var nested = folder.Children.First(x => x.NodeType == NodeType.Directory);

            root.IsNodeExpanded = false;
            folder.IsNodeExpanded = false;
            nested.IsNodeExpanded = false;

            var command = new ExpandNodeCommand();
            command.Configure(root);

            command.Execute();

            Assert.That(root.IsNodeExpanded, Is.True);
            Assert.That(folder.IsNodeExpanded, Is.True);
            Assert.That(nested.IsNodeExpanded, Is.True);
        }
    }
}
