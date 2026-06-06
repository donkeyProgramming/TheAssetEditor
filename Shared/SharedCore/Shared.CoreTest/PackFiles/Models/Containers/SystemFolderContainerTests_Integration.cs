using Moq;
using Shared.Core.Events;
using Shared.Core.Events.Global;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.PackFiles.Models.Containers;
using Shared.Core.PackFiles.Models.FileSources;
using Shared.Core.PackFiles.Serialization;
using Shared.Core.PackFiles.Utility;
using Shared.Core.Services;
using Shared.Core.Settings;

namespace Shared.CoreTest.PackFiles.Models.Containers
{
    [TestFixture]
    internal class SystemFolderContainerTests_Integration
    {
        private string _tempDir = null!;

        [SetUp]
        public void Setup()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "SysFolderInteg_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDir);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, true);
        }

        private static IFileSystemAccess CreateRealFileSystemAccess()
        {
            return new FileSystemAccess();
        }

        [Test]
        public void FullWorkflow_CreateFolder_AddFile_DeleteFile_Save()
        {
            // Arrange: seed a file in the temp directory
            var seedPath = Path.Combine(_tempDir, "models", "unit.txt");
            Directory.CreateDirectory(Path.GetDirectoryName(seedPath)!);
            File.WriteAllText(seedPath, "original");

            var eventHub = new Mock<IGlobalEventHub>();
            var fileSystem = CreateRealFileSystemAccess();
            var watcher = new Mock<IFileSystemWatcher>();

            // Act: create container from real folder
            var container = new SystemFolderContainer(_tempDir, fileSystem, watcher.Object, eventHub.Object);
            Assert.That(container.GetFileCount(), Is.EqualTo(1));
            Assert.That(container.FindFile(@"models\unit.txt"), Is.Not.Null);

            // Register with PackFileService
            var pfs = new PackFileService(eventHub.Object);
            pfs.MessageBoxProvider = new Mock<ISimpleMessageBox>().Object;
            pfs.EnforceGameFilesMustBeLoaded = false;
            pfs.AddContainer(container);

            // Add a file via service
            var newFileData = "new file content"u8.ToArray();
            var newFile = new PackFile("added.bin", new MemorySource(newFileData));
            pfs.AddFilesToPack(container, [new NewPackFileEntry("scripts", newFile)]);

            Assert.That(container.GetFileCount(), Is.EqualTo(2));
            Assert.That(File.Exists(Path.Combine(_tempDir, "scripts", "added.bin")), Is.True);
            Assert.That(File.ReadAllBytes(Path.Combine(_tempDir, "scripts", "added.bin")), Is.EqualTo(newFileData));

            // Delete the original file via service
            var originalFile = container.FindFile(@"models\unit.txt")!;
            pfs.DeleteFile(container, originalFile);

            Assert.That(container.GetFileCount(), Is.EqualTo(1));
            Assert.That(File.Exists(seedPath), Is.False);

            // Save as pack
            var packPath = Path.Combine(_tempDir, "output.pack");
            var gameInfo = GameInformationDatabase.GetGameById(GameTypeEnum.Warhammer3);
            pfs.SavePackContainer(container, packPath, false, gameInfo);

            Assert.That(File.Exists(packPath), Is.True);

            // Verify the pack file is valid and contains only the added file
            using var fs = File.OpenRead(packPath);
            using var reader = new BinaryReader(fs);
            var loaded = PackFileSerializerLoader.Load(packPath, fs.Length, reader, new CaPackDuplicateFileResolver());
            Assert.That(loaded.GetFileCount(), Is.EqualTo(1));
            Assert.That(loaded.FindFile(@"scripts\added.bin"), Is.Not.Null);

            // Container should remain active
            Assert.That(container.GetFileCount(), Is.EqualTo(1));
            Assert.That(container.SystemFilePath, Is.EqualTo(_tempDir));

            // Unload disposes cleanly
            pfs.UnloadPackContainer(container);
            watcher.Verify(w => w.Dispose(), Times.Once);
        }

