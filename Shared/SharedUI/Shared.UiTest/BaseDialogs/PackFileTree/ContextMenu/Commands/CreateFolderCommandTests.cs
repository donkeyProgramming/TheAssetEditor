using Moq;
using Shared.Core.Services;
using Shared.Ui.BaseDialogs.PackFileTree;
using Shared.Ui.BaseDialogs.PackFileTree.ContextMenu.Commands;

namespace Shared.UiTest.BaseDialogs.PackFileTree.ContextMenu.Commands
{
    [TestFixture]
    internal class CreateFolderCommandTests : ContextMenuCommandTestBase
    {
        [Test]
        public void ShouldAdd_ReturnsTrueForEditableRoot()
        {
            AddPackFiles(false, "modfile", "c:\\mymod.pack", ["rootfolder\\file.txt"]);
            var viewModel = PackFileBrowser();
            var root = viewModel.Files.First();

            var command = new CreateFolderCommand(_packFileService, new Mock<IStandardDialogs>().Object, new PackFileTreeMutationService());

            Assert.That(command.ShouldAdd(root), Is.True);
        }

        [Test]
        public void IsEnabled_ReturnsTrue()
        {
            AddPackFiles(false, "modfile", "c:\\mymod.pack", ["rootfolder\\file.txt"]);
            var viewModel = PackFileBrowser();
            var root = viewModel.Files.First();

            var command = new CreateFolderCommand(_packFileService, new Mock<IStandardDialogs>().Object, new PackFileTreeMutationService());

            Assert.That(command.IsEnabled(root), Is.True);
        }

        [Test]
        public void Execute_AddsFolderChild()
        {
            // Arrange
            AddPackFiles(false, "modfile", "c:\\mymod.pack", ["rootfolder\\file.txt"]);
            var viewModel = PackFileBrowser();
            var root = viewModel.Files.First();

            var dialogs = new Mock<IStandardDialogs>();
            dialogs.Setup(x => x.ShowFolderNameDialog(It.IsAny<IEnumerable<string>>(), It.IsAny<string>())).Returns("new_folder");

            // Act
            var command = new CreateFolderCommand(_packFileService, dialogs.Object, new PackFileTreeMutationService());
            command.Execute(root);

            // Assert
            Assert.That(root.Children.Any(x => x.Name == "new_folder"), Is.True);
        }
    }
}
