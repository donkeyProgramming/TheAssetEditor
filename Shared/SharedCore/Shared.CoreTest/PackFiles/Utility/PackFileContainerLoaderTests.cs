using Moq;
using Shared.Core.Misc;
using Shared.Core.PackFiles.Utility;
using Shared.Core.Services;
using Shared.Core.Settings;
using Test.TestingUtility.TestUtility;

namespace Shared.CoreTest.PackFiles.Utility
{
    internal class PackFileContainerLoaderTests
    {
        private string _tempGameDir;
        private Mock<IStandardDialogs> _dialogs;
        private Mock<IWaitCursor> _waitCursor;
        private LocalizationManager _localizationManager;
        private ApplicationSettingsService _settingsService;
        private InMemoryPackFileContainerCacheHelper _cacheHelper;

        [SetUp]
        public void Setup()
        {
            _tempGameDir = Path.Combine(Path.GetTempPath(), "AE_LoaderTest_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempGameDir);

            // Copy the Karl pack file into the temp game dir and touch it to get a unique fingerprint
            var karlPackSource = PathHelper.GetDataFile("Karl_and_celestialgeneral.pack");
            var destPath = Path.Combine(_tempGameDir, "Karl_and_celestialgeneral.pack");
            File.Copy(karlPackSource, destPath);
            File.SetLastWriteTimeUtc(destPath, DateTime.UtcNow);

            _dialogs = new Mock<IStandardDialogs>();
            _waitCursor = new Mock<IWaitCursor>();
            _dialogs.Setup(d => d.ShowWaitCursor()).Returns(_waitCursor.Object);

            _localizationManager = new LocalizationManager();

            _settingsService = new ApplicationSettingsService(GameTypeEnum.Warhammer3);
            _settingsService.CurrentSettings.GameDirectories.Add(new ApplicationSettings.GamePathPair(GameTypeEnum.Warhammer3, _tempGameDir));

            _cacheHelper = new InMemoryPackFileContainerCacheHelper();
        }

        [TearDown]
        public void TearDown()
        {
            _cacheHelper.Dispose();

            if (Directory.Exists(_tempGameDir))
                Directory.Delete(_tempGameDir, true);
        }

        private PackFileContainerLoader CreateLoader() => new PackFileContainerLoader(_settingsService, _dialogs.Object, _localizationManager, _cacheHelper);

        [Test]
        public void LoadAllCaFiles_MissingGameDirectory_ShowsErrorAndSkipsBuild()
        {
            _settingsService.CurrentSettings.GameDirectories.Clear();
            var loader = CreateLoader();

            var result = loader.CreateFromGameEnum(PackFileContainerType.Cached, GameTypeEnum.Warhammer3);

            Assert.That(result, Is.Null);
        }

        [Test]
        public void LoadAllCaFiles_NoCacheExists_ShowsNotFoundDialogAndWaitCursor()
        {
            var loader = CreateLoader();

            var result = loader.CreateFromGameEnum(PackFileContainerType.Cached, GameTypeEnum.Warhammer3);

            Assert.That(result, Is.Not.Null);

            // Single combined dialog: reason + building message
            _dialogs.Verify(d => d.ShowDialogBox(It.IsAny<string>(), It.IsAny<string>()), Times.Once);

            // Should show wait cursor
            _dialogs.Verify(d => d.ShowWaitCursor(), Times.Once);
            _waitCursor.Verify(w => w.Dispose(), Times.Once);
        }

        [Test]
        public void LoadAllCaFiles_CacheExists_NoDialogShown()
        {
            var loader = CreateLoader();

            // First call builds the cache
            var firstResult = loader.CreateFromGameEnum(PackFileContainerType.Cached, GameTypeEnum.Warhammer3);
            Assert.That(firstResult, Is.Not.Null);

            _dialogs.Invocations.Clear();

            // Second call should use cache - no dialogs
            var secondResult = loader.CreateFromGameEnum(PackFileContainerType.Cached, GameTypeEnum.Warhammer3);
            Assert.That(secondResult, Is.Not.Null);

            _dialogs.Verify(d => d.ShowDialogBox(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            _dialogs.Verify(d => d.ShowWaitCursor(), Times.Never);
        }

        [Test]
        public void LoadAllCaFiles_CacheCorrupted_ShowsCorruptedDialog()
        {
            var loader = CreateLoader();

            // First call builds the cache
            var firstResult = loader.CreateFromGameEnum(PackFileContainerType.Cached, GameTypeEnum.Warhammer3);
            Assert.That(firstResult, Is.Not.Null);

            // Corrupt the cache via the in-memory helper
            var packFiles = Directory.GetFiles(_tempGameDir, "*.pack").ToList();
            var fingerprint = _cacheHelper.ComputeFingerprint(packFiles);
            var cacheFilePath = _cacheHelper.GetCacheFilePath("All Game Packs - Warhammer III", fingerprint);
            _cacheHelper.CorruptCache(cacheFilePath);

            _dialogs.Invocations.Clear();

            // Load again — should detect corruption
            var secondResult = loader.CreateFromGameEnum(PackFileContainerType.Cached, GameTypeEnum.Warhammer3);
            Assert.That(secondResult, Is.Not.Null);

            // Single combined dialog: reason + building message
            _dialogs.Verify(d => d.ShowDialogBox(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
            _dialogs.Verify(d => d.ShowWaitCursor(), Times.Once);
        }

        [Test]
        public void LoadAllCaFiles_DataChanged_ShowsDialogsAndRebuildsCache()
        {
            var loader = CreateLoader();

            // First call builds the cache
            var firstResult = loader.CreateFromGameEnum(PackFileContainerType.Cached, GameTypeEnum.Warhammer3);
            Assert.That(firstResult, Is.Not.Null);

            // Add a new pack file to the game dir to change the fingerprint
            var newPackPath = Path.Combine(_tempGameDir, "extra.pack");
            File.Copy(Path.Combine(_tempGameDir, "Karl_and_celestialgeneral.pack"), newPackPath);

            _dialogs.Invocations.Clear();

            // Load again — fingerprint changed, new fingerprint has no cache file
            var secondResult = loader.CreateFromGameEnum(PackFileContainerType.Cached, GameTypeEnum.Warhammer3);
            Assert.That(secondResult, Is.Not.Null);

            // Single combined dialog: reason + building message
            _dialogs.Verify(d => d.ShowDialogBox(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
            _dialogs.Verify(d => d.ShowWaitCursor(), Times.Once);
        }

        [Test]
        public void LoadAllCaFiles_WaitCursorActive_DuringBuild()
        {
            var waitCursorCreated = false;
            var waitCursorDisposed = false;
            var dialogShownBeforeWaitCursor = false;

            _dialogs.Setup(d => d.ShowDialogBox(It.IsAny<string>(), It.IsAny<string>()))
                .Callback(() =>
                {
                    // Dialog is shown BEFORE wait cursor is created
                    if (!waitCursorCreated)
                        dialogShownBeforeWaitCursor = true;
                });

            _dialogs.Setup(d => d.ShowWaitCursor())
                .Returns(() =>
                {
                    waitCursorCreated = true;
                    return _waitCursor.Object;
                });

            _waitCursor.Setup(w => w.Dispose())
                .Callback(() => waitCursorDisposed = true);

            var loader = CreateLoader();
            loader.CreateFromGameEnum(PackFileContainerType.Cached, GameTypeEnum.Warhammer3);

            Assert.That(dialogShownBeforeWaitCursor, Is.True, "Dialog should be shown before wait cursor starts");
            Assert.That(waitCursorCreated, Is.True, "Wait cursor should have been created");
            Assert.That(waitCursorDisposed, Is.True, "Wait cursor should have been disposed");
        }
    }
}
