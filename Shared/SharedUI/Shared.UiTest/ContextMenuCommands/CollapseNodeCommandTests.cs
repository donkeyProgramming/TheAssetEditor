using System.Threading;
using Shared.Core.PackFiles.Models;
using Shared.Ui.BaseDialogs.PackFileTree;
using Shared.Ui.BaseDialogs.PackFileTree.ContextMenu.Commands;

namespace Shared.UiTest.ContextMenuCommands
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    internal class CollapseNodeCommandTests : ContextMenuCommandTestBase
    {
        [Test]
        public void ShouldAdd_IsEnabled_Execute()
        {
            var owner = CreateContainer();
            var root = new TreeNode("root", NodeType.Root, owner, null);
            var folder = new TreeNode("folder", NodeType.Directory, owner, root);
            var file = new TreeNode("file.txt", NodeType.File, owner, folder, PackFile.CreateFromASCII("file.txt", "a"));
            root.AddChild(folder);
            folder.AddChild(file);
            root.Children.Add(folder);
            folder.Children.Add(file);

            root.IsNodeExpanded = true;
            folder.IsNodeExpanded = true;
            file.IsNodeExpanded = true;

            var command = new CollapseNodeCommand();

            Assert.That(command.ShouldAdd(folder), Is.True);
            Assert.That(command.ShouldAdd(file), Is.False);
            Assert.That(command.IsEnabled(folder), Is.True);

            command.Execute(root);

            Assert.That(root.IsNodeExpanded, Is.False);
        }
    }
}
