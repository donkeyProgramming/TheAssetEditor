using System.Threading;
using Moq;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.Services;
using Shared.Ui.BaseDialogs.PackFileTree;
using Shared.Ui.BaseDialogs.PackFileTree.ContextMenu.Commands;

namespace Shared.UiTest.ContextMenuCommands
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    internal class ClosePackContainerFileCommandTests : ContextMenuCommandTestBase
    {
        [Test]
        public void ShouldAdd_IsEnabled_Execute()
        {
            var owner = CreateContainer();
            var root = new TreeNode("root", NodeType.Root, owner, null);
            var service = new Mock<IPackFileService>();
            var dialogs = new Mock<IStandardDialogs>();
            dialogs.Setup(x => x.ShowYesNoBox(It.IsAny<string>(), It.IsAny<string>())).Returns(ShowMessageBoxResult.OK);

            var command = new ClosePackContainerFileCommand(service.Object, dialogs.Object);

            Assert.That(command.ShouldAdd(root), Is.True);
            Assert.That(command.ShouldAdd(new TreeNode("file.txt", NodeType.File, owner, root, PackFile.CreateFromASCII("file.txt", "a"))), Is.False);
            Assert.That(command.IsEnabled(root), Is.True);

            command.Execute(root);

            service.Verify(x => x.UnloadPackContainer(owner), Times.Once);
        }
    }
}
