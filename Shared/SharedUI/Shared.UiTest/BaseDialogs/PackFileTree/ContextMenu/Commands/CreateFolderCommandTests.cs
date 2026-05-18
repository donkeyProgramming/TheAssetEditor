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
            var service = CreatePackFileService(owner);
            var root = CreateRoot(owner);
            var command = new CreateFolderCommand(service.Object, new Mock<IStandardDialogs>().Object, s_treeMutationService);

            Assert.That(command.ShouldAdd(root), Is.True);
        }

        [Test]
        public void IsEnabled_ReturnsTrue()
        {
            var owner = CreateContainer();
            var root = CreateRoot(owner);
            var command = new CreateFolderCommand(CreatePackFileService(owner).Object, new Mock<IStandardDialogs>().Object, s_treeMutationService);

            Assert.That(command.IsEnabled(root), Is.True);
        }

        [Test]
        public void Execute_AddsFolderChild()
        {
            var owner = CreateContainer();
            var root = CreateRoot(owner);
            var existing = new TreeNode("existing", NodeType.Directory, root);
            root.AddChild(existing);

            var service = CreatePackFileService(owner);
            var dialogs = new Mock<IStandardDialogs>();
            dialogs.Setup(x => x.ShowFolderNameDialog(It.IsAny<IEnumerable<string>>(), It.IsAny<string>())).Returns("new_folder");

            var command = new CreateFolderCommand(service.Object, dialogs.Object, s_treeMutationService);

            command.Execute(root);

            Assert.That(root.Children.Any(x => x.Name == "new_folder"), Is.True);
        }
    }
}
