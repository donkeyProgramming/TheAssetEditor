using Moq;
using Shared.Core.Events;
using Shared.Core.PackFiles.Models.Containers;
using Shared.Core.Services;

namespace Shared.CoreTest.PackFiles.Models.Containers
{
    [TestFixture]
    internal class SystemFolderContainerTests_Dispose
    {
        private string _tempDir = null!;
        private Mock<IFileSystemAccess> _fileSystemAccess = null!;
        private Mock<IFileSystemWatcher> _mockWatcher = null!;
        private Mock<IGlobalEventHub> _mockEventHub = null!;

        [SetUp]
        public void Setup()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "SysFolderDispose_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDir);

            var seedPath = Path.Combine(_tempDir, "file.txt");
            File.WriteAllText(seedPath, "content");

            _fileSystemAccess = new Mock<IFileSystemAccess>();
            _fileSystemAccess.Setup(x => x.DirectoryExists(_tempDir)).Returns(true);
            _fileSystemAccess.Setup(x => x.DirectoryGetFiles(_tempDir, "*.*", SearchOption.AllDirectories))
                .Returns([seedPath]);

            _mockWatcher = new Mock<IFileSystemWatcher>();
            _mockEventHub = new Mock<IGlobalEventHub>();
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, true);
        }

        [Test]
        public void Dispose_StopsRaisingEvents()
        {
            var container = new SystemFolderContainer(_tempDir, _fileSystemAccess.Object, _mockWatcher.Object, _mockEventHub.Object);

            container.Dispose();

            _mockWatcher.VerifySet(w => w.EnableRaisingEvents = false);
        }

        [Test]
        public void Dispose_DisposesWatcher()
        {
            var container = new SystemFolderContainer(_tempDir, _fileSystemAccess.Object, _mockWatcher.Object, _mockEventHub.Object);

            container.Dispose();

            _mockWatcher.Verify(w => w.Dispose(), Times.Once);
        }

        [Test]
        public void Dispose_ClearsFileList()
        {
            var container = new SystemFolderContainer(_tempDir, _fileSystemAccess.Object, _mockWatcher.Object, _mockEventHub.Object);
            Assert.That(container.GetFileCount(), Is.GreaterThan(0));

            container.Dispose();

            Assert.That(container.GetFileCount(), Is.EqualTo(0));
        }

        [Test]
        public void Dispose_CalledTwice_NoException()
        {
            var container = new SystemFolderContainer(_tempDir, _fileSystemAccess.Object, _mockWatcher.Object, _mockEventHub.Object);

            container.Dispose();
            Assert.DoesNotThrow(() => container.Dispose());
        }

        [Test]
        public void Dispose_WithoutWatcher_NoException()
        {
            var container = new SystemFolderContainer(_tempDir, _fileSystemAccess.Object);

            Assert.DoesNotThrow(() => container.Dispose());
            Assert.That(container.GetFileCount(), Is.EqualTo(0));
        }
    }
}
