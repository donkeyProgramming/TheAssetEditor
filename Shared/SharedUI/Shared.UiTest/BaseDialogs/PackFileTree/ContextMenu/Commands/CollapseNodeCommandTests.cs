using Shared.Ui.BaseDialogs.PackFileTree;
using Shared.Ui.BaseDialogs.PackFileTree.ContextMenu.Commands;
using Shared.Ui.BaseDialogs.PackFileTree.Utility;

namespace Shared.UiTest.BaseDialogs.PackFileTree.ContextMenu.Commands
{
    [TestFixture]
    internal class CollapseNodeCommandTests : ContextMenuCommandTestBase
    {
        [Test]
        public void ShouldAdd_ReturnsTrueForFolderAndFalseForFile()
        {
            var container = AddPackFiles(false, "modfile", "c:\\mymod.pack", ["folder\\file.txt"]);
            var viewModel = PackFileBrowser();
            var root = viewModel.Files.First();
            var folder = root.Children.First(x => x.NodeType == NodeType.Directory);
            var file = TreeNodeHelper.FindNode(viewModel, container, "folder\\file.txt");

            var command = new CollapseNodeCommand();

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

            var command = new CollapseNodeCommand();

            Assert.That(command.IsEnabled(folder), Is.True);
        }

        [Test]
        public void Execute_CollapsesRootNode()
        {
            AddPackFiles(false, "modfile", "c:\\mymod.pack", ["folder\\file.txt"]);
            var viewModel = PackFileBrowser();
            var root = viewModel.Files.First();

            root.IsNodeExpanded = true;
            root.Children.First().IsNodeExpanded = true;

            var command = new CollapseNodeCommand();
            command.Configure(root);

            command.Execute();

            Assert.That(root.IsNodeExpanded, Is.False);
        }

        [Test]
        public void Execute_CollapsesNestedChildren()
        {
            AddPackFiles(false, "modfile", "c:\\mymod.pack", ["folder\\nested\\file.txt"]);
            var viewModel = PackFileBrowser();
            var root = viewModel.Files.First();
            var folder = root.Children.First();
            var nested = folder.Children.First(x => x.NodeType == NodeType.Directory);

            root.IsNodeExpanded = true;
            folder.IsNodeExpanded = true;
            nested.IsNodeExpanded = true;

            var command = new CollapseNodeCommand();
            command.Configure(root);

            command.Execute();

            Assert.That(root.IsNodeExpanded, Is.False);
            Assert.That(folder.IsNodeExpanded, Is.False);
            Assert.That(nested.IsNodeExpanded, Is.False);
        }
    }
}
