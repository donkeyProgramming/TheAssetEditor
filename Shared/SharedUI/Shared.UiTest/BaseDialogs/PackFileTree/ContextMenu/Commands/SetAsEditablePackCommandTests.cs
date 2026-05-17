using System.Threading;
using Moq;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Ui.BaseDialogs.PackFileTree;
using Shared.Ui.BaseDialogs.PackFileTree.ContextMenu.Commands;

namespace Shared.UiTest.BaseDialogs.PackFileTree.ContextMenu.Commands
{
    [TestFixture]
    internal class SetAsEditablePackCommandTests : ContextMenuCommandTestBase
    {
        [Test]
        public void ShouldAdd_ReturnsTrueForRoot()
        {
            var owner = CreateContainer();
            var root = new TreeNode("root", NodeType.Root, owner, null);
            var command = new SetAsEditablePackCommand(new Mock<IPackFileService>().Object);

            Assert.That(command.ShouldAdd(root, null), Is.True);
        }

        [Test]
        public void IsEnabled_ReturnsTrue()
        {
            var owner = CreateContainer();
            var root = new TreeNode("root", NodeType.Root, owner, null);
            var command = new SetAsEditablePackCommand(new Mock<IPackFileService>().Object);

            Assert.That(command.IsEnabled(root, null), Is.True);
        }

        [Test]
        public void Execute_SetsEditablePack()
        {
            var owner = CreateContainer();
            var root = new TreeNode("root", NodeType.Root, owner, null);
            var service = new Mock<IPackFileService>();
            service.Setup(x => x.GetEditablePack()).Returns((IPackFileContainer?)null);
            var command = new SetAsEditablePackCommand(service.Object);

            command.Execute(root, null);

            service.Verify(x => x.SetEditablePack(owner), Times.Once);
        }
    }
}
