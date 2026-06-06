using Moq;
using Shared.Core.Events;
using Shared.Core.Events.Global;
using Shared.Core.PackFiles.Models;
using Shared.Core.PackFiles.Models.Containers;
using Shared.Core.PackFiles.Models.FileSources;
using Shared.Core.Services;

namespace Shared.CoreTest.PackFiles.Models.Containers
{
    [TestFixture]
    internal class SystemFolderContainerTests_Watcher
    {
        private string _tempDir = null!;
        private Mock<IFileSystemAccess> _fileSystemAccess = null!;
        private Mock<IFileSystemWatcher> _mockWatcher = null!;
        private Mock<IGlobalEventHub> _mockEventHub = null!;
        private SystemFolderContainer _container = null!;

        [SetUp]
        public void Setup()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "SysFolderWatcher_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDir);

            // Create a seed file
            var seedPath = Path.Combine(_tempDir, "existing.txt");
            File.WriteAllText(seedPath, "seed");

            _fileSystemAccess = new Mock<IFileSystemAccess>();
            _fileSystemAccess.Setup(x => x.DirectoryExists(_tempDir)).Returns(true);
            _fileSystemAccess.Setup(x => x.DirectoryGetFiles(_tempDir, "*.*", SearchOption.AllDirectories))
                .Returns([seedPath]);
            _fileSystemAccess.Setup(x => x.DirectoryExists(It.IsAny<string>()))
                .Returns((string p) => Directory.Exists(p));
            _fileSystemAccess.Setup(x => x.DirectoryCreateDirectory(It.IsAny<string>()))
                .Callback((string p) => Directory.CreateDirectory(p));
            _fileSystemAccess.Setup(x => x.FileWriteAllBytes(It.IsAny<string>(), It.IsAny<byte[]>()))
                .Callback((string p, byte[] d) => { Directory.CreateDirectory(Path.GetDirectoryName(p)!); File.WriteAllBytes(p, d); });
            _fileSystemAccess.Setup(x => x.FileExists(It.IsAny<string>()))
                .Returns((string p) => File.Exists(p));
            _fileSystemAccess.Setup(x => x.FileDelete(It.IsAny<string>()))
                .Callback((string p) => File.Delete(p));

            _mockWatcher = new Mock<IFileSystemWatcher>();
            _mockEventHub = new Mock<IGlobalEventHub>();

            _container = new SystemFolderContainer(_tempDir, _fileSystemAccess.Object, _mockWatcher.Object, _mockEventHub.Object);
        }

