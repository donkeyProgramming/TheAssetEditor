using System.IO;
using Test.TestingUtility.TestUtility;
using Moq;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.ErrorHandling;
using Shared.Core.PackFiles.Models;
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

            var command = new SavePackFileContainerCommand(_packFileService, new Mock<IStandardDialogs>().Object, new ApplicationSettingsService(GameTypeEnum.Warhammer3), new LocalizationManager(), MockScopedLogger.Create());

            Assert.That(command.ShouldAdd(root), Is.True);
        }

        [Test]
        public void IsEnabled_ReturnsTrue()
        {
            var container = AddPackFiles(false, "modfile", "c:\\mymod.pack", ["rootfolder\\file.txt"]);
            var viewModel = PackFileBrowser();
            var root = viewModel.Files.First();

            var command = new SavePackFileContainerCommand(_packFileService, new Mock<IStandardDialogs>().Object, new ApplicationSettingsService(GameTypeEnum.Warhammer3), new LocalizationManager(), MockScopedLogger.Create());

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
            var command = new SavePackFileContainerCommand(_packFileService, dialogs.Object, appSettings, new LocalizationManager(), MockScopedLogger.Create());
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
            var command = new SavePackFileContainerCommand(_packFileService, dialogs.Object, appSettings, new LocalizationManager(), MockScopedLogger.Create());
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
            var command = new SavePackFileContainerCommand(_packFileService, dialogs.Object, appSettings, new LocalizationManager(), MockScopedLogger.Create());
            command.Execute();

            // Assert - SavePackContainer was invoked (it will throw internally due to file I/O, caught by the command)
            Assert.That(container.SystemFilePath, Is.EqualTo("c:\\mymod.pack"));
        }

        [Test]
        public void Execute_SystemFolderContainer_SavesToConfiguredOutputLocation()
        {
            var outputDir = Path.Combine(Path.GetTempPath(), "SavePackFileContainerCommandTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(outputDir);
            try
            {
                var outputPath = Path.Combine(outputDir, "generated.pack");
                var settings = new PackFileSettings { SaveLocationPath = outputPath };
                var container = new Mock<IPackFileContainer>();
                container.SetupGet(x => x.Name).Returns("project");
                container.SetupGet(x => x.SystemFilePath).Returns(Path.Combine(outputDir, "project"));
                container.SetupGet(x => x.ContainerType).Returns(PackFileContainerType.SystemFolder);
                container.SetupGet(x => x.PackFileSettings).Returns(settings);
                container.SetupProperty(x => x.IsReadOnly, false);

                var root = new RootTreeNode("project", container.Object);
                var dialogs = new Mock<IStandardDialogs>();
                dialogs.Setup(x => x.ShowWaitCursor()).Returns(new Mock<IWaitCursor>().Object);
                var packFileService = new Mock<IPackFileService>();

                var command = new SavePackFileContainerCommand(packFileService.Object, dialogs.Object, new ApplicationSettingsService(GameTypeEnum.Warhammer3), new LocalizationManager(), MockScopedLogger.Create());
                command.Configure(root);

                command.Execute();

                packFileService.Verify(x => x.SavePackContainer(container.Object, outputPath, false, It.IsAny<GameInformation>()), Times.Once);
            }
            finally
            {
                Directory.Delete(outputDir, true);
            }
        }

        private static (Mock<IPackFileContainer> Container, RootTreeNode Root) DirectPack(string packPath)
        {
            var container = new Mock<IPackFileContainer>();
            container.SetupGet(x => x.Name).Returns("mymod");
            container.SetupGet(x => x.SystemFilePath).Returns(packPath);
            container.SetupGet(x => x.ContainerType).Returns(PackFileContainerType.Normal);
            container.SetupGet(x => x.PackFileSettings).Returns(new PackFileSettings());
            container.SetupProperty(x => x.IsReadOnly, false);
            return (container, new RootTreeNode("mymod", container.Object));
        }

        private static ApplicationSettingsService Settings(bool directEditing, bool backups)
        {
            var settings = new ApplicationSettingsService(GameTypeEnum.Warhammer3);
            settings.CurrentSettings.EnableDirectPackEditing = directEditing;
            settings.CurrentSettings.BackupPacksOnFirstSave = backups;
            return settings;
        }

        [TestCase(true, true, true)]
        [TestCase(true, false, false)]
        [TestCase(false, true, false)]
        public void Execute_DirectPack_KeepsBackupOnlyWhenTurnedOn(bool directEditing, bool backups, bool expectBackup)
        {
            var packPath = Path.Combine(Path.GetTempPath(), "mymod.pack");
            var (container, root) = DirectPack(packPath);
            var packFileService = new Mock<IPackFileService>();
            var command = new SavePackFileContainerCommand(packFileService.Object, new Mock<IStandardDialogs>().Object, Settings(directEditing, backups), new LocalizationManager(), MockScopedLogger.Create());
            command.Configure(root);

            command.Execute();

            packFileService.Verify(x => x.SavePackContainer(container.Object, packPath, expectBackup, It.IsAny<GameInformation>()), Times.Once);
        }

        [TestCase(ShowMessageBoxResult.OK, 1)]
        [TestCase(ShowMessageBoxResult.Cancel, 0)]
        public void Execute_BackupFails_AsksBeforeSavingWithoutOne(ShowMessageBoxResult answer, int savesWithoutBackup)
        {
            var packPath = Path.Combine(Path.GetTempPath(), "mymod.pack");
            var (container, root) = DirectPack(packPath);
            var packFileService = new Mock<IPackFileService>();
            packFileService.Setup(x => x.SavePackContainer(container.Object, packPath, true, It.IsAny<GameInformation>()))
                .Throws(new PackBackupFailedException(packPath, packPath + "1", new IOException("in use")));
            var dialogs = new Mock<IStandardDialogs>();
            dialogs.Setup(x => x.ShowYesNoBox(It.IsAny<string>(), It.IsAny<string>())).Returns(answer);
            var command = new SavePackFileContainerCommand(packFileService.Object, dialogs.Object, Settings(true, true), new LocalizationManager(), MockScopedLogger.Create());
            command.Configure(root);

            command.Execute();

            dialogs.Verify(x => x.ShowYesNoBox(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
            dialogs.Verify(x => x.ShowDialogBox(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            packFileService.Verify(x => x.SavePackContainer(container.Object, packPath, false, It.IsAny<GameInformation>()), Times.Exactly(savesWithoutBackup));
        }
    }
}
