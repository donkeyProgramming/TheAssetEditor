using System.Threading;
using Shared.Core.PackFiles.Models;
using Shared.Ui.BaseDialogs.PackFileTree;
using Shared.Ui.BaseDialogs.PackFileTree.ContextMenu.Commands;

namespace Shared.UiTest.BaseDialogs.PackFileTree.ContextMenu.Commands
{
    [TestFixture]
    internal class ExpandNodeCommandTests : ContextMenuCommandTestBase
    {
        [Test]
        public void ShouldAdd_ReturnsTrueForFolderAndFalseForFile()
        {
            var owner = CreateContainer();
            var root = new TreeNode("root", NodeType.Root, owner, null);
            var folder = new TreeNode("folder", NodeType.Directory, owner, root);
            var file = new TreeNode("file.txt", NodeType.File, owner, folder);
            var command = new ExpandNodeCommand();

            Assert.That(command.ShouldAdd(folder, null), Is.True);
            Assert.That(command.ShouldAdd(file, null), Is.False);
        }

        [Test]
        public void IsEnabled_ReturnsTrue()
        {
            var owner = CreateContainer();
            var root = new TreeNode("root", NodeType.Root, owner, null);
            var folder = new TreeNode("folder", NodeType.Directory, owner, root);
            var command = new ExpandNodeCommand();

            Assert.That(command.IsEnabled(folder, null), Is.True);
        }

        [Test]
        public void Execute_ExpandsAllNodes()
        {
            var owner = CreateContainer();
            var root = new TreeNode("root", NodeType.Root, owner, null);
            var folder = new TreeNode("folder", NodeType.Directory, owner, root);
            var file = new TreeNode("file.txt", NodeType.File, owner, folder);
            root.AddChild(folder);
            folder.AddChild(file);

            root.IsNodeExpanded = false;
            folder.IsNodeExpanded = false;
            file.IsNodeExpanded = false;

            var command = new ExpandNodeCommand();

            command.Execute(root, null);

            Assert.That(root.IsNodeExpanded, Is.True);
            Assert.That(folder.IsNodeExpanded, Is.True);
            Assert.That(file.IsNodeExpanded, Is.True);
        }

        [Test]
        public void Execute_ExpandsNestedChildren()
        {
            var owner = CreateContainer();
            var root = new TreeNode("root", NodeType.Root, owner, null);
            var folder = new TreeNode("folder", NodeType.Directory, owner, root);
            var nested = new TreeNode("nested", NodeType.Directory, owner, folder);

            root.AddChild(folder);
            folder.AddChild(nested);

            root.IsNodeExpanded = false;
            folder.IsNodeExpanded = false;
            nested.IsNodeExpanded = false;

            var command = new ExpandNodeCommand();
            command.Execute(root, null);

            Assert.That(root.IsNodeExpanded, Is.True);
            Assert.That(folder.IsNodeExpanded, Is.True);
            Assert.That(nested.IsNodeExpanded, Is.True);
        }
    }
}
