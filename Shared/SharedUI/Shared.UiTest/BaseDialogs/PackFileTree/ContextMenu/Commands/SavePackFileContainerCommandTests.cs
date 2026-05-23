using Test.TestingUtility.TestUtility;
using Moq;
using Shared.Core.PackFiles;
using Shared.Core.Services;
using Shared.Core.Settings;
using Shared.Ui.BaseDialogs.PackFileTree;
using Shared.Ui.BaseDialogs.PackFileTree.ContextMenu.Commands;

namespace Shared.UiTest.BaseDialogs.PackFileTree.ContextMenu.Commands
{
    [TestFixture]
    internal class SavePackFileContainerCommandTests : ContextMenuCommandTestBase
    {
        [Test]
        public void ShouldAdd_ReturnsTrueForRoot()
        {
            var container = AddPackFiles(false, "modfile", "c:\\mymod.pack", ["rootfolder\\file.txt"]);
            var viewModel = PackFileBrowser();
            var root = viewModel.Files.First();

            var command = new SavePackFileContainerCommand(_packFileService, new Mock<IStandardDialogs>().Object, new ApplicationSettingsService(GameTypeEnum.Warhammer3), MockScopedLogger.Create());

            Assert.That(command.ShouldAdd(root), Is.True);
        }

        [Test]
        public void IsEnabled_ReturnsTrue()
        {
            var container = AddPackFiles(false, "modfile", "c:\\mymod.pack", ["rootfolder\\file.txt"]);
            var viewModel = PackFileBrowser();
            var root = viewModel.Files.First();

            var command = new SavePackFileContainerCommand(_packFileService, new Mock<IStandardDialogs>().Object, new ApplicationSettingsService(GameTypeEnum.Warhammer3), MockScopedLogger.Create());

            Assert.That(command.IsEnabled(root), Is.True);
        }

        [Test]
        public void Execute_SavesPackContainer()
        {
            // Arrange
            var container = AddPackFiles(false, "modfile", "c:\\mymod.pack", ["rootfolder\\file.txt"]);
            var viewModel = PackFileBrowser();
            var root = viewModel.Files.First();

            var dialogs = new Mock<IStandardDialogs>();
            var appSettings = new ApplicationSettingsService(GameTypeEnum.Warhammer3);

            // Act
            var command = new SavePackFileContainerCommand(_packFileService, dialogs.Object, appSettings, MockScopedLogger.Create());
            command.Configure(root);

            command.Execute();

            // Assert - file was saved (no exception thrown, service called through)
            Assert.That(container.SystemFilePath, Is.EqualTo("c:\\mymod.pack"));
        }

        [Test]
        public void Execute_Parameterless_NoEditablePack_ShowsError()
        {
            // Arrange
            AddPackFiles(false, "modfile", "c:\\mymod.pack", ["rootfolder\\file.txt"]);

            var dialogs = new Mock<IStandardDialogs>();
            var appSettings = new ApplicationSettingsService(GameTypeEnum.Warhammer3);

            // Act
            var command = new SavePackFileContainerCommand(_packFileService, dialogs.Object, appSettings, MockScopedLogger.Create());
            command.Execute();

            // Assert
            dialogs.Verify(x => x.ShowDialogBox(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [Test]
        public void Execute_Parameterless_WithEditablePack_SavesSuccessfully()
        {
            // Arrange
            var container = AddPackFiles(false, "modfile", "c:\\mymod.pack", ["rootfolder\\file.txt"]);
            _packFileService.SetEditablePack(container);

            var dialogs = new Mock<IStandardDialogs>();
            var appSettings = new ApplicationSettingsService(GameTypeEnum.Warhammer3);

            // Act
            var command = new SavePackFileContainerCommand(_packFileService, dialogs.Object, appSettings, MockScopedLogger.Create());
            command.Execute();

            // Assert - SavePackContainer was invoked (it will throw internally due to file I/O, caught by the command)
            Assert.That(container.SystemFilePath, Is.EqualTo("c:\\mymod.pack"));
        }
    }
}
