using AssetEditor.Services;
using Moq;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.PackFiles.Utility;
using Shared.Core.Services;

namespace E2EVerification
{
    // Checking open packs for outside changes when AE regains focus
    public class PackChangedOnDiskCheckerTests
    {
        private string _dataDir = null!;
        private string _packPath = null!;
        private Mock<IPackFileService> _packFileService = null!;
        private Mock<IStandardDialogs> _dialogs = null!;
        private Mock<IPackFileContainer> _container = null!;
        private PackChangedOnDiskChecker _checker = null!;

        [SetUp]
        public void Setup()
        {
            _dataDir = Path.Combine(Path.GetTempPath(), "PackChecker_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_dataDir);
            _packPath = Path.Combine(_dataDir, "mymod.pack");
            File.WriteAllBytes(_packPath, [1, 2, 3]);

            _container = new Mock<IPackFileContainer>();
            _container.SetupGet(x => x.Name).Returns("mymod");
            _container.SetupGet(x => x.SystemFilePath).Returns(_packPath);
            _container.SetupGet(x => x.ContainerType).Returns(PackFileContainerType.Normal);

            _packFileService = new Mock<IPackFileService>();
            _packFileService.Setup(x => x.GetAllPackfileContainers()).Returns([_container.Object]);
            _packFileService.Setup(x => x.HasChangedOnDisk(_container.Object)).Returns(true);
            _dialogs = new Mock<IStandardDialogs>();

            var reloader = new PackReloader(_packFileService.Object, _dialogs.Object, new LocalizationManager());
            _checker = new PackChangedOnDiskChecker(_packFileService.Object, reloader);
        }

        [TearDown]
        public void TearDown()
        {
            _checker.Dispose();
            if (Directory.Exists(_dataDir))
                Directory.Delete(_dataDir, true);
        }

        private void UserAnswers(ShowMessageBoxResult answer) => _dialogs.Setup(x => x.ShowYesNoBox(It.IsAny<string>(), It.IsAny<string>())).Returns(answer);

        [Test]
        public void ReadOnlyPackChanged_ReloadedWithoutAsking()
        {
            _container.SetupGet(x => x.IsReadOnly).Returns(true);

            _checker.CheckAll();

            _dialogs.Verify(x => x.ShowYesNoBox(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            _packFileService.Verify(x => x.ReloadPackContainer(_container.Object), Times.Once);
        }

        [Test]
        public void EditablePackChanged_UserAccepts_Reloaded()
        {
            UserAnswers(ShowMessageBoxResult.OK);

            _checker.CheckAll();

            _packFileService.Verify(x => x.ReloadPackContainer(_container.Object), Times.Once);
        }

        [Test]
        public void EditablePackChanged_UserDeclines_NotAskedAgainUntilItChangesAgain()
        {
            UserAnswers(ShowMessageBoxResult.Cancel);

            _checker.CheckAll();
            _checker.CheckAll();
            _dialogs.Verify(x => x.ShowYesNoBox(It.IsAny<string>(), It.IsAny<string>()), Times.Once);

            File.SetLastWriteTimeUtc(_packPath, DateTime.UtcNow.AddMinutes(5));
            _checker.CheckAll();

            _dialogs.Verify(x => x.ShowYesNoBox(It.IsAny<string>(), It.IsAny<string>()), Times.Exactly(2));
            _packFileService.Verify(x => x.ReloadPackContainer(It.IsAny<IPackFileContainer>()), Times.Never);
        }

        [Test]
        public void PackUnchanged_NothingHappens()
        {
            _packFileService.Setup(x => x.HasChangedOnDisk(_container.Object)).Returns(false);

            _checker.CheckAll();

            _dialogs.VerifyNoOtherCalls();
            _packFileService.Verify(x => x.ReloadPackContainer(It.IsAny<IPackFileContainer>()), Times.Never);
        }

        [Test]
        public void PackDeletedFromDisk_NotReloaded()
        {
            File.Delete(_packPath);

            _checker.CheckAll();

            _dialogs.VerifyNoOtherCalls();
            _packFileService.Verify(x => x.ReloadPackContainer(It.IsAny<IPackFileContainer>()), Times.Never);
        }
    }
}
