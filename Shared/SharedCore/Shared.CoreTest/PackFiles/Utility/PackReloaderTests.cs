using Moq;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.PackFiles.Utility;
using Shared.Core.Services;
using Shared.Core.Settings;

namespace Shared.CoreTest.PackFiles.Utility
{
    [TestFixture]
    internal class PackReloaderTests
    {
        private string _dataDir = null!;
        private string _packPath = null!;
        private Mock<IPackFileService> _packFileService = null!;
        private Mock<IStandardDialogs> _dialogs = null!;
        private Mock<IPackFileContainer> _container = null!;
        private PackReloader _reloader = null!;

        [SetUp]
        public void Setup()
        {
            _dataDir = Path.Combine(Path.GetTempPath(), "PackReloader_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_dataDir);
            _packPath = Path.Combine(_dataDir, "mymod.pack");
            File.WriteAllBytes(_packPath, [1, 2, 3]);

            _packFileService = new Mock<IPackFileService>();
            _dialogs = new Mock<IStandardDialogs>();
            _container = new Mock<IPackFileContainer>();
            _container.SetupGet(x => x.Name).Returns("mymod");
            _container.SetupGet(x => x.SystemFilePath).Returns(_packPath);

            var backups = new PackBackupService(new ApplicationSettingsService(GameTypeEnum.Warhammer3));
            _reloader = new PackReloader(_packFileService.Object, backups, _dialogs.Object, new LocalizationManager());
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_dataDir))
                Directory.Delete(_dataDir, true);
        }

        private void UserAnswers(ShowMessageBoxResult answer) => _dialogs.Setup(x => x.ShowYesNoBox(It.IsAny<string>(), It.IsAny<string>())).Returns(answer);

        [Test]
        public void Reload_EditablePack_ReloadsThenBacksUpTheNewVersion()
        {
            Assert.That(_reloader.Reload(_container.Object), Is.True);

            _packFileService.Verify(x => x.ReloadPackContainer(_container.Object), Times.Once);
            Assert.That(File.Exists(PackBackupService.GetBackupPath(_packPath)), Is.True);
        }

        [Test]
        public void Reload_ReadOnlyPack_NoBackup()
        {
            _container.SetupGet(x => x.IsReadOnly).Returns(true);

            _reloader.Reload(_container.Object);

            _packFileService.Verify(x => x.ReloadPackContainer(_container.Object), Times.Once);
            Assert.That(File.Exists(PackBackupService.GetBackupPath(_packPath)), Is.False);
        }

        [Test]
        public void Reload_Fails_ExplainsAndReportsFalse()
        {
            _packFileService.Setup(x => x.ReloadPackContainer(_container.Object)).Throws(new InvalidDataException("bad index"));

            Assert.That(_reloader.Reload(_container.Object), Is.False);

            _dialogs.Verify(x => x.ShowDialogBox(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
            Assert.That(File.Exists(PackBackupService.GetBackupPath(_packPath)), Is.False);
        }

        [Test]
        public void ConfirmAndReload_NoUnsavedChanges_ReloadsWithoutAsking()
        {
            _reloader.ConfirmAndReload(_container.Object);

            _dialogs.Verify(x => x.ShowYesNoBox(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            _packFileService.Verify(x => x.ReloadPackContainer(_container.Object), Times.Once);
        }

        [Test]
        public void ConfirmAndReload_UnsavedChanges_UserDeclines_NotReloaded()
        {
            _packFileService.Setup(x => x.HasUnsavedChanges(_container.Object)).Returns(true);
            UserAnswers(ShowMessageBoxResult.Cancel);

            Assert.That(_reloader.ConfirmAndReload(_container.Object), Is.False);

            _packFileService.Verify(x => x.ReloadPackContainer(It.IsAny<IPackFileContainer>()), Times.Never);
        }

        [Test]
        public void AskToReloadChangedPack_UserAccepts_Reloaded()
        {
            UserAnswers(ShowMessageBoxResult.OK);

            Assert.That(_reloader.AskToReloadChangedPack(_container.Object), Is.True);

            _packFileService.Verify(x => x.ReloadPackContainer(_container.Object), Times.Once);
        }
    }
}
