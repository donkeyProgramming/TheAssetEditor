// Integration tests for SystemFolderContainer + PackFileBrowserViewModel.
//
// Validates that the tree view stays in sync with the container and the file system
// across all mutation scenarios:
//   - Copying files from another pack into the SystemFolderContainer
//   - Adding/deleting/renaming files and folders via the application (PackFileService)
//   - Adding/deleting/renaming files and folders externally on disk (FileSystemWatcher)
//   - Saving the container to a .pack file and reloading it
//
// Each test asserts three things:
//   1. Container state (in-memory _fileList)
//   2. Disk state (actual files on the filesystem)
//   3. Tree view state (PackFileBrowserViewModel nodes)

using System.IO;
using AssetEditor.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Shared.Core.Events;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.PackFiles.Models.Containers;
using Shared.Core.PackFiles.Models.FileSources;
using Shared.Core.PackFiles.Serialization;
using Shared.Core.PackFiles.Utility;
using Shared.Core.Services;
using Shared.Core.Settings;
using Shared.Ui.BaseDialogs.PackFileTree;
using Test.TestingUtility.Shared;
using Test.TestingUtility.TestUtility;
using Shared.UiTest.BaseDialogs.PackFileTree.Utility;

namespace Shared.UiTest.BaseDialogs.PackFileTree
{
    [TestFixture]
    internal class SystemFolderContainer_PackFileBrowserIntegrationTests
    {
        private readonly string _inputPackFileKarl = PathHelper.GetDataFolder("Data\\Karl_and_celestialgeneral_Pack");

        private AssetEditorTestRunner _runner = null!;
        private IPackFileService _packFileService = null!;
        private PackFileBrowserViewModel _viewModel = null!;
        private string _tempDir = null!;
        private SystemFolderContainer _container = null!;
        private Mock<IFileSystemWatcher> _mockWatcher = null!;
        private RootTreeNode _rootNode = null!;

        [SetUp]
        public void Setup()
        {
            _runner = new AssetEditorTestRunner();
            _runner.PackFileService.EnforceGameFilesMustBeLoaded = false;
            _packFileService = _runner.PackFileService;

            var mainApplicationView = _runner.ServiceProvider.GetRequiredService<MainViewModel>();
            _viewModel = mainApplicationView.FileTree;

            _tempDir = Path.Combine(Path.GetTempPath(), "SysFolderBrowserInteg_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDir);
        }

        [TearDown]
        public void TearDown()
        {
            _viewModel.Dispose();
            _container?.Dispose();
            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, true);
        }

        // ──────────────────────────────────────────────────────────────────────
        // Step 1: Load Karl pack, copy files into SystemFolderContainer, verify
        // ──────────────────────────────────────────────────────────────────────

        [Test]
        public void Step01_LoadKarlPack_CopyToSystemFolderContainer_VerifyTreeAndDisk()
        {
            // Load Karl pack as source
            var karlContainer = _runner.LoadFolderPackFile(_inputPackFileKarl);

            // Create the SystemFolderContainer
            CreateSystemFolderContainer();

            // Copy a selection of files from Karl into the SystemFolderContainer
            var filesToCopy = new[]
            {
                @"animations\skeletons\humanoid01.anim",
                @"animations\skeletons\humanoid01.bone_inv_trans_mats",
                @"variantmeshes\wh_variantmodels\hu1\emp\emp_karl_franz\emp_karl_franz.rigid_model_v2"
            };

            var newFileEntries = new List<NewPackFileEntry>();
            foreach (var filePath in filesToCopy)
            {
                var sourceFile = karlContainer.FindFile(filePath);
                Assert.That(sourceFile, Is.Not.Null, $"Source file '{filePath}' must exist in Karl pack");

                var data = sourceFile!.DataSource.ReadData();
                var directory = Path.GetDirectoryName(filePath)!.Replace('/', '\\');
                var newFile = new PackFile(sourceFile.Name, new MemorySource(data));
                newFileEntries.Add(new NewPackFileEntry(directory, newFile));
            }

            _packFileService.AddFilesToPack(_container, newFileEntries);

            // Verify container state
            Assert.That(_container.GetFileCount(), Is.EqualTo(3));
            foreach (var filePath in filesToCopy)
                Assert.That(_container.ContainsFile(filePath), Is.True, $"Container should contain '{filePath}'");

            // Verify disk state
            foreach (var filePath in filesToCopy)
            {
                var diskPath = Path.Combine(_tempDir, filePath);
                Assert.That(File.Exists(diskPath), Is.True, $"File should exist on disk: '{diskPath}'");
            }

            // Verify tree view
            VerifyTreeNodeExists(@"animations\skeletons\humanoid01.anim");
            VerifyTreeNodeExists(@"animations\skeletons\humanoid01.bone_inv_trans_mats");
            VerifyTreeNodeExists(@"variantmeshes\wh_variantmodels\hu1\emp\emp_karl_franz\emp_karl_franz.rigid_model_v2");
        }

