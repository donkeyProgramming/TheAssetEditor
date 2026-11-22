using AssetEditor.UiCommands;
using Moq;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.PackFiles.Utility;
using Shared.Core.Services;
using Shared.Core.Settings;

namespace E2EVerification
{
    // Opening a pack for direct editing: when it is added editable. Opening writes nothing to disk;
    // the pack's backup is kept by its first save.
    public class DirectPackEditingTests
    {
        private string _dataDir = null!;
        private string _packPath = null!;
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

        private OpenPackForEditingCommand CreateCommand() => new(_packFileService.Object, _loader.Object, _dialogs.Object, _settings, new LocalizationManager());

        private Mock<IPackFileContainer> LoaderReturns(bool isReadOnly)
        {
            var container = new Mock<IPackFileContainer>();
            container.SetupGet(x => x.IsReadOnly).Returns(isReadOnly);
            _loader.Setup(x => x.CreateFromPackFile(PackFileContainerType.Normal, _packPath, false)).Returns(container.Object);
            return container;
        }

        [Test]
        public void Open_EditablePack_AddsItEditable_WritesNothing()
        {
            var container = LoaderReturns(isReadOnly: false);

            CreateCommand().Open(_packPath);

            _packFileService.Verify(x => x.AddContainer(container.Object, true), Times.Once);
            Assert.That(Directory.GetFileSystemEntries(_dataDir), Is.EqualTo(new[] { _packPath }));
        }

        [Test]
        public void Open_DirectEditingOff_DoesNothing()
        {
            _settings.CurrentSettings.EnableDirectPackEditing = false;

            CreateCommand().Open(_packPath);

            _loader.VerifyNoOtherCalls();
            _packFileService.Verify(x => x.AddContainer(It.IsAny<IPackFileContainer>(), It.IsAny<bool>()), Times.Never);
        }

        [Test]
        public void Open_AlreadyOpen_NotLoadedAgain()
        {
            _packFileService.Setup(x => x.IsPackFileLoaded(_packPath)).Returns(true);

            CreateCommand().Open(_packPath);

            _loader.VerifyNoOtherCalls();
            _dialogs.Verify(x => x.ShowDialogBox(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [Test]
        public void Open_LoaderMadeItReadOnly_AddsItReadOnly()
        {
            var container = LoaderReturns(isReadOnly: true);

            CreateCommand().Open(_packPath);

            _packFileService.Verify(x => x.AddContainer(container.Object, false), Times.Once);
        }
    }
}
