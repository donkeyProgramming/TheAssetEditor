using System.Threading;
using Moq;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Ui.BaseDialogs.PackFileTree;
using Shared.Ui.BaseDialogs.PackFileTree.ContextMenu.Commands;

namespace Shared.UiTest.BaseDialogs.PackFileTree.ContextMenu.Commands
{
    [TestFixture]
    internal class CopyNodePathCommandTests : ContextMenuCommandTestBase
    {
        [Test]
        public void ShouldAdd_ReturnsTrueForFile()
        {
            var owner = CreateContainer();
            var root = new TreeNode("root", NodeType.Root, owner, null);
            var file = new TreeNode("file.txt", NodeType.File, owner, root);
            var command = new CopyNodePathCommand();

            Assert.That(command.ShouldAdd(file, null), Is.True);
        }

        [Test]
        public void IsEnabled_ReturnsTrue()
        {
            var owner = CreateContainer();
            var root = new TreeNode("root", NodeType.Root, owner, null);
            var file = new TreeNode("file.txt", NodeType.File, owner, root);
            var command = new CopyNodePathCommand();

            Assert.That(command.IsEnabled(file, null), Is.True);
        }

        [Test]
        [Apartment(ApartmentState.STA)]
        public void Execute_CopiesNodePathToClipboard()
        {
            var owner = CreateContainer();
            var root = new TreeNode("root", NodeType.Root, owner, null);
            var folder = new TreeNode("folder", NodeType.Directory, owner, root);
            var file = new TreeNode("file.txt", NodeType.File, owner, folder);
            folder.AddChild(file);
            root.AddChild(folder);

            var command = new CopyNodePathCommand();

            command.Execute(file, null);

            Assert.That(System.Windows.Clipboard.GetText(), Is.EqualTo("folder\\file.txt"));
        }
    }
}