        // ──────────────────────────────────────────────────────────────────────
        // Step 2: Add a file on disk (external)
        // ──────────────────────────────────────────────────────────────────────

        [Test]
        public void Step02_AddFileOnDisk_VerifyTreeAndContainer()
        {
            CreateSystemFolderContainer();
            SeedFile(@"models\unit.txt", "unit data");

            // Simulate external file creation on disk
            var newFilePath = Path.Combine(_tempDir, "scripts", "campaign.lua");
            Directory.CreateDirectory(Path.GetDirectoryName(newFilePath)!);
            File.WriteAllText(newFilePath, "-- campaign script");

            // Raise watcher event
            _mockWatcher.Raise(w => w.Created += null, new FileSystemEventArgs(WatcherChangeTypes.Created, Path.Combine(_tempDir, "scripts"), "campaign.lua"));
            _container.ProcessPendingEvents(null);

            // Verify container
            Assert.That(_container.ContainsFile(@"scripts\campaign.lua"), Is.True);
            Assert.That(_container.GetFileCount(), Is.EqualTo(2));

            // Verify tree view
            VerifyTreeNodeExists(@"scripts\campaign.lua");
            VerifyTreeNodeExists(@"models\unit.txt");
        }

        // ──────────────────────────────────────────────────────────────────────
        // Step 3: Add a file in the application
        // ──────────────────────────────────────────────────────────────────────

        [Test]
        public void Step03_AddFileInApplication_VerifyTreeAndDisk()
        {
            CreateSystemFolderContainer();
            SeedFile(@"existing.txt", "existing");

            // Add file through PackFileService
            var newFile = PackFile.CreateFromASCII("newfile.bin", "binary content here");
            _packFileService.AddFilesToPack(_container, [new NewPackFileEntry(@"data\subfolder", newFile)]);

            // Verify container
            Assert.That(_container.ContainsFile(@"data\subfolder\newfile.bin"), Is.True);
            Assert.That(_container.GetFileCount(), Is.EqualTo(2));

            // Verify disk
            var diskPath = Path.Combine(_tempDir, "data", "subfolder", "newfile.bin");
            Assert.That(File.Exists(diskPath), Is.True);

            // Verify tree view
            VerifyTreeNodeExists(@"data\subfolder\newfile.bin");
            VerifyTreeNodeExists(@"existing.txt");
        }

        // ──────────────────────────────────────────────────────────────────────
        // Step 4: Add a folder with content on disk (external)
        // ──────────────────────────────────────────────────────────────────────

        [Test]
        public void Step04_AddFolderWithContentOnDisk_VerifyTreeAndContainer()
        {
            CreateSystemFolderContainer();
            SeedFile(@"base.txt", "base");

            // Create a folder with multiple files on disk
            var newFolder = Path.Combine(_tempDir, "imported_textures");
            Directory.CreateDirectory(newFolder);
            File.WriteAllText(Path.Combine(newFolder, "diffuse.dds"), "diffuse tex");
            File.WriteAllText(Path.Combine(newFolder, "normal.dds"), "normal tex");

            // Watcher fires a Created event for the directory
            _mockWatcher.Raise(w => w.Created += null, new FileSystemEventArgs(WatcherChangeTypes.Created, _tempDir, "imported_textures"));
            _container.ProcessPendingEvents(null);

            // Verify container
            Assert.That(_container.ContainsFile(@"imported_textures\diffuse.dds"), Is.True);
            Assert.That(_container.ContainsFile(@"imported_textures\normal.dds"), Is.True);
            Assert.That(_container.GetFileCount(), Is.EqualTo(3));

            // Verify tree view
            VerifyTreeNodeExists(@"imported_textures\diffuse.dds");
            VerifyTreeNodeExists(@"imported_textures\normal.dds");
        }

