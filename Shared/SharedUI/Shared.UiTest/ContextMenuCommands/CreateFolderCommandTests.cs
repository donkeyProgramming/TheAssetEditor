using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Moq;
using Shared.Core.Services;
using Shared.Ui.BaseDialogs.PackFileTree;
using Shared.Ui.BaseDialogs.PackFileTree.ContextMenu.Commands;

namespace Shared.UiTest.ContextMenuCommands
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    internal class CreateFolderCommandTests : ContextMenuCommandTestBase
    {
        [Test]
        public void ShouldAdd_IsEnabled_Execute()
        {
            var owner = CreateContainer();
            var root = new TreeNode("root", NodeType.Root, owner, null);
            var existing = new TreeNode("existing", NodeType.Directory, owner, root);
            root.AddChild(existing);

            var dialogs = new Mock<IStandardDialogs>();
            dialogs.Setup(x => x.ShowFolderNameDialog(It.IsAny<IEnumerable<string>>(), It.IsAny<string>())).Returns("new_folder");

            var command = new CreateFolderCommand(dialogs.Object);

            Assert.That(command.ShouldAdd(root), Is.True);
            Assert.That(command.IsEnabled(root), Is.True);

            command.Execute(root);

            Assert.That(root.BackingChildren.Any(x => x.Name == "new_folder"), Is.True);
        }
    }
}