        [Test]
        public void FullWorkflow_ExternalAdd_DetectedAndEventsPublished()
        {
            // Arrange
            var seedPath = Path.Combine(_tempDir, "initial.txt");
            File.WriteAllText(seedPath, "init");

            var eventHub = new Mock<IGlobalEventHub>();
            var fileSystem = CreateRealFileSystemAccess();
            var mockWatcher = new Mock<IFileSystemWatcher>();

            var container = new SystemFolderContainer(_tempDir, fileSystem, mockWatcher.Object, eventHub.Object);
            Assert.That(container.GetFileCount(), Is.EqualTo(1));

            // Simulate an external file being created on disk
            var externalPath = Path.Combine(_tempDir, "external.txt");
            File.WriteAllText(externalPath, "external content");

            // Simulate the watcher firing a Created event
            mockWatcher.Raise(w => w.Created += null, new FileSystemEventArgs(WatcherChangeTypes.Created, _tempDir, "external.txt"));

            // Process the pending events (as the debounce timer would)
            container.ProcessPendingEvents(null);

            // Verify file was added to container
            Assert.That(container.GetFileCount(), Is.EqualTo(2));
            Assert.That(container.ContainsFile("external.txt"), Is.True);

            // Verify event was published
            eventHub.Verify(x => x.PublishGlobalEvent(It.Is<PackFileContainerFilesAddedEvent>(e =>
                e.Container == container &&
                e.AddedFiles.Count == 1 &&
                e.AddedFiles[0].Name == "external.txt"
            )), Times.Once);

            // Simulate external deletion
            File.Delete(externalPath);
            mockWatcher.Raise(w => w.Deleted += null, new FileSystemEventArgs(WatcherChangeTypes.Deleted, _tempDir, "external.txt"));
            container.ProcessPendingEvents(null);

            Assert.That(container.GetFileCount(), Is.EqualTo(1));
            Assert.That(container.ContainsFile("external.txt"), Is.False);
            eventHub.Verify(x => x.PublishGlobalEvent(It.Is<PackFileContainerFilesRemovedEvent>(e =>
                e.Container == container &&
                e.RemovedFiles.Count == 1 &&
                e.RemovedFiles[0].Name == "external.txt"
            )), Times.Once);
        }

        [Test]
        public void FullWorkflow_RenameAndMoveFile_ThenSave()
        {
            // Seed files
            var filePath = Path.Combine(_tempDir, "folder", "original.txt");
            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
            File.WriteAllText(filePath, "data");

            var eventHub = new Mock<IGlobalEventHub>();
            var fileSystem = CreateRealFileSystemAccess();
            var mockWatcher = new Mock<IFileSystemWatcher>();

            var container = new SystemFolderContainer(_tempDir, fileSystem, mockWatcher.Object, eventHub.Object);
            var pfs = new PackFileService(eventHub.Object);
            pfs.MessageBoxProvider = new Mock<ISimpleMessageBox>().Object;
            pfs.EnforceGameFilesMustBeLoaded = false;
            pfs.AddContainer(container);

            // Rename file
            var file = container.FindFile(@"folder\original.txt")!;
            pfs.RenameFile(container, file, "renamed.txt");

            Assert.That(container.ContainsFile(@"folder\original.txt"), Is.False);
            Assert.That(container.ContainsFile(@"folder\renamed.txt"), Is.True);
            Assert.That(File.Exists(Path.Combine(_tempDir, "folder", "renamed.txt")), Is.True);

            // Move file to root
            var renamedFile = container.FindFile(@"folder\renamed.txt")!;
            pfs.MoveFile(container, renamedFile, "");

            Assert.That(container.ContainsFile(@"folder\renamed.txt"), Is.False);
            Assert.That(container.ContainsFile("renamed.txt"), Is.True);
            Assert.That(File.Exists(Path.Combine(_tempDir, "renamed.txt")), Is.True);

            // Save to pack and verify content
            var packPath = Path.Combine(_tempDir, "result.pack");
            var gameInfo = GameInformationDatabase.GetGameById(GameTypeEnum.Warhammer3);
            pfs.SavePackContainer(container, packPath, false, gameInfo);

            using var fs = File.OpenRead(packPath);
            using var reader = new BinaryReader(fs);
            var loaded = PackFileSerializerLoader.Load(packPath, fs.Length, reader, new CaPackDuplicateFileResolver());
            Assert.That(loaded.GetFileCount(), Is.EqualTo(1));
            Assert.That(loaded.FindFile("renamed.txt"), Is.Not.Null);
        }
    }
}
