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
    internal class DeleteNodeCommandTests : ContextMenuCommandTestBase
    {
        [Test]
        public void ShouldAdd_IsEnabled_Execute()
        {
            var owner = CreateContainer();
            var root = new TreeNode("root", NodeType.Root, owner, null);
            var file = new TreeNode("file.txt", NodeType.File, owner, root, PackFile.CreateFromASCII("file.txt", "a"));
            root.AddChild(file);

            var service = new Mock<IPackFileService>();
            var dialogs = new Mock<IStandardDialogs>();
            dialogs.Setup(x => x.ShowYesNoBox(It.IsAny<string>(), It.IsAny<string>())).Returns(ShowMessageBoxResult.OK);

            var command = new DeleteNodeCommand(service.Object, dialogs.Object);

            Assert.That(command.ShouldAdd(file), Is.True);
            Assert.That(command.IsEnabled(file), Is.True);

            command.Execute(file);

            service.Verify(x => x.DeleteFile(owner, file.Item!), Times.Once);
        }
    }
}