        // ──────────────────────────────────────────────────────────────────────
        // Step 5: Add a folder with content in the application
        // ──────────────────────────────────────────────────────────────────────

        [Test]
        public void Step05_AddFolderWithContentInApplication_VerifyTreeAndDisk()
        {
            CreateSystemFolderContainer();
            SeedFile(@"root.txt", "root");

            // Add multiple files to a new folder via the service
            var file1 = PackFile.CreateFromASCII("model.rmv2", "mesh data");
            var file2 = PackFile.CreateFromASCII("texture.dds", "tex data");
            _packFileService.AddFilesToPack(_container, [
                new NewPackFileEntry(@"units\empire", file1),
                new NewPackFileEntry(@"units\empire", file2)
            ]);

            // Verify container
            Assert.That(_container.ContainsFile(@"units\empire\model.rmv2"), Is.True);
            Assert.That(_container.ContainsFile(@"units\empire\texture.dds"), Is.True);
            Assert.That(_container.GetFileCount(), Is.EqualTo(3));

            // Verify disk
            Assert.That(File.Exists(Path.Combine(_tempDir, "units", "empire", "model.rmv2")), Is.True);
            Assert.That(File.Exists(Path.Combine(_tempDir, "units", "empire", "texture.dds")), Is.True);

            // Verify tree view
            VerifyTreeNodeExists(@"units\empire\model.rmv2");
            VerifyTreeNodeExists(@"units\empire\texture.dds");
        }

        // ──────────────────────────────────────────────────────────────────────
        // Step 6: Delete a file on disk (external)
        // ──────────────────────────────────────────────────────────────────────

        [Test]
        public void Step06_DeleteFileOnDisk_VerifyTreeAndContainer()
        {
            CreateSystemFolderContainer();
            SeedFile(@"scripts\main.lua", "main");
            SeedFile(@"scripts\utils.lua", "utils");

            // Delete on disk
            File.Delete(Path.Combine(_tempDir, "scripts", "main.lua"));

            // Raise watcher event
            _mockWatcher.Raise(w => w.Deleted += null, new FileSystemEventArgs(WatcherChangeTypes.Deleted, Path.Combine(_tempDir, "scripts"), "main.lua"));
            _container.ProcessPendingEvents(null);

            // Verify container
            Assert.That(_container.ContainsFile(@"scripts\main.lua"), Is.False);
            Assert.That(_container.ContainsFile(@"scripts\utils.lua"), Is.True);
            Assert.That(_container.GetFileCount(), Is.EqualTo(1));

            // Verify tree view
            VerifyTreeNodeDoesNotExist(@"scripts\main.lua");
            VerifyTreeNodeExists(@"scripts\utils.lua");
        }

        // ──────────────────────────────────────────────────────────────────────
        // Step 7: Delete a file in the application
        // ──────────────────────────────────────────────────────────────────────

        [Test]
        public void Step07_DeleteFileInApplication_VerifyTreeAndDisk()
        {
            CreateSystemFolderContainer();
            SeedFile(@"data\config.xml", "config");
            SeedFile(@"data\values.xml", "values");

            // Delete via PackFileService
            var fileToDelete = _container.FindFile(@"data\config.xml")!;
            _packFileService.DeleteFile(_container, fileToDelete);

            // Verify container
            Assert.That(_container.ContainsFile(@"data\config.xml"), Is.False);
            Assert.That(_container.ContainsFile(@"data\values.xml"), Is.True);
            Assert.That(_container.GetFileCount(), Is.EqualTo(1));

            // Verify disk
            Assert.That(File.Exists(Path.Combine(_tempDir, "data", "config.xml")), Is.False);
            Assert.That(File.Exists(Path.Combine(_tempDir, "data", "values.xml")), Is.True);

            // Verify tree view
            VerifyTreeNodeDoesNotExist(@"data\config.xml");
            VerifyTreeNodeExists(@"data\values.xml");
        }

