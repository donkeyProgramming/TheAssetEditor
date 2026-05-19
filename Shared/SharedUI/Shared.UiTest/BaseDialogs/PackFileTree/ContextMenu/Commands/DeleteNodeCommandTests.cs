using Moq;
using Shared.Core.PackFiles;
using Shared.Core.Services;
using Shared.Ui.BaseDialogs.PackFileTree.ContextMenu.Commands;
using Shared.Ui.BaseDialogs.PackFileTree.Utility;

namespace Shared.UiTest.BaseDialogs.PackFileTree.ContextMenu.Commands
{
    [TestFixture]
    internal class DeleteNodeCommandTests : ContextMenuCommandTestBase
    {
        [Test]
        public void ShouldAdd_ReturnsTrueForFile()
        {
            var container = AddPackFiles(false, "modfile", "c:\\mymod.pack", ["rootfolder\\file.txt"]);
            var viewModel = PackFileBrowser();
            var node = TreeNodeHelper.FindNode(viewModel, container, "rootfolder\\file.txt");

            var command = new DeleteNodeCommand(_packFileService, new Mock<IStandardDialogs>().Object);

            Assert.That(command.ShouldAdd(node), Is.True);
        }

        [Test]
        public void IsEnabled_ReturnsTrue()
        {
            var container = AddPackFiles(false, "modfile", "c:\\mymod.pack", ["rootfolder\\file.txt"]);
            var viewModel = PackFileBrowser();
            var node = TreeNodeHelper.FindNode(viewModel, container, "rootfolder\\file.txt");

            var command = new DeleteNodeCommand(_packFileService, new Mock<IStandardDialogs>().Object);

            Assert.That(command.IsEnabled(node), Is.True);
        }

        [Test]
        public void Execute_DeletesFileAfterConfirmation()
        {
            // Arrange
            var container = AddPackFiles(false, "modfile", "c:\\mymod.pack", ["rootfolder\\file.txt"]);
            var viewModel = PackFileBrowser();
            var node = TreeNodeHelper.FindNode(viewModel, container, "rootfolder\\file.txt");

            var dialogs = new Mock<IStandardDialogs>();
            dialogs.Setup(x => x.ShowYesNoBox(It.IsAny<string>(), It.IsAny<string>())).Returns(ShowMessageBoxResult.OK);

            // Act
            var command = new DeleteNodeCommand(_packFileService, dialogs.Object);
            command.Execute(node);

            // Assert
            var packFile = container.FindFile("rootfolder\\file.txt");
            Assert.That(packFile, Is.Null);
        }
    }
}
