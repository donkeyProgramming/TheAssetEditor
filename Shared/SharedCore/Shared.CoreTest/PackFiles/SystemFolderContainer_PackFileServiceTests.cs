using Moq;
using Shared.Core.Events;
using Shared.Core.Events.Global;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.PackFiles.Models.Containers;
using Shared.Core.PackFiles.Models.FileSources;
using Shared.Core.Services;
using Shared.Core.Settings;

namespace Shared.CoreTest.PackFiles
{
    [TestFixture]
    internal class SystemFolderContainer_PackFileServiceTests
    {
        private string _tempDir = null!;
        private Mock<IGlobalEventHub> _eventHub = null!;
        private Mock<IFileSystemAccess> _fileSystemAccess = null!;
        private Mock<IFileSystemWatcher> _mockWatcher = null!;
        private PackFileService _pfs = null!;

        [SetUp]
        public void Setup()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "SysFolderPFS_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDir);

            // Seed a file
            var seedPath = Path.Combine(_tempDir, "data", "file.txt");
            Directory.CreateDirectory(Path.GetDirectoryName(seedPath)!);
            File.WriteAllText(seedPath, "hello");

            _fileSystemAccess = new Mock<IFileSystemAccess>();
            _fileSystemAccess.Setup(x => x.DirectoryExists(It.IsAny<string>()))
                .Returns((string p) => Directory.Exists(p));
            _fileSystemAccess.Setup(x => x.DirectoryGetFiles(_tempDir, "*.*", SearchOption.AllDirectories))
                .Returns([seedPath]);
            _fileSystemAccess.Setup(x => x.DirectoryCreateDirectory(It.IsAny<string>()))
                .Callback((string p) => Directory.CreateDirectory(p));
            _fileSystemAccess.Setup(x => x.FileWriteAllBytes(It.IsAny<string>(), It.IsAny<byte[]>()))
                .Callback((string p, byte[] d) => { Directory.CreateDirectory(Path.GetDirectoryName(p)!); File.WriteAllBytes(p, d); });
            _fileSystemAccess.Setup(x => x.FileExists(It.IsAny<string>()))
                .Returns((string p) => File.Exists(p));
            _fileSystemAccess.Setup(x => x.FileDelete(It.IsAny<string>()))
                .Callback((string p) => File.Delete(p));
            _fileSystemAccess.Setup(x => x.FileMove(It.IsAny<string>(), It.IsAny<string>()))
                .Callback((string s, string d) => { Directory.CreateDirectory(Path.GetDirectoryName(d)!); File.Move(s, d); });
            _fileSystemAccess.Setup(x => x.DirectoryMove(It.IsAny<string>(), It.IsAny<string>()))
                .Callback((string s, string d) => Directory.Move(s, d));
            _fileSystemAccess.Setup(x => x.DirectoryDelete(It.IsAny<string>(), It.IsAny<bool>()))
                .Callback((string p, bool r) => Directory.Delete(p, r));

            _mockWatcher = new Mock<IFileSystemWatcher>();
            _eventHub = new Mock<IGlobalEventHub>();

            _pfs = new PackFileService(_eventHub.Object);
            _pfs.MessageBoxProvider = new Mock<ISimpleMessageBox>().Object;
            _pfs.EnforceGameFilesMustBeLoaded = false;
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, true);
        }

        private SystemFolderContainer CreateContainer()
        {
            return new SystemFolderContainer(_tempDir, _fileSystemAccess.Object, _mockWatcher.Object, _eventHub.Object);
        }

        [Test]
        public void AddContainer_SystemFolder_RegistersSuccessfully()
        {
            var container = CreateContainer();

            var result = _pfs.AddContainer(container);

            Assert.That(result, Is.Not.Null);
            Assert.That(_pfs.GetAllPackfileContainers(), Does.Contain(container));
            _eventHub.Verify(x => x.PublishGlobalEvent(It.Is<PackFileContainerAddedEvent>(e => e.Container == container)), Times.Once);
        }

        [Test]
        public void AddContainer_DuplicateFolderPath_Rejected()
        {
            var container1 = CreateContainer();
            _pfs.AddContainer(container1);

            var container2 = CreateContainer();
            var result = _pfs.AddContainer(container2);

            Assert.That(result, Is.Null);
            Assert.That(_pfs.GetAllPackfileContainers().Count, Is.EqualTo(1));
        }

        [Test]
        public void UnloadContainer_DisposesWatcher()
        {
            var container = CreateContainer();
            _pfs.AddContainer(container);

            _pfs.UnloadPackContainer(container);

            _mockWatcher.VerifySet(w => w.EnableRaisingEvents = false);
            _mockWatcher.Verify(w => w.Dispose(), Times.Once);
            Assert.That(_pfs.GetAllPackfileContainers(), Does.Not.Contain(container));
        }

        [Test]
        public void SetEditablePack_SystemFolder_Works()
        {
            var container = CreateContainer();
            _pfs.AddContainer(container);

            _pfs.SetEditablePack(container);

            Assert.That(_pfs.GetEditablePack(), Is.EqualTo(container));
        }

        [Test]
        public void AddFilesToPack_SystemFolder_WritesThrough()
        {
            var container = CreateContainer();
            _pfs.AddContainer(container);

            var newFile = new PackFile("added.txt", new MemorySource("new content"u8.ToArray()));
            var entries = new List<NewPackFileEntry> { new("newdir", newFile) };

            _pfs.AddFilesToPack(container, entries);

            Assert.That(container.ContainsFile(@"newdir\added.txt"), Is.True);
            Assert.That(File.Exists(Path.Combine(_tempDir, "newdir", "added.txt")), Is.True);
        }

        [Test]
        public void DeleteFile_SystemFolder_DeletesFromDisk()
        {
            var container = CreateContainer();
            _pfs.AddContainer(container);
            var file = container.FindFile(@"data\file.txt");
            Assert.That(file, Is.Not.Null);

            _pfs.DeleteFile(container, file!);

            Assert.That(container.ContainsFile(@"data\file.txt"), Is.False);
            Assert.That(File.Exists(Path.Combine(_tempDir, "data", "file.txt")), Is.False);
        }

        [Test]
        public void SavePackContainer_SystemFolder_GeneratesPackFile()
        {
            var container = CreateContainer();
            _pfs.AddContainer(container);

            var outputPath = Path.Combine(_tempDir, "output.pack");
            var gameInfo = GameInformationDatabase.GetGameById(GameTypeEnum.Warhammer3);

            _pfs.SavePackContainer(container, outputPath, false, gameInfo);

            Assert.That(File.Exists(outputPath), Is.True);
            // Container should remain active
            Assert.That(container.GetFileCount(), Is.EqualTo(1));
        }

        [Test]
        public void FindFile_AcrossContainers_FindsInSystemFolder()
        {
            var container = CreateContainer();
            _pfs.AddContainer(container);

            var result = _pfs.FindFile(@"data\file.txt");

            Assert.That(result, Is.Not.Null);
            Assert.That(result!.Name, Is.EqualTo("file.txt"));
        }
    }
}