        // ──────────────────────────────────────────────────────────────────────
        // Step 8: Delete a folder with content on disk (external)
        // ──────────────────────────────────────────────────────────────────────

        [Test]
        public void Step08_DeleteFolderOnDisk_VerifyTreeAndContainer()
        {
            CreateSystemFolderContainer();
            SeedFile(@"keep.txt", "keep");
            SeedFile(@"removeme\a.txt", "a");
            SeedFile(@"removeme\b.txt", "b");

            // Delete folder from disk
            Directory.Delete(Path.Combine(_tempDir, "removeme"), true);

            // Watcher fires a single Deleted event for the folder
            _mockWatcher.Raise(w => w.Deleted += null, new FileSystemEventArgs(WatcherChangeTypes.Deleted, _tempDir, "removeme"));
            _container.ProcessPendingEvents(null);

            // Verify container
            Assert.That(_container.ContainsFile(@"removeme\a.txt"), Is.False);
            Assert.That(_container.ContainsFile(@"removeme\b.txt"), Is.False);
            Assert.That(_container.ContainsFile(@"keep.txt"), Is.True);
            Assert.That(_container.GetFileCount(), Is.EqualTo(1));

            // Verify tree view
            VerifyTreeNodeDoesNotExist(@"removeme\a.txt");
            VerifyTreeNodeDoesNotExist(@"removeme\b.txt");
            VerifyTreeNodeExists(@"keep.txt");
        }

        // ──────────────────────────────────────────────────────────────────────
        // Step 9: Delete a folder with content in the application
        // ──────────────────────────────────────────────────────────────────────

        [Test]
        public void Step09_DeleteFolderInApplication_VerifyTreeAndDisk()
        {
            CreateSystemFolderContainer();
            SeedFile(@"survive.txt", "survive");
            SeedFile(@"doomed\file1.bin", "f1");
            SeedFile(@"doomed\sub\file2.bin", "f2");

            // Delete folder via PackFileService
            _packFileService.DeleteFolder(_container, "doomed");

            // Verify container
            Assert.That(_container.ContainsFile(@"doomed\file1.bin"), Is.False);
            Assert.That(_container.ContainsFile(@"doomed\sub\file2.bin"), Is.False);
            Assert.That(_container.ContainsFile(@"survive.txt"), Is.True);
            Assert.That(_container.GetFileCount(), Is.EqualTo(1));

            // Verify disk
            Assert.That(Directory.Exists(Path.Combine(_tempDir, "doomed")), Is.False);
            Assert.That(File.Exists(Path.Combine(_tempDir, "survive.txt")), Is.True);

            // Verify tree view
            VerifyTreeNodeDoesNotExist(@"doomed");
            VerifyTreeNodeExists(@"survive.txt");
        }

        // ──────────────────────────────────────────────────────────────────────
        // Step 10: Rename a folder on disk (external)
        // ──────────────────────────────────────────────────────────────────────

        [Test]
        public void Step10_RenameFolderOnDisk_VerifyTreeAndContainer()
        {
            CreateSystemFolderContainer();
            SeedFile(@"myfolder\data.bin", "data");
            SeedFile(@"myfolder\info.txt", "info");

            // Rename on disk
            Directory.Move(Path.Combine(_tempDir, "myfolder"), Path.Combine(_tempDir, "renamed_folder"));

            // Watcher fires a Renamed event for the folder
            var args = new RenamedEventArgs(WatcherChangeTypes.Renamed, _tempDir, "renamed_folder", "myfolder");
            _mockWatcher.Raise(w => w.Renamed += null, args);
            _container.ProcessPendingEvents(null);

            // Verify container
            Assert.That(_container.ContainsFile(@"myfolder\data.bin"), Is.False);
            Assert.That(_container.ContainsFile(@"myfolder\info.txt"), Is.False);
            Assert.That(_container.ContainsFile(@"renamed_folder\data.bin"), Is.True);
            Assert.That(_container.ContainsFile(@"renamed_folder\info.txt"), Is.True);

            // Verify tree view
            VerifyTreeNodeDoesNotExist(@"myfolder\data.bin");
            VerifyTreeNodeExists(@"renamed_folder\data.bin");
            VerifyTreeNodeExists(@"renamed_folder\info.txt");
        }