        [TearDown]
        public void TearDown()
        {
            _container.Dispose();
            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, true);
        }

        [Test]
        public void Constructor_StartsWatcher()
        {
            _mockWatcher.VerifySet(w => w.Path = _tempDir);
            _mockWatcher.VerifySet(w => w.IncludeSubdirectories = true);
            _mockWatcher.VerifySet(w => w.EnableRaisingEvents = true);
        }

        [Test]
        public void ExternalFileCreated_PublishesFilesAddedEvent()
        {
            // Create the file on disk so FileSystemSource can work
            var newFilePath = Path.Combine(_tempDir, "newfile.txt");
            File.WriteAllText(newFilePath, "new content");

            // Simulate the watcher event
            _mockWatcher.Raise(w => w.Created += null, new FileSystemEventArgs(WatcherChangeTypes.Created, _tempDir, "newfile.txt"));

            // Process debounced events immediately
            _container.ProcessPendingEvents(null);

            _mockEventHub.Verify(x => x.PublishGlobalEvent(It.Is<PackFileContainerFilesAddedEvent>(e =>
                e.AddedFiles.Count == 1 && e.AddedFiles[0].Name == "newfile.txt"
            )), Times.Once);

            Assert.That(_container.ContainsFile("newfile.txt"), Is.True);
        }

        [Test]
        public void ExternalFileDeleted_PublishesFilesRemovedEvent()
        {
            // Simulate deletion of the existing file
            _mockWatcher.Raise(w => w.Deleted += null, new FileSystemEventArgs(WatcherChangeTypes.Deleted, _tempDir, "existing.txt"));

            _container.ProcessPendingEvents(null);

            _mockEventHub.Verify(x => x.PublishGlobalEvent(It.Is<PackFileContainerFilesRemovedEvent>(e =>
                e.RemovedFiles.Count == 1 && e.RemovedFiles[0].Name == "existing.txt"
            )), Times.Once);

            Assert.That(_container.ContainsFile("existing.txt"), Is.False);
        }

        [Test]
        public void ExternalFileRenamed_PublishesRemovedAndAddedEvents()
        {
            // Create the renamed file on disk
            var renamedPath = Path.Combine(_tempDir, "renamed.txt");
            File.WriteAllText(renamedPath, "seed");

            // Simulate rename event
            var args = new RenamedEventArgs(WatcherChangeTypes.Renamed, _tempDir, "renamed.txt", "existing.txt");
            _mockWatcher.Raise(w => w.Renamed += null, args);

            _container.ProcessPendingEvents(null);

            _mockEventHub.Verify(x => x.PublishGlobalEvent(It.Is<PackFileContainerFilesRemovedEvent>(e =>
                e.RemovedFiles.Count == 1 && e.RemovedFiles[0].Name == "existing.txt"
            )), Times.Once);

            _mockEventHub.Verify(x => x.PublishGlobalEvent(It.Is<PackFileContainerFilesAddedEvent>(e =>
                e.AddedFiles.Count == 1 && e.AddedFiles[0].Name == "renamed.txt"
            )), Times.Once);

            Assert.That(_container.ContainsFile("existing.txt"), Is.False);
            Assert.That(_container.ContainsFile("renamed.txt"), Is.True);
        }

        [Test]
        public void InternalAdd_DoesNotTriggerExternalEvent()
        {
            // Create file on disk so it could be picked up
            var filePath = Path.Combine(_tempDir, "suppressed.txt");
            File.WriteAllText(filePath, "data");

            // Simulate watcher firing while suppression is active (e.g., during internal write)
            _container._suppressWatcher = true;
            _mockWatcher.Raise(w => w.Created += null, new FileSystemEventArgs(WatcherChangeTypes.Created, _tempDir, "suppressed.txt"));
            _container._suppressWatcher = false;

            _container.ProcessPendingEvents(null);

            // The suppressed event should not have been queued
            _mockEventHub.Verify(x => x.PublishGlobalEvent(It.IsAny<PackFileContainerFilesAddedEvent>()), Times.Never);
            // File should NOT be in container since the event was suppressed
            Assert.That(_container.ContainsFile("suppressed.txt"), Is.False);
        }

        [Test]
        public void InternalDelete_DoesNotTriggerExternalEvent()
        {
            _container._suppressWatcher = true;
            _mockWatcher.Raise(w => w.Deleted += null, new FileSystemEventArgs(WatcherChangeTypes.Deleted, _tempDir, "existing.txt"));
            _container._suppressWatcher = false;

            _container.ProcessPendingEvents(null);

            // Event should not be published since it was suppressed
            _mockEventHub.Verify(x => x.PublishGlobalEvent(It.IsAny<PackFileContainerFilesRemovedEvent>()), Times.Never);
            // File should still be in the list
            Assert.That(_container.ContainsFile("existing.txt"), Is.True);
        }

        [Test]
        public void MultipleRapidCreates_BatchedIntoSingleEvent()
        {
            // Create files on disk
            var file1 = Path.Combine(_tempDir, "batch1.txt");
            var file2 = Path.Combine(_tempDir, "batch2.txt");
            File.WriteAllText(file1, "1");
            File.WriteAllText(file2, "2");

            // Simulate multiple rapid creates
            _mockWatcher.Raise(w => w.Created += null, new FileSystemEventArgs(WatcherChangeTypes.Created, _tempDir, "batch1.txt"));
            _mockWatcher.Raise(w => w.Created += null, new FileSystemEventArgs(WatcherChangeTypes.Created, _tempDir, "batch2.txt"));

            // Process them all at once
            _container.ProcessPendingEvents(null);

            // Should be a single event with both files
            _mockEventHub.Verify(x => x.PublishGlobalEvent(It.Is<PackFileContainerFilesAddedEvent>(e =>
                e.AddedFiles.Count == 2
            )), Times.Once);
        }

        [Test]
        public void Dispose_StopsWatcher()
        {
            _container.Dispose();

            _mockWatcher.VerifySet(w => w.EnableRaisingEvents = false);
            _mockWatcher.Verify(w => w.Dispose(), Times.Once);
        }

        [Test]
        public void ExternalFileCreated_DuplicatePath_IgnoredGracefully()
        {
            // existing.txt is already in the container
            // Try to trigger a Created event for the same path
            _mockWatcher.Raise(w => w.Created += null, new FileSystemEventArgs(WatcherChangeTypes.Created, _tempDir, "existing.txt"));

            _container.ProcessPendingEvents(null);

            // Should not publish event since file is already tracked
            _mockEventHub.Verify(x => x.PublishGlobalEvent(It.IsAny<PackFileContainerFilesAddedEvent>()), Times.Never);
        }

        [Test]
        public void ExternalFileDeleted_UnknownFile_IgnoredGracefully()
        {
            _mockWatcher.Raise(w => w.Deleted += null, new FileSystemEventArgs(WatcherChangeTypes.Deleted, _tempDir, "unknown.txt"));

            _container.ProcessPendingEvents(null);

            _mockEventHub.Verify(x => x.PublishGlobalEvent(It.IsAny<PackFileContainerFilesRemovedEvent>()), Times.Never);
        }
    }
}
