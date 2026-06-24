using Moq;
using Shared.Core.Events;
using Shared.Core.PackFiles.Events;
using Shared.Core.PackFiles.Models.Containers;
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

        [Test]
        public void ExternalFileCreated_InSubdirectory_PublishesFilesAddedEvent()
        {
            var subDir = Path.Combine(_tempDir, "models");
            Directory.CreateDirectory(subDir);
            var newFilePath = Path.Combine(subDir, "hero.mesh");
            File.WriteAllText(newFilePath, "mesh data");

            _mockWatcher.Raise(w => w.Created += null, new FileSystemEventArgs(WatcherChangeTypes.Created, subDir, "hero.mesh"));

            _container.ProcessPendingEvents(null);

            _mockEventHub.Verify(x => x.PublishGlobalEvent(It.Is<PackFileContainerFilesAddedEvent>(e =>
                e.AddedFiles.Count == 1 && e.AddedFiles[0].Name == "hero.mesh"
            )), Times.Once);

            Assert.That(_container.ContainsFile(@"models\hero.mesh"), Is.True);
        }

        [Test]
        public void ExternalFolderDeleted_RemovesAllContainedFiles()
        {
            // Set up container with files in a subfolder
            var subDir = Path.Combine(_tempDir, "scripts");
            Directory.CreateDirectory(subDir);
            var file1 = Path.Combine(subDir, "main.lua");
            var file2 = Path.Combine(subDir, "utils.lua");
            File.WriteAllText(file1, "lua1");
            File.WriteAllText(file2, "lua2");

            // Re-create container with the subfolder files
            _container.Dispose();
            _fileSystemAccess.Setup(x => x.DirectoryGetFiles(_tempDir, "*.*", SearchOption.AllDirectories))
                .Returns([Path.Combine(_tempDir, "existing.txt"), file1, file2]);
            _mockWatcher = new Mock<IFileSystemWatcher>();
            _mockEventHub = new Mock<IGlobalEventHub>();
            _container = new SystemFolderContainer(_tempDir, _fileSystemAccess.Object, _mockWatcher.Object, _mockEventHub.Object);

            Assert.That(_container.GetFileCount(), Is.EqualTo(3));

            // Simulate folder deletion event — watcher fires a single event for the folder
            _mockWatcher.Raise(w => w.Deleted += null, new FileSystemEventArgs(WatcherChangeTypes.Deleted, _tempDir, "scripts"));

            _container.ProcessPendingEvents(null);

            _mockEventHub.Verify(x => x.PublishGlobalEvent(It.Is<PackFileContainerFilesRemovedEvent>(e =>
                e.RemovedFiles.Count == 2
            )), Times.Once);

            Assert.That(_container.GetFileCount(), Is.EqualTo(1));
            Assert.That(_container.ContainsFile("existing.txt"), Is.True);
            Assert.That(_container.ContainsFile(@"scripts\main.lua"), Is.False);
            Assert.That(_container.ContainsFile(@"scripts\utils.lua"), Is.False);
        }

        [Test]
        public void ExternalFolderRenamed_RemovesOldAndAddsNewFiles()
        {
            // Set up container with files in a subfolder
            var oldDir = Path.Combine(_tempDir, "oldfolder");
            var newDir = Path.Combine(_tempDir, "newfolder");
            Directory.CreateDirectory(oldDir);
            var oldFile = Path.Combine(oldDir, "data.bin");
            File.WriteAllText(oldFile, "binary");

            _container.Dispose();
            _fileSystemAccess.Setup(x => x.DirectoryGetFiles(_tempDir, "*.*", SearchOption.AllDirectories))
                .Returns([Path.Combine(_tempDir, "existing.txt"), oldFile]);
            _mockWatcher = new Mock<IFileSystemWatcher>();
            _mockEventHub = new Mock<IGlobalEventHub>();
            _container = new SystemFolderContainer(_tempDir, _fileSystemAccess.Object, _mockWatcher.Object, _mockEventHub.Object);

            Assert.That(_container.GetFileCount(), Is.EqualTo(2));

            // Simulate the folder rename on disk (move oldfolder to newfolder)
            Directory.Move(oldDir, newDir);

            // Watcher raises a single Renamed event for the folder
            var args = new RenamedEventArgs(WatcherChangeTypes.Renamed, _tempDir, "newfolder", "oldfolder");
            _mockWatcher.Raise(w => w.Renamed += null, args);

            _container.ProcessPendingEvents(null);

            // Old file should be removed
            _mockEventHub.Verify(x => x.PublishGlobalEvent(It.Is<PackFileContainerFilesRemovedEvent>(e =>
                e.RemovedFiles.Count == 1 && e.RemovedFiles[0].Name == "data.bin"
            )), Times.Once);

            // New file should be added from the renamed folder
            _mockEventHub.Verify(x => x.PublishGlobalEvent(It.Is<PackFileContainerFilesAddedEvent>(e =>
                e.AddedFiles.Count == 1 && e.AddedFiles[0].Name == "data.bin"
            )), Times.Once);

            Assert.That(_container.ContainsFile(@"oldfolder\data.bin"), Is.False);
            Assert.That(_container.ContainsFile(@"newfolder\data.bin"), Is.True);
        }

        [Test]
        public void ExternalFileRenamed_InSubdirectory_UpdatesCorrectly()
        {
            // Set up container with a file in subfolder
            var subDir = Path.Combine(_tempDir, "textures");
            Directory.CreateDirectory(subDir);
            var oldFile = Path.Combine(subDir, "diffuse.dds");
            File.WriteAllText(oldFile, "texture");

            _container.Dispose();
            _fileSystemAccess.Setup(x => x.DirectoryGetFiles(_tempDir, "*.*", SearchOption.AllDirectories))
                .Returns([Path.Combine(_tempDir, "existing.txt"), oldFile]);
            _mockWatcher = new Mock<IFileSystemWatcher>();
            _mockEventHub = new Mock<IGlobalEventHub>();
            _container = new SystemFolderContainer(_tempDir, _fileSystemAccess.Object, _mockWatcher.Object, _mockEventHub.Object);

            // Rename on disk
            var newFile = Path.Combine(subDir, "normal.dds");
            File.Move(oldFile, newFile);

            // Watcher event
            var args = new RenamedEventArgs(WatcherChangeTypes.Renamed, subDir, "normal.dds", "diffuse.dds");
            _mockWatcher.Raise(w => w.Renamed += null, args);

            _container.ProcessPendingEvents(null);

            _mockEventHub.Verify(x => x.PublishGlobalEvent(It.Is<PackFileContainerFilesRemovedEvent>(e =>
                e.RemovedFiles.Count == 1 && e.RemovedFiles[0].Name == "diffuse.dds"
            )), Times.Once);

            _mockEventHub.Verify(x => x.PublishGlobalEvent(It.Is<PackFileContainerFilesAddedEvent>(e =>
                e.AddedFiles.Count == 1 && e.AddedFiles[0].Name == "normal.dds"
            )), Times.Once);

            Assert.That(_container.ContainsFile(@"textures\diffuse.dds"), Is.False);
            Assert.That(_container.ContainsFile(@"textures\normal.dds"), Is.True);
        }

        [Test]
        public void MultipleRapidDeletes_BatchedIntoSingleEvent()
        {
            // Set up container with multiple files
            var file1 = Path.Combine(_tempDir, "a.txt");
            var file2 = Path.Combine(_tempDir, "b.txt");
            File.WriteAllText(file1, "a");
            File.WriteAllText(file2, "b");

            _container.Dispose();
            _fileSystemAccess.Setup(x => x.DirectoryGetFiles(_tempDir, "*.*", SearchOption.AllDirectories))
                .Returns([file1, file2]);
            _mockWatcher = new Mock<IFileSystemWatcher>();
            _mockEventHub = new Mock<IGlobalEventHub>();
            _container = new SystemFolderContainer(_tempDir, _fileSystemAccess.Object, _mockWatcher.Object, _mockEventHub.Object);

            // Simulate rapid deletes
            _mockWatcher.Raise(w => w.Deleted += null, new FileSystemEventArgs(WatcherChangeTypes.Deleted, _tempDir, "a.txt"));
            _mockWatcher.Raise(w => w.Deleted += null, new FileSystemEventArgs(WatcherChangeTypes.Deleted, _tempDir, "b.txt"));

            _container.ProcessPendingEvents(null);

            _mockEventHub.Verify(x => x.PublishGlobalEvent(It.Is<PackFileContainerFilesRemovedEvent>(e =>
                e.RemovedFiles.Count == 2
            )), Times.Once);

            Assert.That(_container.GetFileCount(), Is.EqualTo(0));
        }

        [Test]
        public void MixedCreateAndDelete_InSameBatch_PublishesBothEvents()
        {
            // Create a new file on disk
            var newFile = Path.Combine(_tempDir, "added.txt");
            File.WriteAllText(newFile, "new");

            // Simulate: existing file deleted + new file created in same debounce window
            _mockWatcher.Raise(w => w.Deleted += null, new FileSystemEventArgs(WatcherChangeTypes.Deleted, _tempDir, "existing.txt"));
            _mockWatcher.Raise(w => w.Created += null, new FileSystemEventArgs(WatcherChangeTypes.Created, _tempDir, "added.txt"));

            _container.ProcessPendingEvents(null);

            _mockEventHub.Verify(x => x.PublishGlobalEvent(It.Is<PackFileContainerFilesRemovedEvent>(e =>
                e.RemovedFiles.Count == 1 && e.RemovedFiles[0].Name == "existing.txt"
            )), Times.Once);

            _mockEventHub.Verify(x => x.PublishGlobalEvent(It.Is<PackFileContainerFilesAddedEvent>(e =>
                e.AddedFiles.Count == 1 && e.AddedFiles[0].Name == "added.txt"
            )), Times.Once);

            Assert.That(_container.ContainsFile("existing.txt"), Is.False);
            Assert.That(_container.ContainsFile("added.txt"), Is.True);
        }

        [Test]
        public void ExternalDirectoryCreated_AddsAllFilesWithin()
        {
            // Simulate copying a folder into the watched directory
            var newDir = Path.Combine(_tempDir, "imported");
            Directory.CreateDirectory(newDir);
            File.WriteAllText(Path.Combine(newDir, "model.rmv2"), "mesh");
            File.WriteAllText(Path.Combine(newDir, "texture.dds"), "tex");

            // Watcher fires Created for the directory
            _mockWatcher.Raise(w => w.Created += null, new FileSystemEventArgs(WatcherChangeTypes.Created, _tempDir, "imported"));

            _container.ProcessPendingEvents(null);

            _mockEventHub.Verify(x => x.PublishGlobalEvent(It.Is<PackFileContainerFilesAddedEvent>(e =>
                e.AddedFiles.Count == 2
            )), Times.Once);

            Assert.That(_container.ContainsFile(@"imported\model.rmv2"), Is.True);
            Assert.That(_container.ContainsFile(@"imported\texture.dds"), Is.True);
        }

        // ──────────────────────────────────────────────────────────────────────
        // B10 — Chatty / duplicate watcher events must not produce duplicate
        // entries in the published add/remove payloads.
        // ──────────────────────────────────────────────────────────────────────

        [Test]
        public void B10_DuplicateDeleteEvents_RemovedFilePublishedOnce()
        {
            // Watcher commonly raises the same Deleted event more than once.
            _mockWatcher.Raise(w => w.Deleted += null, new FileSystemEventArgs(WatcherChangeTypes.Deleted, _tempDir, "existing.txt"));
            _mockWatcher.Raise(w => w.Deleted += null, new FileSystemEventArgs(WatcherChangeTypes.Deleted, _tempDir, "existing.txt"));

            _container.ProcessPendingEvents(null);

            _mockEventHub.Verify(x => x.PublishGlobalEvent(It.Is<PackFileContainerFilesRemovedEvent>(e =>
                e.RemovedFiles.Count == 1 && e.RemovedFiles[0].Name == "existing.txt"
            )), Times.Once);

            Assert.That(_container.ContainsFile("existing.txt"), Is.False);
        }

        [Test]
        public void B10_DuplicateCreateEvents_AddedFilePublishedOnce()
        {
            var newFilePath = Path.Combine(_tempDir, "dup.txt");
            File.WriteAllText(newFilePath, "data");

            _mockWatcher.Raise(w => w.Created += null, new FileSystemEventArgs(WatcherChangeTypes.Created, _tempDir, "dup.txt"));
            _mockWatcher.Raise(w => w.Created += null, new FileSystemEventArgs(WatcherChangeTypes.Created, _tempDir, "dup.txt"));

            _container.ProcessPendingEvents(null);

            _mockEventHub.Verify(x => x.PublishGlobalEvent(It.Is<PackFileContainerFilesAddedEvent>(e =>
                e.AddedFiles.Count == 1 && e.AddedFiles[0].Name == "dup.txt"
            )), Times.Once);
        }

        [Test]
        public void B10_FolderDeletePlusChildDelete_FilePublishedOnce()
        {
            // Re-create container with a subfolder file
            var subDir = Path.Combine(_tempDir, "folder");
            Directory.CreateDirectory(subDir);
            var child = Path.Combine(subDir, "child.txt");
            File.WriteAllText(child, "child");

            _container.Dispose();
            _fileSystemAccess.Setup(x => x.DirectoryGetFiles(_tempDir, "*.*", SearchOption.AllDirectories))
                .Returns([Path.Combine(_tempDir, "existing.txt"), child]);
            _mockWatcher = new Mock<IFileSystemWatcher>();
            _mockEventHub = new Mock<IGlobalEventHub>();
            _container = new SystemFolderContainer(_tempDir, _fileSystemAccess.Object, _mockWatcher.Object, _mockEventHub.Object);

            // Both a folder delete and the child file delete arrive in the same batch.
            _mockWatcher.Raise(w => w.Deleted += null, new FileSystemEventArgs(WatcherChangeTypes.Deleted, _tempDir, "folder"));
            _mockWatcher.Raise(w => w.Deleted += null, new FileSystemEventArgs(WatcherChangeTypes.Deleted, subDir, "child.txt"));

            _container.ProcessPendingEvents(null);

            // child.txt must be reported exactly once, not twice.
            _mockEventHub.Verify(x => x.PublishGlobalEvent(It.Is<PackFileContainerFilesRemovedEvent>(e =>
                e.RemovedFiles.Count(f => f.Name == "child.txt") == 1
            )), Times.Once);

            Assert.That(_container.ContainsFile(@"folder\child.txt"), Is.False);
        }

        // ──────────────────────────────────────────────────────────────────────
        // B12 — Watcher activity around dispose must not throw or publish.
        // ──────────────────────────────────────────────────────────────────────

        [Test]
        public void B12_ProcessPendingEvents_AfterDispose_DoesNotThrowOrPublish()
        {
            // Queue an event, then dispose before processing.
            var newFilePath = Path.Combine(_tempDir, "late.txt");
            File.WriteAllText(newFilePath, "late");
            _mockWatcher.Raise(w => w.Created += null, new FileSystemEventArgs(WatcherChangeTypes.Created, _tempDir, "late.txt"));

            _container.Dispose();

            Assert.DoesNotThrow(() => _container.ProcessPendingEvents(null));
            _mockEventHub.Verify(x => x.PublishGlobalEvent(It.IsAny<PackFileContainerFilesAddedEvent>()), Times.Never);
        }
    }
}
