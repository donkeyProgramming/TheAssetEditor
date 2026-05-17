using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Moq;
using Shared.Core.Services;
using Shared.Ui.BaseDialogs.PackFileTree;
using Shared.Ui.BaseDialogs.PackFileTree.ContextMenu.Commands;

namespace Shared.UiTest.BaseDialogs.PackFileTree.ContextMenu.Commands
{
    [TestFixture]
    internal class CreateFolderCommandTests : ContextMenuCommandTestBase
    {
        private static readonly PackFileTreeMutationService s_treeMutationService = new();

        [Test]
        public void ShouldAdd_ReturnsTrueForEditableRoot()
        {
            var owner = CreateContainer();
            var root = new TreeNode("root", NodeType.Root, owner, null);
            var command = new CreateFolderCommand(new Mock<IStandardDialogs>().Object, s_treeMutationService);

            Assert.That(command.ShouldAdd(root), Is.True);
        }

        [Test]
        public void IsEnabled_ReturnsTrue()
        {
            var owner = CreateContainer();
            var root = new TreeNode("root", NodeType.Root, owner, null);
            var command = new CreateFolderCommand(new Mock<IStandardDialogs>().Object, s_treeMutationService);

            Assert.That(command.IsEnabled(root), Is.True);
        }

        [Test]
        public void Execute_AddsFolderChild()
        {
            var owner = CreateContainer();
            var root = new TreeNode("root", NodeType.Root, owner, null);
            var existing = new TreeNode("existing", NodeType.Directory, owner, root);
            root.AddChild(existing);

            var dialogs = new Mock<IStandardDialogs>();
            dialogs.Setup(x => x.ShowFolderNameDialog(It.IsAny<IEnumerable<string>>(), It.IsAny<string>())).Returns("new_folder");

            var command = new CreateFolderCommand(dialogs.Object, s_treeMutationService);

            command.Execute(root);

            Assert.That(root.Children.Any(x => x.Name == "new_folder"), Is.True);
        }
    }
}
