using System.Threading;
using Moq;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Ui.BaseDialogs.PackFileTree;
using Shared.Ui.BaseDialogs.PackFileTree.ContextMenu.Commands;

namespace Shared.UiTest.ContextMenuCommands
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    internal class SetAsEditablePackCommandTests : ContextMenuCommandTestBase
    {
        [Test]
        public void ShouldAdd_IsEnabled_Execute()
        {
            var owner = CreateContainer();
            var root = new TreeNode("root", NodeType.Root, owner, null);
            var service = new Mock<IPackFileService>();
            service.Setup(x => x.GetEditablePack()).Returns((IPackFileContainer?)null);
            var command = new SetAsEditablePackCommand(service.Object);

            Assert.That(command.ShouldAdd(root), Is.True);
            Assert.That(command.IsEnabled(root), Is.True);

            command.Execute(root);

            service.Verify(x => x.SetEditablePack(owner), Times.Once);
        }
    }
}
