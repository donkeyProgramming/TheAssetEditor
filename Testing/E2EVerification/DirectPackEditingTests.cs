using AssetEditor.UiCommands;
using Moq;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.PackFiles.Utility;
using Shared.Core.Services;
using Shared.Core.Settings;

namespace E2EVerification
{
    // Opening a pack for direct editing: when it is backed up, and when it is added editable
    public class DirectPackEditingTests
    {
        private string _dataDir = null!;
        private string _packPath = null!;
        private string _backupPath = null!;
        private ApplicationSettingsService _settings = null!;
        private Mock<IPackFileService> _packFileService = null!;
        private Mock<IPackFileContainerLoader> _loader = null!;
        private Mock<IStandardDialogs> _dialogs = null!;

        [SetUp]
        public void Setup()
        {
            _dataDir = Path.Combine(Path.GetTempPath(), "DirectPackEditing_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_dataDir);
            _packPath = Path.Combine(_dataDir, "mymod.pack");
            File.WriteAllBytes(_packPath, [1, 2, 3]);
            _backupPath = PackBackupService.GetBackupPath(_packPath);

            _settings = new ApplicationSettingsService(GameTypeEnum.Warhammer3);
            _settings.CurrentSettings.EnableDirectPackEditing = true;
            _packFileService = new Mock<IPackFileService>();
            _loader = new Mock<IPackFileContainerLoader>();
            _dialogs = new Mock<IStandardDialogs>();
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_dataDir))
                Directory.Delete(_dataDir, true);
        }

        private OpenPackForEditingCommand CreateCommand() => new(_packFileService.Object, _loader.Object, new PackBackupService(_settings), _dialogs.Object, _settings, new LocalizationManager());

        private Mock<IPackFileContainer> LoaderReturns(bool isReadOnly)
        {
            var container = new Mock<IPackFileContainer>();
            container.SetupGet(x => x.IsReadOnly).Returns(isReadOnly);
            _loader.Setup(x => x.CreateFromPackFile(PackFileContainerType.Normal, _packPath, false)).Returns(container.Object);
            return container;
        }

        [Test]
        public void Open_EditablePack_BacksUpThenAddsItEditable()
        {
            var container = LoaderReturns(isReadOnly: false);

            CreateCommand().Open(_packPath);

            Assert.That(File.ReadAllBytes(_backupPath), Is.EqualTo(new byte[] { 1, 2, 3 }));
            _packFileService.Verify(x => x.AddContainer(container.Object, true), Times.Once);
        }

        [Test]
        public void Open_DirectEditingOff_DoesNothing()
        {
            _settings.CurrentSettings.EnableDirectPackEditing = false;

            CreateCommand().Open(_packPath);

            _loader.VerifyNoOtherCalls();
            Assert.That(File.Exists(_backupPath), Is.False);
        }

        [Test]
        public void Open_AlreadyOpen_KeepsTheExistingBackup()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_backupPath)!);
            File.WriteAllBytes(_backupPath, [7]);
            _packFileService.Setup(x => x.IsPackFileLoaded(_packPath)).Returns(true);

            CreateCommand().Open(_packPath);

            _loader.VerifyNoOtherCalls();
            Assert.That(File.ReadAllBytes(_backupPath), Is.EqualTo(new byte[] { 7 }));
        }

        [Test]
        public void Open_PackFailsToLoad_KeepsTheExistingBackup()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_backupPath)!);
            File.WriteAllBytes(_backupPath, [7]);
            _loader.Setup(x => x.CreateFromPackFile(PackFileContainerType.Normal, _packPath, false)).Throws(new InvalidDataException("unreadable index"));

            Assert.Throws<InvalidDataException>(() => CreateCommand().Open(_packPath));

            Assert.That(File.ReadAllBytes(_backupPath), Is.EqualTo(new byte[] { 7 }));
        }

        [Test]
        public void Open_LoaderMadeItReadOnly_AddsItWithoutBackup()
        {
            var container = LoaderReturns(isReadOnly: true);

            CreateCommand().Open(_packPath);

            Assert.That(File.Exists(_backupPath), Is.False);
            _packFileService.Verify(x => x.AddContainer(container.Object, false), Times.Once);
        }

        [Test]
        public void Open_BackupFails_UserDeclines_PackNotOpened()
        {
            var container = LoaderReturns(isReadOnly: false);
            _dialogs.Setup(x => x.ShowYesNoBox(It.IsAny<string>(), It.IsAny<string>())).Returns(ShowMessageBoxResult.Cancel);

            using (new FileStream(_packPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                CreateCommand().Open(_packPath);

            _packFileService.Verify(x => x.AddContainer(It.IsAny<IPackFileContainer>(), It.IsAny<bool>()), Times.Never);
        }

        [Test]
        public void Open_BackupsOff_AddsItWithoutBackup()
        {
            _settings.CurrentSettings.BackupPacksOnOpen = false;
            var container = LoaderReturns(isReadOnly: false);

            CreateCommand().Open(_packPath);

            Assert.That(File.Exists(_backupPath), Is.False);
            _packFileService.Verify(x => x.AddContainer(container.Object, true), Times.Once);
        }
    }
}