        // ──────────────────────────────────────────────────────────────────────
        // Step 11: Rename a folder in the application
        // ──────────────────────────────────────────────────────────────────────

        [Test]
        public void Step11_RenameFolderInApplication_VerifyTreeAndDisk()
        {
            CreateSystemFolderContainer();
            SeedFile(@"original\alpha.txt", "alpha");
            SeedFile(@"original\beta.txt", "beta");

            // Rename via PackFileService
            _packFileService.RenameDirectory(_container, "original", "updated");

            // Verify container
            Assert.That(_container.ContainsFile(@"original\alpha.txt"), Is.False);
            Assert.That(_container.ContainsFile(@"updated\alpha.txt"), Is.True);
            Assert.That(_container.ContainsFile(@"updated\beta.txt"), Is.True);

            // Verify disk
            Assert.That(Directory.Exists(Path.Combine(_tempDir, "original")), Is.False);
            Assert.That(File.Exists(Path.Combine(_tempDir, "updated", "alpha.txt")), Is.True);
            Assert.That(File.Exists(Path.Combine(_tempDir, "updated", "beta.txt")), Is.True);

            // Verify tree view — folder renamed event updates tree
            var renamedNode = PackFileBrowserViewModelTestHelper.GetFromPath(_rootNode, "updated");
            Assert.That(renamedNode, Is.Not.Null, "Renamed folder should appear in tree");
            VerifyTreeNodeDoesNotExist(@"original");
        }

        // ──────────────────────────────────────────────────────────────────────
        // Step 12: Rename a file on disk (external)
        // ──────────────────────────────────────────────────────────────────────

        [Test]
        public void Step12_RenameFileOnDisk_VerifyTreeAndContainer()
        {
            CreateSystemFolderContainer();
            SeedFile(@"assets\model.rmv2", "mesh");

            // Rename on disk
            File.Move(Path.Combine(_tempDir, "assets", "model.rmv2"), Path.Combine(_tempDir, "assets", "model_v2.rmv2"));

            // Watcher fires a Renamed event for the file
            var args = new RenamedEventArgs(WatcherChangeTypes.Renamed, Path.Combine(_tempDir, "assets"), "model_v2.rmv2", "model.rmv2");
            _mockWatcher.Raise(w => w.Renamed += null, args);
            _container.ProcessPendingEvents(null);

            // Verify container
            Assert.That(_container.ContainsFile(@"assets\model.rmv2"), Is.False);
            Assert.That(_container.ContainsFile(@"assets\model_v2.rmv2"), Is.True);

            // Verify tree view
            VerifyTreeNodeDoesNotExist(@"assets\model.rmv2");
            VerifyTreeNodeExists(@"assets\model_v2.rmv2");
        }

        // ──────────────────────────────────────────────────────────────────────
        // Step 13: Rename a file in the application
        // ──────────────────────────────────────────────────────────────────────

        [Test]
        public void Step13_RenameFileInApplication_VerifyTreeAndDisk()
        {
            CreateSystemFolderContainer();
            SeedFile(@"textures\diffuse.dds", "tex data");

            // Rename via PackFileService
            var file = _container.FindFile(@"textures\diffuse.dds")!;
            _packFileService.RenameFile(_container, file, "albedo.dds");

            // Verify container
            Assert.That(_container.ContainsFile(@"textures\diffuse.dds"), Is.False);
            Assert.That(_container.ContainsFile(@"textures\albedo.dds"), Is.True);

            // Verify disk
            Assert.That(File.Exists(Path.Combine(_tempDir, "textures", "diffuse.dds")), Is.False);
            Assert.That(File.Exists(Path.Combine(_tempDir, "textures", "albedo.dds")), Is.True);

            // Verify tree view — FilesUpdatedEvent renames the node
            var renamedNode = PackFileBrowserViewModelTestHelper.GetFromPath(_rootNode, @"textures\albedo.dds");
            Assert.That(renamedNode, Is.Not.Null, "Renamed file should appear in tree");
            Assert.That(renamedNode!.Name, Is.EqualTo("albedo.dds"));
            VerifyTreeNodeDoesNotExist(@"textures\diffuse.dds");
        }

