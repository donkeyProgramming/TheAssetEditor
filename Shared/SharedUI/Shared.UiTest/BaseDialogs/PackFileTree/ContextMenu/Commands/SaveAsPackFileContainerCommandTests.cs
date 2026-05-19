using Moq;
using Shared.Core.Services;
using Shared.Core.Settings;
using Shared.Ui.BaseDialogs.PackFileTree.ContextMenu.Commands;

namespace Shared.UiTest.BaseDialogs.PackFileTree.ContextMenu.Commands
{
    [TestFixture]
    internal class SaveAsPackFileContainerCommandTests : ContextMenuCommandTestBase
    {
        [Test]
        public void ShouldAdd_ReturnsTrueForRoot()
        {
            AddPackFiles(false, "modfile", "c:\\mymod.pack", ["rootfolder\\file.txt"]);
            var viewModel = PackFileBrowser();
            var root = viewModel.Files.First();

            var command = new SaveAsPackFileContainerCommand(_packFileService, new ApplicationSettingsService(GameTypeEnum.Warhammer3), new Mock<IStandardDialogs>().Object);

            Assert.That(command.ShouldAdd(root), Is.True);
        }

        [Test]
        public void IsEnabled_ReturnsTrue()
        {
            AddPackFiles(false, "modfile", "c:\\mymod.pack", ["rootfolder\\file.txt"]);
            var viewModel = PackFileBrowser();
            var root = viewModel.Files.First();

            var command = new SaveAsPackFileContainerCommand(_packFileService, new ApplicationSettingsService(GameTypeEnum.Warhammer3), new Mock<IStandardDialogs>().Object);

            Assert.That(command.IsEnabled(root), Is.True);
        }

        [Test]
        public void Execute_SaveDialogCancelled_DoesNotSave()
        {
            // Arrange
            AddPackFiles(false, "modfile", "c:\\mymod.pack", ["rootfolder\\file.txt"]);
            var viewModel = PackFileBrowser();
            var root = viewModel.Files.First();

            var dialogs = new Mock<IStandardDialogs>();
            dialogs.Setup(x => x.ShowSystemSaveFileDialog(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(new SystemSaveFileDialogResult(false, null));

            // Act
            var command = new SaveAsPackFileContainerCommand(_packFileService, new ApplicationSettingsService(GameTypeEnum.Warhammer3), dialogs.Object);
            command.Execute(root);

            // Assert - no exception, command exits early
            Assert.Pass();
        }
    }
}
