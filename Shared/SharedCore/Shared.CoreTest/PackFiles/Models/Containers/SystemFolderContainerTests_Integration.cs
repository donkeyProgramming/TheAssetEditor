using Moq;
using Shared.Core.Events;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Events;
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

        private static string GetDataFileFromWorkspace(string fileName)
        {
            var currentDirectory = TestContext.CurrentContext.TestDirectory;

            while (true)
            {
                var directoryName = Path.GetFileName(currentDirectory);
                if (string.IsNullOrWhiteSpace(directoryName))
                    throw new Exception($"Unable to resolve workspace root for test directory '{TestContext.CurrentContext.TestDirectory}'");

                if (string.Equals(directoryName, "TheAssetEditor", StringComparison.OrdinalIgnoreCase))
                    break;

                currentDirectory = Path.GetDirectoryName(currentDirectory)
                    ?? throw new Exception($"Unable to resolve parent folder from '{currentDirectory}'");
            }

            var fullPath = Path.Combine(currentDirectory, "Data", fileName);
            if (File.Exists(fullPath) == false)
                throw new Exception($"Unable to find data file '{fileName}' in '{fullPath}'");

            return fullPath;
        }

        [Test]
        public void ComplexFlow_SystemFolderProject_FromKarlPack_PersistsIgnoreSettings_AndSavesExpectedPack()
        {
            // Arrange: load Karl pack as game pack
            var karlPackPath = GetDataFileFromWorkspace("Karl_and_celestialgeneral.pack");
            PackFileContainer karlContainer;
            using (var fs = File.OpenRead(karlPackPath))
            using (var reader = new BinaryReader(fs))
            {
                karlContainer = PackFileSerializerLoader.Load(karlPackPath, fs.Length, reader, new CustomPackDuplicateFileResolver());
            }
            karlContainer.IsCaPackFile = true;
            karlContainer.IsReadOnly = true;

            var eventHub = new Mock<IGlobalEventHub>();
            var fileSystem = CreateRealFileSystemAccess();
            var watcher = new Mock<IFileSystemWatcher>();

            var projectContainer = new SystemFolderContainer(_tempDir, fileSystem, watcher.Object, eventHub.Object);
            projectContainer.PackFileSettings.GameVersion = GameTypeEnum.Warhammer3;

            var pfs = new PackFileService(eventHub.Object);
            pfs.MessageBoxProvider = new Mock<ISimpleMessageBox>().Object;
            pfs.EnforceGameFilesMustBeLoaded = false;
            pfs.AddContainer(karlContainer);
            pfs.AddContainer(projectContainer, true);

            // Copy a few files from Karl into the project
            var copiedPaths = karlContainer.GetAllFiles().Keys.OrderBy(x => x).Take(3).ToList();
            Assert.That(copiedPaths.Count, Is.EqualTo(3), "Karl pack should have enough files for this integration test.");
            foreach (var path in copiedPaths)
                pfs.CopyFileFromOtherPackFile(karlContainer, path, projectContainer);

            // Add two new files; one inside a folder and one at root
            var inFolder = PackFile.CreateFromASCII("in_folder.txt", "folder-content");
            var atRoot = PackFile.CreateFromASCII("at_root.txt", "root-content");
            pfs.AddFilesToPack(projectContainer,
            [
                new NewPackFileEntry("new_folder", inFolder),
                new NewPackFileEntry("", atRoot)
            ]);

            var ignoredPath = PathNormalization.NormalizeFileName(@"new_folder\in_folder.txt");
            projectContainer.PackFileSettings.IgnoredFilesWhenSerializing.Add(ignoredPath);

            // Act: save and reload the pack
            var outputPackPath = Path.Combine(_tempDir, "complex_flow_output.pack");
            var gameInfo = GameInformationDatabase.GetGameById(GameTypeEnum.Rome2); // should be ignored in favor of settings
            pfs.SavePackContainer(projectContainer, outputPackPath, false, gameInfo);

            PackFileContainer reloadedPack;
            using (var outFs = File.OpenRead(outputPackPath))
            using (var outReader = new BinaryReader(outFs))
            {
                reloadedPack = PackFileSerializerLoader.Load(outputPackPath, outFs.Length, outReader, new CaPackDuplicateFileResolver());
            }

            // Assert: version comes from project settings (Warhammer3 -> PFH5)
            var expectedVersion = GameInformationDatabase.GetGameById(GameTypeEnum.Warhammer3).PackFileVersion;
            Assert.That(reloadedPack.Header.Version, Is.EqualTo(expectedVersion));

            // Assert: copied files and root file are present, ignored file is excluded
            foreach (var copiedPath in copiedPaths)
                Assert.That(reloadedPack.ContainsFile(copiedPath), Is.True, $"Expected copied file '{copiedPath}' in saved pack");

            Assert.That(reloadedPack.ContainsFile("at_root.txt"), Is.True);
            Assert.That(reloadedPack.ContainsFile(ignoredPath), Is.False);

            var rootData = reloadedPack.FindFile("at_root.txt")!.DataSource.ReadData();
            Assert.That(System.Text.Encoding.ASCII.GetString(rootData), Is.EqualTo("root-content"));

            // Assert: project folder structure (excluding ignored + project settings json) matches saved pack content
            var ignoredSet = projectContainer.PackFileSettings.IgnoredFilesWhenSerializing
                .Select(PathNormalization.NormalizeFileName)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var diskFiles = Directory.GetFiles(_tempDir, "*.*", SearchOption.AllDirectories)
                .Select(x => PathNormalization.NormalizeFileName(Path.GetRelativePath(_tempDir, x)))
                .Where(x => !string.Equals(x, "project_ignore.json", StringComparison.OrdinalIgnoreCase))
                .Where(x => !ignoredSet.Contains(x))
                .Where(x => !x.EndsWith(".pack", StringComparison.OrdinalIgnoreCase))
                .ToList();

            var packFiles = reloadedPack.GetAllFiles().Keys.ToList();
            Assert.That(diskFiles, Is.EquivalentTo(packFiles));

            // Close + reopen project; ignore list should persist in project_ignore.json
            projectContainer.Dispose();
            var reopened = new SystemFolderContainer(_tempDir, fileSystem, watcher.Object, eventHub.Object);
            Assert.That(reopened.PackFileSettings.IgnoredFilesWhenSerializing, Does.Contain(ignoredPath));
            reopened.Dispose();
        }

        [Test]
        public void ComplexFlow_CreateProjectFromPack_EditDeleteIgnore_Save_VerifyContent()
        {
            // Arrange: load source pack and create project folder by extracting files
            var sourcePackPath = GetDataFileFromWorkspace("Karl_and_celestialgeneral.pack");
            PackFileContainer sourcePack;
            using (var fs = File.OpenRead(sourcePackPath))
            using (var reader = new BinaryReader(fs))
            {
                sourcePack = PackFileSerializerLoader.Load(sourcePackPath, fs.Length, reader, new CustomPackDuplicateFileResolver());
            }

            var projectDir = Path.Combine(_tempDir, "project_from_pack");
            Directory.CreateDirectory(projectDir);
            foreach (var (relativePath, packFile) in sourcePack.GetAllFiles())
            {
                var absolutePath = Path.Combine(projectDir, relativePath);
                Directory.CreateDirectory(Path.GetDirectoryName(absolutePath)!);
                File.WriteAllBytes(absolutePath, packFile.DataSource.ReadData());
            }

            var eventHub = new Mock<IGlobalEventHub>();
            var fileSystem = CreateRealFileSystemAccess();
            var watcher = new Mock<IFileSystemWatcher>();
            var projectContainer = new SystemFolderContainer(projectDir, fileSystem, watcher.Object, eventHub.Object);
            projectContainer.PackFileSettings.GameVersion = GameTypeEnum.Warhammer3;

            var pfs = new PackFileService(eventHub.Object);
            pfs.MessageBoxProvider = new Mock<ISimpleMessageBox>().Object;
            pfs.EnforceGameFilesMustBeLoaded = false;
            pfs.AddContainer(projectContainer, true);

            var candidatePaths = projectContainer.GetAllFiles().Keys.OrderBy(x => x).Take(3).ToList();
            Assert.That(candidatePaths.Count, Is.EqualTo(3), "Project should contain at least 3 files");

            var editedPath = candidatePaths[0];
            var deletedPath = candidatePaths[1];
            var ignoredPath = candidatePaths[2];

            // Edit one file
            var editedFile = projectContainer.FindFile(editedPath)!;
            var editedBytes = System.Text.Encoding.UTF8.GetBytes("edited-from-integration-flow");
            projectContainer.SaveFileData(editedFile, editedBytes);

            // Delete one file
            var fileToDelete = projectContainer.FindFile(deletedPath)!;
            pfs.DeleteFile(projectContainer, fileToDelete);

            // Ignore one existing file
            projectContainer.PackFileSettings.IgnoredFilesWhenSerializing.Add(ignoredPath);

            // Save
            var outputPackPath = Path.Combine(_tempDir, "project_from_pack_output.pack");
            var gameInfo = GameInformationDatabase.GetGameById(GameTypeEnum.Rome2); // overridden by settings
            pfs.SavePackContainer(projectContainer, outputPackPath, false, gameInfo);

            // Reload and verify
            using var outFs = File.OpenRead(outputPackPath);
            using var outReader = new BinaryReader(outFs);
            var loaded = PackFileSerializerLoader.Load(outputPackPath, outFs.Length, outReader, new CaPackDuplicateFileResolver());

            var expectedVersion = GameInformationDatabase.GetGameById(GameTypeEnum.Warhammer3).PackFileVersion;
            Assert.That(loaded.Header.Version, Is.EqualTo(expectedVersion));

            Assert.That(loaded.ContainsFile(deletedPath), Is.False, "Deleted file should not be in saved pack");
            Assert.That(loaded.ContainsFile(ignoredPath), Is.False, "Ignored file should not be in saved pack");
            Assert.That(loaded.ContainsFile(editedPath), Is.True, "Edited file should still be present");

            var loadedEditedBytes = loaded.FindFile(editedPath)!.DataSource.ReadData();
            Assert.That(loadedEditedBytes, Is.EqualTo(editedBytes));
        }

        [Test]
        public void IgnoreList_PathNormalization_MixedCaseAndSlashDirection_AllIgnoredAtSave()
        {
            // Arrange
            var eventHub = new Mock<IGlobalEventHub>();
            var fileSystem = CreateRealFileSystemAccess();
            var watcher = new Mock<IFileSystemWatcher>();
            var container = new SystemFolderContainer(_tempDir, fileSystem, watcher.Object, eventHub.Object);

            var pfs = new PackFileService(eventHub.Object);
            pfs.MessageBoxProvider = new Mock<ISimpleMessageBox>().Object;
            pfs.EnforceGameFilesMustBeLoaded = false;
            pfs.AddContainer(container, true);

            pfs.AddFilesToPack(container,
            [
                new NewPackFileEntry("FolderA", PackFile.CreateFromASCII("MixedCase.TXT", "a")),
                new NewPackFileEntry("FolderB", PackFile.CreateFromASCII("slash_file.bin", "b")),
                new NewPackFileEntry("FolderC", PackFile.CreateFromASCII("lead.txt", "c")),
                new NewPackFileEntry("FolderD", PackFile.CreateFromASCII("keep.txt", "keep"))
            ]);

            // Intentionally use mixed separators/casing/leading slashes
            container.PackFileSettings.IgnoredFilesWhenSerializing.Add(@"FOLDERA/MIXEDCASE.TXT");
            container.PackFileSettings.IgnoredFilesWhenSerializing.Add(@"FolderB/slash_file.bin");
            container.PackFileSettings.IgnoredFilesWhenSerializing.Add(@"FOLDERC/LEAD.TXT");

            // Act
            var outputPackPath = Path.Combine(_tempDir, "ignore_normalization.pack");
            var gameInfo = GameInformationDatabase.GetGameById(GameTypeEnum.Warhammer3);
            pfs.SavePackContainer(container, outputPackPath, false, gameInfo);

            using var fs = File.OpenRead(outputPackPath);
            using var reader = new BinaryReader(fs);
            var loaded = PackFileSerializerLoader.Load(outputPackPath, fs.Length, reader, new CaPackDuplicateFileResolver());

            // Assert
            Assert.That(loaded.ContainsFile(@"foldera\mixedcase.txt"), Is.False);
            Assert.That(loaded.ContainsFile(@"folderb\slash_file.bin"), Is.False);
            Assert.That(loaded.ContainsFile(@"folderc\lead.txt"), Is.False);
            Assert.That(loaded.ContainsFile(@"folderd\keep.txt"), Is.True);
            Assert.That(loaded.GetFileCount(), Is.EqualTo(1));
        }

        [Test]
        public void SaveFlow_UsesSaveLocationPathValueAsTarget_WhenProvidedByCaller()
        {
            // Arrange
            var seedPath = Path.Combine(_tempDir, "data", "seed.txt");
            Directory.CreateDirectory(Path.GetDirectoryName(seedPath)!);
            File.WriteAllText(seedPath, "seed-data");

            var eventHub = new Mock<IGlobalEventHub>();
            var fileSystem = CreateRealFileSystemAccess();
            var watcher = new Mock<IFileSystemWatcher>();
            var container = new SystemFolderContainer(_tempDir, fileSystem, watcher.Object, eventHub.Object);
            container.PackFileSettings.GameVersion = GameTypeEnum.Warhammer3;

            var saveDir = Path.Combine(_tempDir, "save-target");
            Directory.CreateDirectory(saveDir);
            var overridePackPath = Path.Combine(saveDir, "override_location.pack");
            container.PackFileSettings.SaveLocationPath = overridePackPath;

            var pfs = new PackFileService(eventHub.Object);
            pfs.MessageBoxProvider = new Mock<ISimpleMessageBox>().Object;
            pfs.EnforceGameFilesMustBeLoaded = false;
            pfs.AddContainer(container, true);

            // Act: emulate caller honoring SaveLocationPath
            var gameInfo = GameInformationDatabase.GetGameById(GameTypeEnum.Rome2); // overridden by settings game version
            var targetPath = container.PackFileSettings.SaveLocationPath!;
            pfs.SavePackContainer(container, targetPath, false, gameInfo);

            // Assert: pack written to override path and source folder still represents project files
            Assert.That(File.Exists(overridePackPath), Is.True);
            Assert.That(File.Exists(seedPath), Is.True, "Saving a pack should not mutate source project files.");

            using var fs = File.OpenRead(overridePackPath);
            using var reader = new BinaryReader(fs);
            var loaded = PackFileSerializerLoader.Load(overridePackPath, fs.Length, reader, new CaPackDuplicateFileResolver());

            Assert.That(loaded.ContainsFile(@"data\seed.txt"), Is.True);
            var expectedVersion = GameInformationDatabase.GetGameById(GameTypeEnum.Warhammer3).PackFileVersion;
            Assert.That(loaded.Header.Version, Is.EqualTo(expectedVersion));
        }

        [Test]
        public void SaveToDisk_FileLockedInProject_ThrowsButContainerRemainsUsable()
        {
            // Arrange
            var lockPath = Path.Combine(_tempDir, "locked", "file.txt");
            Directory.CreateDirectory(Path.GetDirectoryName(lockPath)!);
            File.WriteAllText(lockPath, "locked-content");

            var eventHub = new Mock<IGlobalEventHub>();
            var fileSystem = CreateRealFileSystemAccess();
            var watcher = new Mock<IFileSystemWatcher>();
            var container = new SystemFolderContainer(_tempDir, fileSystem, watcher.Object, eventHub.Object);

            var pfs = new PackFileService(eventHub.Object);
            pfs.MessageBoxProvider = new Mock<ISimpleMessageBox>().Object;
            pfs.EnforceGameFilesMustBeLoaded = false;
            pfs.AddContainer(container, true);

            var outputPackPath = Path.Combine(_tempDir, "locked_output.pack");
            var gameInfo = GameInformationDatabase.GetGameById(GameTypeEnum.Warhammer3);
            var beforeCount = container.GetFileCount();

            // Hold an exclusive lock on one project file during save
            using (new FileStream(lockPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            {
                Assert.Throws<IOException>(() => pfs.SavePackContainer(container, outputPackPath, false, gameInfo));
            }

            // Assert: no state corruption and save succeeds once lock is released
            Assert.That(container.GetFileCount(), Is.EqualTo(beforeCount));
            Assert.That(container.ContainsFile(@"locked\file.txt"), Is.True);

            pfs.SavePackContainer(container, outputPackPath, false, gameInfo);
            Assert.That(File.Exists(outputPackPath), Is.True);

            using var fs = File.OpenRead(outputPackPath);
            using var reader = new BinaryReader(fs);
            var loaded = PackFileSerializerLoader.Load(outputPackPath, fs.Length, reader, new CaPackDuplicateFileResolver());
            Assert.That(loaded.ContainsFile(@"locked\file.txt"), Is.True);
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

        [Test]
        public void CopyFileFromOtherPackFile_PublishesCorrectReference()
        {
            // Arrange: create a SystemFolderContainer as target
            var eventHub = new Mock<IGlobalEventHub>();
            var fileSystem = CreateRealFileSystemAccess();
            var mockWatcher = new Mock<IFileSystemWatcher>();

            File.WriteAllText(Path.Combine(_tempDir, "existing.txt"), "existing");
            var target = new SystemFolderContainer(_tempDir, fileSystem, mockWatcher.Object, eventHub.Object);

            // Create a source PackFileContainer
            var source = PackFileContainer.CreatePackFile("source");
            source.AddOrUpdateFile(@"scripts\new_script.lua", new PackFile("new_script.lua", new MemorySource("lua content"u8.ToArray())));

            var pfs = new PackFileService(eventHub.Object);
            pfs.MessageBoxProvider = new Mock<ISimpleMessageBox>().Object;
            pfs.EnforceGameFilesMustBeLoaded = false;
            pfs.AddContainer(source);
            pfs.AddContainer(target);

            // Act: copy from source to target
            pfs.CopyFileFromOtherPackFile(source, @"scripts\new_script.lua", target);

            // Assert: the event should publish the reference that's actually stored in the container
            eventHub.Verify(x => x.PublishGlobalEvent(It.Is<PackFileContainerFilesAddedEvent>(e =>
                e.Container == target &&
                e.AddedFiles.Count == 1 &&
                ReferenceEquals(e.AddedFiles[0], target.FindFile(@"scripts\new_script.lua"))
            )), Times.Once);

            // The stored file should be a FileSystemSource (written to disk)
            var storedFile = target.FindFile(@"scripts\new_script.lua")!;
            Assert.That(storedFile.DataSource, Is.InstanceOf<FileSystemSource>());
            Assert.That(storedFile.DataSource.ReadData(), Is.EqualTo("lua content"u8.ToArray()));
        }

        [Test]
        public void CopyFileFromOtherPackFile_FileIncludedInSavedPack()
        {
            // Arrange
            var eventHub = new Mock<IGlobalEventHub>();
            var fileSystem = CreateRealFileSystemAccess();
            var mockWatcher = new Mock<IFileSystemWatcher>();

            File.WriteAllText(Path.Combine(_tempDir, "initial.txt"), "initial");
            var target = new SystemFolderContainer(_tempDir, fileSystem, mockWatcher.Object, eventHub.Object);

            var source = PackFileContainer.CreatePackFile("source"); 
            source.AddOrUpdateFile(@"data\added_file.bin", new PackFile("added_file.bin", new MemorySource(new byte[] { 1, 2, 3, 4 })));

            var pfs = new PackFileService(eventHub.Object);
            pfs.MessageBoxProvider = new Mock<ISimpleMessageBox>().Object;
            pfs.EnforceGameFilesMustBeLoaded = false;
            pfs.AddContainer(source);
            pfs.AddContainer(target);

            // Act: copy then save
            pfs.CopyFileFromOtherPackFile(source, @"data\added_file.bin", target);

            var packPath = Path.Combine(_tempDir, "output.pack");
            var gameInfo = GameInformationDatabase.GetGameById(GameTypeEnum.Warhammer3);
            pfs.SavePackContainer(target, packPath, false, gameInfo);

            // Assert: load the saved pack and verify both files are present
            using var fs = File.OpenRead(packPath);
            using var reader = new BinaryReader(fs);
            var loaded = PackFileSerializerLoader.Load(packPath, fs.Length, reader, new CaPackDuplicateFileResolver());

            Assert.That(loaded.GetFileCount(), Is.EqualTo(2));
            Assert.That(loaded.FindFile(@"initial.txt"), Is.Not.Null);
            Assert.That(loaded.FindFile(@"data\added_file.bin"), Is.Not.Null);

            var loadedData = loaded.FindFile(@"data\added_file.bin")!.DataSource.ReadData();
            Assert.That(loadedData, Is.EqualTo(new byte[] { 1, 2, 3, 4 }));
        }

        [Test]
        public void SaveToDisk_SortOrder_MatchesPackFileContainer()
        {
            // Arrange: create identical file sets in both container types
            var filePaths = new[]
            {
                @"animations\battle\humanoid01.anim",
                @"animations\skeletons\humanoid01.bone",
                @"db\units_tables\data",
                @"scripts\campaign\main.lua",
                @"variantmeshes\wh_main\hum01.variantmeshdefinition",
            };

            var eventHub = new Mock<IGlobalEventHub>();
            var fileSystem = CreateRealFileSystemAccess();
            var mockWatcher = new Mock<IFileSystemWatcher>();

            // Create files on disk for SystemFolderContainer
            foreach (var path in filePaths)
            {
                var absolutePath = Path.Combine(_tempDir, path);
                Directory.CreateDirectory(Path.GetDirectoryName(absolutePath)!);
                File.WriteAllText(absolutePath, $"content of {path}");
            }

            var sysContainer = new SystemFolderContainer(_tempDir, fileSystem, mockWatcher.Object, eventHub.Object);

            // Create equivalent PackFileContainer
            var packContainer = PackFileContainer.CreatePackFile("test", "test.pack");
            foreach (var path in filePaths)
                packContainer.AddOrUpdateFile(path, PackFile.CreateFromASCII(Path.GetFileName(path), $"content of {path}"));

            var gameInfo = GameInformationDatabase.GetGameById(GameTypeEnum.Warhammer3);

            // Save SystemFolderContainer
            var sysPackPath = Path.Combine(_tempDir, "sys_output.pack");
            var pfs = new PackFileService(eventHub.Object);
            pfs.MessageBoxProvider = new Mock<ISimpleMessageBox>().Object;
            pfs.EnforceGameFilesMustBeLoaded = false;
            pfs.AddContainer(sysContainer);
            pfs.SavePackContainer(sysContainer, sysPackPath, false, gameInfo);

            // Save PackFileContainer
            var normalPackPath = Path.Combine(_tempDir, "normal_output.pack");
            pfs.AddContainer(packContainer);
            pfs.SavePackContainer(packContainer, normalPackPath, false, gameInfo);

            // Load both and compare file order
            using var sysFs = File.OpenRead(sysPackPath);
            using var sysReader = new BinaryReader(sysFs);
            var loadedSys = PackFileSerializerLoader.Load(sysPackPath, sysFs.Length, sysReader, new CaPackDuplicateFileResolver());

            using var normFs = File.OpenRead(normalPackPath);
            using var normReader = new BinaryReader(normFs);
            var loadedNorm = PackFileSerializerLoader.Load(normalPackPath, normFs.Length, normReader, new CaPackDuplicateFileResolver());

            // Both should have the same files in the same order
            var sysKeys = loadedSys.GetAllFiles().Keys.ToList();
            var normKeys = loadedNorm.GetAllFiles().Keys.ToList();

            Assert.That(sysKeys.Count, Is.EqualTo(normKeys.Count), "File count mismatch");
            for (var i = 0; i < sysKeys.Count; i++)
                Assert.That(sysKeys[i], Is.EqualTo(normKeys[i]), $"File order mismatch at index {i}");
        }

        // ──────────────────────────────────────────────────────────────────────
        // B6 — Sort-order consistency between GetDirectoryContent, SearchFiles and
        //      the saved .pack. All three must use the same ordering so the tree,
        //      search results and persisted pack agree. Uses names that sort
        //      differently under ordinal vs current-culture rules (underscore,
        //      mixed case, digits).
        // ──────────────────────────────────────────────────────────────────────

        [Test]
        public void B6_GetDirectoryContent_SortOrder_IsOrdinal_AndConsistentWithSearchAndSavedPack()
        {
            // Arrange: root-level files whose ordinal and culture orderings differ
            var rootFiles = new[]
            {
                "Zebra.txt",
                "apple.txt",
                "_underscore.txt",
                "File1.txt",
                "file10.txt",
                "file2.txt",
            };

            foreach (var name in rootFiles)
                File.WriteAllText(Path.Combine(_tempDir, name), $"content of {name}");

            var eventHub = new Mock<IGlobalEventHub>();
            var fileSystem = CreateRealFileSystemAccess();
            var mockWatcher = new Mock<IFileSystemWatcher>();
            var container = new SystemFolderContainer(_tempDir, fileSystem, mockWatcher.Object, eventHub.Object);

            // Expected ordering: ordinal, matching PackFileSortHelper.PathComparer / serializer
            var expectedOrder = rootFiles
                .Select(n => n.ToLowerInvariant())
                .OrderBy(n => n, StringComparer.Ordinal)
                .ToList();

            // Act
            var directoryOrder = container.GetDirectoryContent("").Select(x => x.Path).ToList();
            var searchOrder = container.SearchFiles(null, null).Select(x => x.Path).ToList();

            // Assert: GetDirectoryContent matches the ordinal expectation ...
            Assert.That(directoryOrder, Is.EqualTo(expectedOrder),
                "GetDirectoryContent should sort using ordinal rules (consistent with the saved pack).");

            // ... and is consistent with SearchFiles
            Assert.That(directoryOrder, Is.EqualTo(searchOrder),
                "GetDirectoryContent and SearchFiles must produce the same ordering.");

            // ... and with the persisted pack
            var pfs = new PackFileService(eventHub.Object);
            pfs.MessageBoxProvider = new Mock<ISimpleMessageBox>().Object;
            pfs.EnforceGameFilesMustBeLoaded = false;
            pfs.AddContainer(container);

            var packPath = Path.Combine(_tempDir, "sorted_output.pack");
            var gameInfo = GameInformationDatabase.GetGameById(GameTypeEnum.Warhammer3);
            pfs.SavePackContainer(container, packPath, false, gameInfo);

            using var fs = File.OpenRead(packPath);
            using var reader = new BinaryReader(fs);
            var loaded = PackFileSerializerLoader.Load(packPath, fs.Length, reader, new CaPackDuplicateFileResolver());
            var savedOrder = loaded.GetAllFiles().Keys.ToList();

            Assert.That(directoryOrder, Is.EqualTo(savedOrder),
                "GetDirectoryContent ordering must match the order files are written into the .pack.");
        }

        // ──────────────────────────────────────────────────────────────────────
        // B7 — DeleteFolder with empty / whitespace input must NOT delete the
        //      entire source folder. Guards against the destructive root-delete.
        // ──────────────────────────────────────────────────────────────────────

        [Test]
        public void B7_DeleteFolder_EmptyOrWhitespace_DoesNotWipeSourceFolder()
        {
            // Arrange: seed files at root and in a subfolder
            File.WriteAllText(Path.Combine(_tempDir, "keep.txt"), "keep");
            var subDir = Path.Combine(_tempDir, "sub");
            Directory.CreateDirectory(subDir);
            File.WriteAllText(Path.Combine(subDir, "nested.txt"), "nested");

            var eventHub = new Mock<IGlobalEventHub>();
            var fileSystem = CreateRealFileSystemAccess();
            var mockWatcher = new Mock<IFileSystemWatcher>();
            var container = new SystemFolderContainer(_tempDir, fileSystem, mockWatcher.Object, eventHub.Object);

            Assert.That(container.GetFileCount(), Is.EqualTo(2));

            // Act: attempt to delete an empty / whitespace folder
            foreach (var input in new[] { "", "   ", "\\" })
                container.DeleteFolder(input);

            // Assert: source folder and all files are intact on disk and in the container
            Assert.That(Directory.Exists(_tempDir), Is.True, "Source folder must not be deleted.");
            Assert.That(File.Exists(Path.Combine(_tempDir, "keep.txt")), Is.True, "Root file must survive.");
            Assert.That(File.Exists(Path.Combine(subDir, "nested.txt")), Is.True, "Nested file must survive.");
            Assert.That(container.GetFileCount(), Is.EqualTo(2), "Container must still track both files.");
        }

        // ──────────────────────────────────────────────────────────────────────
        // B15 — Path edge cases: spaces, mixed case, deep nesting. Add / rename /
        //       move / delete and save must round-trip; lookups stay case-insensitive.
        // ──────────────────────────────────────────────────────────────────────

        [Test]
        public void B15_PathEdgeCases_SpacesMixedCaseAndDeepNesting_RoundTrip()
        {
            var eventHub = new Mock<IGlobalEventHub>();
            var fileSystem = CreateRealFileSystemAccess();
            var mockWatcher = new Mock<IFileSystemWatcher>();
            var container = new SystemFolderContainer(_tempDir, fileSystem, mockWatcher.Object, eventHub.Object);

            var pfs = new PackFileService(eventHub.Object);
            pfs.MessageBoxProvider = new Mock<ISimpleMessageBox>().Object;
            pfs.EnforceGameFilesMustBeLoaded = false;
            pfs.AddContainer(container);

            // Add a file into a folder with spaces and mixed case
            var spaced = PackFile.CreateFromASCII("Mixed Case.TXT", "spaced content");
            pfs.AddFilesToPack(container, [new NewPackFileEntry(@"My Folder", spaced)]);

            // Add a deeply nested file
            var deep = PackFile.CreateFromASCII("leaf.bin", "deep content");
            pfs.AddFilesToPack(container, [new NewPackFileEntry(@"a\b\c\d\e\f", deep)]);

            // Lookups are case-insensitive (paths normalize to lower case)
            Assert.That(container.ContainsFile(@"my folder\mixed case.txt"), Is.True);
            Assert.That(container.ContainsFile(@"MY FOLDER\MIXED CASE.TXT"), Is.True);
            Assert.That(container.ContainsFile(@"a\b\c\d\e\f\leaf.bin"), Is.True);

            // Disk reflects the paths
            Assert.That(File.Exists(Path.Combine(_tempDir, "My Folder", "Mixed Case.TXT")), Is.True);
            Assert.That(File.Exists(Path.Combine(_tempDir, "a", "b", "c", "d", "e", "f", "leaf.bin")), Is.True);

            // Rename the spaced file
            var toRename = container.FindFile(@"my folder\mixed case.txt")!;
            pfs.RenameFile(container, toRename, "Renamed File.txt");
            Assert.That(container.ContainsFile(@"my folder\renamed file.txt"), Is.True);
            Assert.That(File.Exists(Path.Combine(_tempDir, "My Folder", "Renamed File.txt")), Is.True);

            // Save and reload — structure and content preserved
            var packPath = Path.Combine(_tempDir, "edgecases.pack");
            var gameInfo = GameInformationDatabase.GetGameById(GameTypeEnum.Warhammer3);
            pfs.SavePackContainer(container, packPath, false, gameInfo);

            using var fs = File.OpenRead(packPath);
            using var reader = new BinaryReader(fs);
            var loaded = PackFileSerializerLoader.Load(packPath, fs.Length, reader, new CaPackDuplicateFileResolver());

            Assert.That(loaded.FindFile(@"my folder\renamed file.txt"), Is.Not.Null);
            Assert.That(loaded.FindFile(@"a\b\c\d\e\f\leaf.bin"), Is.Not.Null);

            var deepData = loaded.FindFile(@"a\b\c\d\e\f\leaf.bin")!.DataSource.ReadData();
            Assert.That(System.Text.Encoding.ASCII.GetString(deepData), Is.EqualTo("deep content"));

            container.Dispose();
        }
    }
}
