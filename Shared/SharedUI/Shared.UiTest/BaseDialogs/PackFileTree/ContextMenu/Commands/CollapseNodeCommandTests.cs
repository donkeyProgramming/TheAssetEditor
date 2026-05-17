using Shared.Core.PackFiles.Models;
using Shared.Ui.BaseDialogs.PackFileTree;
using Shared.Ui.BaseDialogs.PackFileTree.ContextMenu.Commands;

namespace Shared.UiTest.BaseDialogs.PackFileTree.ContextMenu.Commands
{
    [TestFixture]
    internal class CollapseNodeCommandTests : ContextMenuCommandTestBase
    {
        [Test]
        public void ShouldAdd_ReturnsTrueForFolderAndFalseForFile()
        {
            var owner = CreateContainer();
            var root = new TreeNode("root", NodeType.Root, owner, null);
            var folder = new TreeNode("folder", NodeType.Directory, owner, root);
            var file = new TreeNode("file.txt", NodeType.File, owner, folder, PackFile.CreateFromASCII("file.txt", "a"));
            var command = new CollapseNodeCommand();

            Assert.That(command.ShouldAdd(folder), Is.True);
            Assert.That(command.ShouldAdd(file), Is.False);
        }

        [Test]
        public void IsEnabled_ReturnsTrue()
        {
            var owner = CreateContainer();
            var root = new TreeNode("root", NodeType.Root, owner, null);
            var folder = new TreeNode("folder", NodeType.Directory, owner, root);
            var command = new CollapseNodeCommand();

            Assert.That(command.IsEnabled(folder), Is.True);
        }

        [Test]
        public void Execute_CollapsesRootNode()
        {
            var owner = CreateContainer();
            var root = new TreeNode("root", NodeType.Root, owner, null);
            var folder = new TreeNode("folder", NodeType.Directory, owner, root);
            var file = new TreeNode("file.txt", NodeType.File, owner, folder, PackFile.CreateFromASCII("file.txt", "a"));
            root.AddChild(folder);
            folder.AddChild(file);

            root.IsNodeExpanded = true;
            folder.IsNodeExpanded = true;
            file.IsNodeExpanded = true;

            var command = new CollapseNodeCommand();

            command.Execute(root);

            Assert.That(root.IsNodeExpanded, Is.False);
        }

        [Test]
        public void Execute_CollapsesNestedChildren()
        {
            var owner = CreateContainer();
            var root = new TreeNode("root", NodeType.Root, owner, null);
            var folder = new TreeNode("folder", NodeType.Directory, owner, root);
            var nested = new TreeNode("nested", NodeType.Directory, owner, folder);

            root.AddChild(folder);
            folder.AddChild(nested);

            root.IsNodeExpanded = true;
            folder.IsNodeExpanded = true;
            nested.IsNodeExpanded = true;

            var command = new CollapseNodeCommand();
            command.Execute(root);

            Assert.That(root.IsNodeExpanded, Is.False);
            Assert.That(folder.IsNodeExpanded, Is.False);
            Assert.That(nested.IsNodeExpanded, Is.False);
        }
    }
}
