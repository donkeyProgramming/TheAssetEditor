using Test.TestingUtility.TestUtility;
using Moq;
using Shared.Core.Services;
using Shared.Ui.BaseDialogs.PackFileTree;
using Shared.Ui.BaseDialogs.PackFileTree.ContextMenu.Commands;
using Shared.Ui.BaseDialogs.PackFileTree.Utility;

namespace Shared.UiTest.BaseDialogs.PackFileTree.ContextMenu.Commands
{
    [TestFixture]
    internal class ClosePackContainerFileCommandTests : ContextMenuCommandTestBase
    {
        [Test]
        public void ShouldAdd_ReturnsTrueForRoot()
        {
            var container = AddPackFiles(false, "modfile", "c:\\mymod.pack", ["rootfolder\\file.txt"]);
            var viewModel = PackFileBrowser();
            var root = viewModel.Files.First();
            var fileNode = TreeNodeHelper.FindNode(viewModel, container, "rootfolder\\file.txt");

            var command = new ClosePackContainerFileCommand(_packFileService, new Mock<IStandardDialogs>().Object, MockScopedLogger.Create());

            Assert.That(command.ShouldAdd(root), Is.True);
            Assert.That(command.ShouldAdd(fileNode), Is.False);
        }

        [Test]
        public void IsEnabled_ReturnsTrue()
        {
            AddPackFiles(false, "modfile", "c:\\mymod.pack", ["rootfolder\\file.txt"]);
            var viewModel = PackFileBrowser();
            var root = viewModel.Files.First();

            var command = new ClosePackContainerFileCommand(_packFileService, new Mock<IStandardDialogs>().Object, MockScopedLogger.Create());

            Assert.That(command.IsEnabled(root), Is.True);
        }

        [Test]
        public void Execute_ConfirmsAndUnloadsPack()
        {
            // Arrange
            var container = AddPackFiles(false, "modfile", "c:\\mymod.pack", ["rootfolder\\file.txt"]);
            var viewModel = PackFileBrowser();
            var root = viewModel.Files.First();

            var dialogs = new Mock<IStandardDialogs>();
            dialogs.Setup(x => x.ShowYesNoBox(It.IsAny<string>(), It.IsAny<string>())).Returns(ShowMessageBoxResult.OK);

            // Act
            var command = new ClosePackContainerFileCommand(_packFileService, dialogs.Object, MockScopedLogger.Create());
            command.Configure(root);

            command.Execute();

            // Assert
            Assert.That(viewModel.Files.Count, Is.EqualTo(0));
        }
    }
}