        // ──────────────────────────────────────────────────────────────────────
        // Step 14: Save the SystemFolderContainer as .pack, reload and verify
        // ──────────────────────────────────────────────────────────────────────

        [Test]
        public void Step14_SaveToPack_ReloadAndVerify()
        {
            CreateSystemFolderContainer();
            SeedFile(@"animations\idle.anim", "anim data");
            SeedFile(@"models\hero.rmv2", "mesh data");
            SeedFile(@"textures\skin.dds", "tex data");

            // Save to .pack
            var packPath = Path.Combine(_tempDir, "output.pack");
            var gameInfo = GameInformationDatabase.GetGameById(GameTypeEnum.Warhammer3);
            _packFileService.SavePackContainer(_container, packPath, false, gameInfo);

            Assert.That(File.Exists(packPath), Is.True);

            // Load the saved pack and verify
            using var fs = File.OpenRead(packPath);
            using var reader = new BinaryReader(fs);
            var loaded = PackFileSerializerLoader.Load(packPath, fs.Length, reader, new CaPackDuplicateFileResolver());

            Assert.That(loaded.GetFileCount(), Is.EqualTo(3));
            Assert.That(loaded.FindFile(@"animations\idle.anim"), Is.Not.Null);
            Assert.That(loaded.FindFile(@"models\hero.rmv2"), Is.Not.Null);
            Assert.That(loaded.FindFile(@"textures\skin.dds"), Is.Not.Null);

            // Verify data integrity
            var animFile = loaded.FindFile(@"animations\idle.anim")!;
            var animData = animFile.DataSource.ReadData();
            Assert.That(System.Text.Encoding.ASCII.GetString(animData), Is.EqualTo("anim data"));
        }

        // ──────────────────────────────────────────────────────────────────────
        // Helpers
        // ──────────────────────────────────────────────────────────────────────

        private void CreateSystemFolderContainer()
        {
            _mockWatcher = new Mock<IFileSystemWatcher>();
            var fileSystemAccess = new FileSystemAccess();
            var eventHub = _runner.ServiceProvider.GetRequiredService<IGlobalEventHub>();

            _container = new SystemFolderContainer(_tempDir, fileSystemAccess, _mockWatcher.Object, eventHub);
            _packFileService.AddContainer(_container);
            _packFileService.SetEditablePack(_container);

            _rootNode = _viewModel.Files.First(x => x.Owner == _container);
        }

        /// <summary>
        /// Creates a file on disk AND in the container's tracking (by re-creating the container).
        /// This simulates files that already exist when the container is opened.
        /// </summary>
        private void SeedFile(string relativePath, string content)
        {
            var absolutePath = Path.Combine(_tempDir, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(absolutePath)!);
            File.WriteAllText(absolutePath, content);

            // If container already exists, dispose and recreate with new files
            if (_container != null)
            {
                // Unload old container from service
                _packFileService.UnloadPackContainer(_container);

                // Recreate
                var fileSystemAccess = new FileSystemAccess();
                var eventHub = _runner.ServiceProvider.GetRequiredService<IGlobalEventHub>();
                _container = new SystemFolderContainer(_tempDir, fileSystemAccess, _mockWatcher.Object, eventHub);
                _packFileService.AddContainer(_container);
                _packFileService.SetEditablePack(_container);
                _rootNode = _viewModel.Files.First(x => x.Owner == _container);
            }
        }

        private void VerifyTreeNodeExists(string path)
        {
            var node = PackFileBrowserViewModelTestHelper.GetFromPath(_rootNode, path);
            Assert.That(node, Is.Not.Null, $"Tree node should exist at path: '{path}'");
        }

        private void VerifyTreeNodeDoesNotExist(string path)
        {
            var node = PackFileBrowserViewModelTestHelper.GetFromPath(_rootNode, path);
            Assert.That(node, Is.Null, $"Tree node should NOT exist at path: '{path}'");
        }
    }
}
