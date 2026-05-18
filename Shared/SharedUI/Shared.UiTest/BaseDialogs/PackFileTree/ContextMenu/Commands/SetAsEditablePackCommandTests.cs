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
            var root = CreateRoot(owner);
            var command = new SetAsEditablePackCommand(CreatePackFileService(owner).Object);

            Assert.That(command.ShouldAdd(root), Is.True);
        }

        [Test]
        public void IsEnabled_ReturnsTrue()
        {
            var owner = CreateContainer();
            var root = CreateRoot(owner);
            var command = new SetAsEditablePackCommand(CreatePackFileService(owner).Object);

            Assert.That(command.IsEnabled(root), Is.True);
        }

        [Test]
        public void Execute_SetsEditablePack()
        {
            var owner = CreateContainer();
            var root = CreateRoot(owner);
            var service = CreatePackFileService(owner);
            service.Setup(x => x.GetEditablePack()).Returns((IPackFileContainer?)null);
            var command = new SetAsEditablePackCommand(service.Object);

            command.Execute(root);

            service.Verify(x => x.SetEditablePack(owner), Times.Once);
        }
    }
}
