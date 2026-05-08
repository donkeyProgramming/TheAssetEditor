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
    internal class CopyToEditablePackCommandTests : ContextMenuCommandTestBase
    {
        [Test]
        public void ShouldAdd_IsEnabled_Execute()
        {
            var source = CreateContainer(name: "source");
            var target = CreateContainer(name: "target");
            var service = new Mock<IPackFileService>();
            service.Setup(x => x.GetEditablePack()).Returns(target);

            var dialogs = new Mock<IStandardDialogs>();
            var waitCursor = new Mock<IWaitCursor>();
            dialogs.Setup(x => x.ShowWaitCursor()).Returns(waitCursor.Object);

            var root = new TreeNode("root", NodeType.Root, source, null);
            var folder = new TreeNode("folder", NodeType.Directory, source, root);
            root.AddChild(folder);
            var file = new TreeNode("file.txt", NodeType.File, source, folder, PackFile.CreateFromASCII("file.txt", "a"));
            folder.AddChild(file);

            var command = new CopyToEditablePackCommand(service.Object, dialogs.Object);

            Assert.That(command.ShouldAdd(folder), Is.True);
            Assert.That(command.IsEnabled(folder), Is.True);

            command.Execute(folder);

            service.Verify(x => x.CopyFileFromOtherPackFile(source, It.IsAny<string>(), target), Times.Once);
        }
    }
}
