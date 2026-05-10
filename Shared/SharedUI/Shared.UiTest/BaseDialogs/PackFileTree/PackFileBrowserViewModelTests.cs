// PackFileBrowserViewModel Lazy Loading Architecture:
//
// The tree is built lazily. When a container is added (via PackFileContainerAddedEvent), only
// the root TreeNode is created with a child-loader delegate. Children are NOT loaded from
// the container's GetDirectoryContent() until the node is expanded or explicitly queried.
//
// TreeNode serves as both the data model (BackingChildren) and the WPF-bound node.
// BackingChildren holds the full logical tree once loaded; Children (ObservableCollection) holds
// only the materialized subset visible to WPF. A placeholder child is used so the WPF TreeView
// shows the expand arrow for unloaded directories.
//
// Event-driven updates from PackFileService:
//   - PackFileContainerAddedEvent      ? ReloadTree: creates root node + lazy child loader
//   - PackFileContainerRemovedEvent    ? removes container's root and TreeNode from Files
//   - PackFileContainerFilesAddedEvent ? AddFiles: inserts nodes into loaded branches, creates dirs as needed
//   - PackFileContainerFilesRemovedEvent ? removes nodes for deleted files
//   - PackFileContainerFilesUpdatedEvent ? updates Name/UnsavedChanged on nodes (for rename/save)
//   - PackFileContainerFolderRemovedEvent ? finds and removes folder node and subtree
//   - PackFileContainerFolderRenamedEvent ? finds folder by OLD path, renames leaf, marks unsaved
//   - PackFileContainerSetAsMainEditableEvent ? toggles IsMainEditabelPack on root TreeNodes
//   - PackFileContainerSavedEvent      ? clears UnsavedChanged on loaded nodes only (avoids forcing full population)
//
// SearchFilter: when active, calls EnsureFullyPopulated() to load entire subtree for regex matching.
// When cleared, filter-expanded nodes are absorbed as user expansions and collapsed nodes are unloaded.

using System.Windows.Input;
using System.IO;
using AssetEditor.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Shared.Core.Events;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.Settings;
using Shared.Ui.BaseDialogs.PackFileTree;
using Test.TestingUtility.Shared;
using Test.TestingUtility.TestUtility;
using Shared.UiTest.BaseDialogs.PackFileTree.Utility;

namespace Shared.UiTest.BaseDialogs.PackFileTree
{
    internal class PackFileBrowserViewModelTests
    {
        private readonly string _inputPackFileKarl = PathHelper.GetDataFolder("Data\\Karl_and_celestialgeneral_Pack");

        private AssetEditorTestRunner _runner;
        private IPackFileService _packageFileService;
        private PackFileBrowserViewModel _viewModel;

        [SetUp]
        public void Setup()
        {
            _runner = new AssetEditorTestRunner();
            _runner.PackFileService.EnforceGameFilesMustBeLoaded = false;
            _packageFileService = _runner.PackFileService;

            var mainApplicationView = _runner.ServiceProvider.GetRequiredService<MainViewModel>();
            _viewModel = mainApplicationView.FileTree;
        }

        [TearDown]
        public void Cleanup()
        {
            _viewModel.Dispose();
        }

        [Test]
        public void DragAndDrop_FileInFolder()
        {
            _runner.CreateCaContainer();
            var outputPackFile = _runner.LoadFolderPackFile(_inputPackFileKarl);

            var packRootNode = _viewModel.Files[1];

            // Act
            var fileToMove = PackFileBrowserViewModelTestHelper.GetFromPath(packRootNode, @"animations\battle\humanoid01\2handed_hammer\stand\hu1_2hh_stand_idle_01.anim");
            var destinationNode = PackFileBrowserViewModelTestHelper.GetFromPath(packRootNode, @"animations");
            _viewModel.Drop(fileToMove, destinationNode);

            // Assert
            // Get file in packfileservice
            var movedFile = _runner.PackFileService.FindFile(@"animations\hu1_2hh_stand_idle_01.anim");
            Assert.That(movedFile, Is.Not.Null);

            // Get file node from 
            var movedNode = PackFileBrowserViewModelTestHelper.GetFromPath(packRootNode, @"animations\hu1_2hh_stand_idle_01.anim");
            Assert.That(movedNode, Is.Not.Null);
            Assert.That(movedNode.UnsavedChanged, Is.True);
            Assert.That(movedNode.Parent.UnsavedChanged, Is.True);
        }

        [Test]
        public void OnlyRootExpandedByDefault()
        {
            CreatePackfiles(("folderA\\file1.txt", "file1.txt"), ("folderB\\file2.txt", "file2.txt"));

            Assert.That(_viewModel.Files.Count, Is.EqualTo(1));

            var root = _viewModel.Files[0];
            Assert.That(PackFileBrowserViewModelTestHelper.GetFromPath(root, "foldera"), Is.Null);

            root.IsNodeExpanded = true;

            Assert.That(PackFileBrowserViewModelTestHelper.GetFromPath(root, "foldera"), Is.Not.Null);
            Assert.That(PackFileBrowserViewModelTestHelper.GetFromPath(root, "folderb"), Is.Not.Null);
        }

        [Test]
        public void GetFileFromCollapsedNode()
        {
            CreatePackfiles(("animations\\battle\\humanoid01\\test.anim", "test.anim"));
            var root = _viewModel.Files[0];

            Assert.That(root.IsNodeExpanded, Is.False);

            var fileNode = PackFileBrowserViewModelTestHelper.GetFromPath(root, "animations\\battle\\humanoid01\\test.anim");

            Assert.That(fileNode, Is.Not.Null);
            Assert.That(fileNode.NodeType, Is.EqualTo(NodeType.File));
            Assert.That(fileNode.Name, Is.EqualTo("test.anim"));
            Assert.That(root.IsNodeExpanded, Is.False);
        }

        [Test]
        public void CollapsingNodeUnloadsItsChildren()
        {
            CreatePackfiles(("folderA\\sub\\file2.txt", "file2.txt"));
            var root = _viewModel.Files[0];

            root.IsNodeExpanded = true;
            var folderA = PackFileBrowserViewModelTestHelper.GetFromPath(root, "foldera");
            Assert.That(folderA, Is.Not.Null);

            folderA.IsNodeExpanded = true;
            Assert.That(PackFileBrowserViewModelTestHelper.GetFromPath(root, "foldera\\sub"), Is.Not.Null);

            folderA.IsNodeExpanded = false;

            Assert.That(PackFileBrowserViewModelTestHelper.GetFromPath(root, "foldera\\sub"), Is.Null);
        }

        [Test]
        public void SearchMaterializesOnlyMatchingBranchAndClearsBackToLazyState()
        {
            CreatePackfiles(
                 ("folderA\\sub\\match_file.txt", "match_file.txt"),
                 ("folderB\\other_file.txt", "other_file.txt"));

            var root = _viewModel.Files[0];
            Assert.That(root.IsNodeExpanded, Is.False);

            _viewModel.Filter.FilterText = "match_file";

            Assert.That(root.IsNodeExpanded, Is.True);
            Assert.That(PackFileBrowserViewModelTestHelper.GetFromPath(root, "foldera\\sub\\match_file.txt"), Is.Not.Null);
            Assert.That(PackFileBrowserViewModelTestHelper.GetFromPath(root, "folderb"), Is.Null);

            _viewModel.Filter.FilterText = string.Empty;

            // After clearing, filter-expanded folders are preserved (absorbed as user expansions)
            Assert.That(root.IsNodeExpanded, Is.True);
            Assert.That(PackFileBrowserViewModelTestHelper.GetFromPath(root, "foldera"), Is.Not.Null);
            // folderB is now also visible and materialized since filter is cleared
            Assert.That(PackFileBrowserViewModelTestHelper.GetFromPath(root, "folderb"), Is.Not.Null);
        }

        [Test]
        public void SearchDoesNotCollapseNodesExpandedByUserBeforeFiltering()
        {
            CreatePackfiles(("folderA\\match_file.txt", "match_file.txt"), ("folderB\\other_file.txt", "other_file.txt"));
            var root = _viewModel.Files[0];

            root.IsNodeExpanded = true;

            _viewModel.Filter.FilterText = "match_file";
            _viewModel.Filter.FilterText = string.Empty;

            Assert.That(root.IsNodeExpanded, Is.True);
            Assert.That(PackFileBrowserViewModelTestHelper.GetFromPath(root, "foldera"), Is.Not.Null);
        }

        [Test]
        public void ClearingFilterKeepsExpandedFoldersAndShowsAllFiles()
        {
            CreatePackfiles(
                ("folderA\\sub\\match_file.txt", "match_file.txt"),
                ("folderA\\sub\\other_a.txt", "other_a.txt"),
                ("folderB\\other_b.txt", "other_b.txt"));

            var root = _viewModel.Files[0];

            // User manually expands root and folderA
            root.IsNodeExpanded = true;
            var folderA = PackFileBrowserViewModelTestHelper.GetFromPath(root, "foldera");
            Assert.That(folderA, Is.Not.Null);
            folderA.IsNodeExpanded = true;

            // Search for a specific file — folderA/sub gets expanded by filter, folderB hidden
            _viewModel.Filter.FilterText = "match_file";

            var sub = PackFileBrowserViewModelTestHelper.GetFromPath(root, "foldera\\sub");
            Assert.That(sub, Is.Not.Null);
            Assert.That(sub.IsNodeExpanded, Is.True, "sub should be expanded by filter");

            // Clear filter
            _viewModel.Filter.FilterText = string.Empty;

            // User-expanded nodes stay expanded
            Assert.That(root.IsNodeExpanded, Is.True, "Root was user-expanded, should stay open");
            Assert.That(folderA.IsNodeExpanded, Is.True, "folderA was user-expanded, should stay open");

            // Filter-expanded nodes are preserved (absorbed)
            sub = PackFileBrowserViewModelTestHelper.GetFromPath(root, "foldera\\sub");
            Assert.That(sub, Is.Not.Null, "sub should still be materialized");
            Assert.That(sub.IsNodeExpanded, Is.True, "sub was filter-expanded, now absorbed as user expansion");

            // Previously hidden folder is now visible and materialized
            var folderB = PackFileBrowserViewModelTestHelper.GetFromPath(root, "folderb");
            Assert.That(folderB, Is.Not.Null, "folderB should be materialized after filter cleared");
            Assert.That(folderB.IsVisible, Is.True, "folderB should be visible after filter cleared");

            // All files in expanded folders are visible
            var matchFile = PackFileBrowserViewModelTestHelper.GetFromPath(root, "foldera\\sub\\match_file.txt");
            var otherA = PackFileBrowserViewModelTestHelper.GetFromPath(root, "foldera\\sub\\other_a.txt");
            Assert.That(matchFile, Is.Not.Null, "match_file should be visible");
            Assert.That(otherA, Is.Not.Null, "other_a should be visible after filter cleared");
        }

        [Test]
        public void CtrlDoubleClickDirectoryExpandsAllChildren()
        {
            CreatePackfiles(
                  ("folderA\\subA\\file1.txt", "file1.txt"),
                  ("folderA\\subB\\file2.txt", "file2.txt"));

            var root = _viewModel.Files[0];
            root.IsNodeExpanded = true;

            var folderA = PackFileBrowserViewModelTestHelper.GetFromPath(root, "foldera");
            Assert.That(folderA, Is.Not.Null);

            // Intentionally set a different selected item to verify command uses clicked node argument.
            _runner.Keyboard.SetKeyDown(Key.LeftCtrl, true);
            _viewModel.SelectedItem = root;
            _viewModel.DoubleClickCommand.Execute(folderA);

            var subA = PackFileBrowserViewModelTestHelper.GetFromPath(root, "foldera\\suba");
            var subB = PackFileBrowserViewModelTestHelper.GetFromPath(root, "foldera\\subb");

            Assert.That(folderA.IsNodeExpanded, Is.True);
            Assert.That(subA, Is.Not.Null);
            Assert.That(subB, Is.Not.Null);
            Assert.That(subA.IsNodeExpanded, Is.True);
            Assert.That(subB.IsNodeExpanded, Is.True);
        }

        [Test]
        public void DeleteFileUsingPfs_EnsureTreeViewUpdated()
        {
            // Arrange: file nested inside a folder
            CreatePackfiles(("foldera\\file.txt", "file.txt"));

            var root = _viewModel.Files[0];
            root.IsNodeExpanded = true;

            var folderA = PackFileBrowserViewModelTestHelper.GetFromPath(root, "foldera");
            Assert.That(folderA, Is.Not.Null);
            folderA.IsNodeExpanded = true;

            // Confirm the file node is visible in the WPF tree before deletion
            Assert.That(folderA.Children.Count(x => x.NodeType == NodeType.File && x.Item != null),
                Is.EqualTo(1), "File node should be present before delete");

            // Act: simulate what PackFileService.DeleteFile does
            var filePackFile = _packageFileService.FindFile("foldera\\file.txt");
            var container = _packageFileService.GetPackFileContainer(filePackFile);
            _packageFileService.DeleteFile(container, filePackFile);

            // Assert: the file node must be gone from the WPF tree
            Assert.That(folderA.Children.Count(x => x.NodeType == NodeType.File && x.Item != null),
                Is.EqualTo(0), "File node should be removed after delete");
        }

        [Test]
        public void RenameFileUsingPfs_EnsureTreeViewUpdated()
        {
            // Arrange: file nested inside a folder
            CreatePackfiles(("foldera\\file.txt", "file.txt"));

            var root = _viewModel.Files[0];
            root.IsNodeExpanded = true;

            var folderA = PackFileBrowserViewModelTestHelper.GetFromPath(root, "foldera");
            Assert.That(folderA, Is.Not.Null);
            folderA.IsNodeExpanded = true;

            // Confirm the file node is visible in the WPF tree before rename
            Assert.That(folderA.Children.Count(x => x.NodeType == NodeType.File && x.Item != null),
                Is.EqualTo(1), "File node should be present before rename");

            // Act: rename the file through PackFileService
            var filePackFile = _packageFileService.FindFile("foldera\\file.txt");
            var container = _packageFileService.GetPackFileContainer(filePackFile);
            _packageFileService.RenameFile(container, filePackFile, "file_renamed.txt");

            // Assert: old node/path should be gone and new node/path should exist in the tree
            var oldNode = PackFileBrowserViewModelTestHelper.GetFromPath(root, "foldera\\file.txt");
            var renamedNode = PackFileBrowserViewModelTestHelper.GetFromPath(root, "foldera\\file_renamed.txt");

            Assert.That(oldNode, Is.Null, "Old file path should not exist after rename");
            Assert.That(renamedNode, Is.Not.Null, "Renamed file should exist in the tree view");
            Assert.That(renamedNode.Name, Is.EqualTo("file_renamed.txt"));
        }

        [Test]
        public void AddDuplicateNamedFileToNewFolderUsingPfs_EnsureTreeViewUpdated()
        {
            // Arrange: existing rigid model file already in one folder
            CreatePackfiles(("variantmeshes\\wh_variantmodels\\hu1\\emp\\emp_karl_franz\\emp_karl_franz.rigid_model_v2", "emp_karl_franz.rigid_model_v2"));

            var root = _viewModel.Files.First(x => x.Name == "test.pack");
            var container = _packageFileService.GetAllPackfileContainers().Last(x => x.Name == "test.pack");
            var saveAsTargetFolder = @"variantmeshes\wh_variantmodels\hu1\emp\new";
            var saveAsTargetPath = saveAsTargetFolder + @"\emp_karl_franz.rigid_model_v2";

            // Act: simulate Save As by adding a second file with the same name in a new folder.
            var copiedRigidModel = PackFile.CreateFromASCII("emp_karl_franz.rigid_model_v2", "copied");
            _packageFileService.AddFilesToPack(container, [new NewPackFileEntry(saveAsTargetFolder, copiedRigidModel)]);

            var addedFile = _packageFileService.FindFile(saveAsTargetPath, container);
            var resolvedPathForAddedFile = _packageFileService.GetFullPath(copiedRigidModel, container);
            var addedNode = PackFileBrowserViewModelTestHelper.GetFromPath(root, saveAsTargetPath);

            Assert.Multiple(() =>
            {
                Assert.That(addedFile, Is.Not.Null, "PackFileService should contain the Save As file at the new folder path");
                Assert.That(resolvedPathForAddedFile, Is.EqualTo(saveAsTargetPath), "Full path lookup should resolve to the newly added file instance path");
                Assert.That(addedNode, Is.Not.Null, "PackFile Explorer should show newly added file path without requiring reload");
            });
        }

        private void CreatePackfiles(params (string Path, string FileName)[] files)
        {
            var container = _packageFileService.CreateNewPackFileContainer("test.pack", PackFileVersion.PFH5, PackFileCAType.MOD);
            foreach (var (path, fileName) in files)
            {
                var normalizedPath = path.ToLowerInvariant().Replace('/', '\\');
                var directory = Path.GetDirectoryName(normalizedPath)?.Replace('/', '\\') ?? string.Empty;
                _packageFileService.AddFilesToPack(container, [new NewPackFileEntry(directory, PackFile.CreateFromASCII(fileName, fileName))]);
            }

            _packageFileService.AddContainer(container);
        }

        [Test]
        public void RenameFolderUsingPfs_EnsureTreeViewUpdated()
        {
            CreatePackfiles(("foldera\\file.txt", "file.txt"));
            var root = _viewModel.Files[0];
            root.IsNodeExpanded = true;

            var folderA = PackFileBrowserViewModelTestHelper.GetFromPath(root, "foldera");
            Assert.That(folderA, Is.Not.Null);

            var container = _packageFileService.GetAllPackfileContainers().Last(x => x.Name == "test.pack");
            _packageFileService.RenameDirectory(container, "foldera", "folderb");

            // Old path gone, new path available
            var oldNode = PackFileBrowserViewModelTestHelper.GetFromPath(root, "foldera");
            var newNode = PackFileBrowserViewModelTestHelper.GetFromPath(root, "folderb");

            Assert.That(oldNode, Is.Null, "Old folder path should not exist after rename");
            Assert.That(newNode, Is.Not.Null, "Renamed folder should be accessible via the new name");
            Assert.That(newNode.UnsavedChanged, Is.True, "Renamed folder should be marked unsaved");
        }

        [Test]
        public void RenameFolderUsingPfs_NestedFolder_EnsureTreeViewUpdated()
        {
            CreatePackfiles(("parent\\child\\file.txt", "file.txt"));
            var root = _viewModel.Files[0];
            root.IsNodeExpanded = true;

            var parent = PackFileBrowserViewModelTestHelper.GetFromPath(root, "parent");
            Assert.That(parent, Is.Not.Null);
            parent.IsNodeExpanded = true;

            var container = _packageFileService.GetAllPackfileContainers().Last(x => x.Name == "test.pack");
            _packageFileService.RenameDirectory(container, "parent\\child", "renamed");

            var newNode = PackFileBrowserViewModelTestHelper.GetFromPath(root, "parent\\renamed");
            Assert.That(newNode, Is.Not.Null, "Nested renamed folder should be accessible");
            Assert.That(newNode.UnsavedChanged, Is.True);
        }

        [Test]
        public void DeleteFolderUsingPfs_EnsureTreeViewUpdated()
        {
            CreatePackfiles(("foldera\\file.txt", "file.txt"), ("folderb\\other.txt", "other.txt"));
            var root = _viewModel.Files[0];
            root.IsNodeExpanded = true;

            var folderA = PackFileBrowserViewModelTestHelper.GetFromPath(root, "foldera");
            Assert.That(folderA, Is.Not.Null);

            var container = _packageFileService.GetAllPackfileContainers().Last(x => x.Name == "test.pack");
            _packageFileService.DeleteFolder(container, "foldera");

            var deletedNode = PackFileBrowserViewModelTestHelper.GetFromPath(root, "foldera");
            Assert.That(deletedNode, Is.Null, "Deleted folder should not exist in tree");

            var survivingNode = PackFileBrowserViewModelTestHelper.GetFromPath(root, "folderb");
            Assert.That(survivingNode, Is.Not.Null, "Non-deleted folder should still exist");
        }

        [Test]
        public void UnloadPackContainer_RemovesFromTree()
        {
            CreatePackfiles(("foldera\\file.txt", "file.txt"));
            Assert.That(_viewModel.Files.Count, Is.EqualTo(1));

            var container = _packageFileService.GetAllPackfileContainers().Last(x => x.Name == "test.pack");
            _packageFileService.UnloadPackContainer(container);

            Assert.That(_viewModel.Files.Count, Is.EqualTo(0));
        }

        [Test]
        public void AddContainer_AppearsInTree()
        {
            Assert.That(_viewModel.Files.Count, Is.EqualTo(0));

            var container = _packageFileService.CreateNewPackFileContainer("new.pack", PackFileVersion.PFH5, PackFileCAType.MOD);
            _packageFileService.AddContainer(container);

            Assert.That(_viewModel.Files.Count, Is.EqualTo(1));
            Assert.That(_viewModel.Files[0].Name, Is.EqualTo("new.pack"));
            Assert.That(_viewModel.Files[0].NodeType, Is.EqualTo(NodeType.Root));
        }

        [Test]
        public void MainEditablePackChanged_UpdatesRootNode()
        {
            var container1 = _packageFileService.CreateNewPackFileContainer("pack1.pack", PackFileVersion.PFH5, PackFileCAType.MOD, true);
            _packageFileService.AddContainer(container1);
            var container2 = _packageFileService.CreateNewPackFileContainer("pack2.pack", PackFileVersion.PFH5, PackFileCAType.MOD);
            _packageFileService.AddContainer(container2);

            var root1 = _viewModel.Files.First(x => x.Name == "pack1.pack");
            var root2 = _viewModel.Files.First(x => x.Name == "pack2.pack");

            Assert.That(root1.IsMainEditabelPack, Is.True);
            Assert.That(root2.IsMainEditabelPack, Is.False);

            _packageFileService.SetEditablePack(container2);

            Assert.That(root1.IsMainEditabelPack, Is.False);
            Assert.That(root2.IsMainEditabelPack, Is.True);
        }

        [Test]
        public void ContainerSaved_ClearsUnsavedFlags()
        {
            CreatePackfiles(("foldera\\file.txt", "file.txt"));
            var root = _viewModel.Files[0];
            var container = _packageFileService.GetAllPackfileContainers().Last(x => x.Name == "test.pack");

            // Add a file to mark things as unsaved
            var newFile = PackFile.CreateFromASCII("new.txt", "data");
            _packageFileService.AddFilesToPack(container, [new NewPackFileEntry("foldera", newFile)]);

            root.IsNodeExpanded = true;
            var folderA = PackFileBrowserViewModelTestHelper.GetFromPath(root, "foldera");
            Assert.That(folderA, Is.Not.Null);
            Assert.That(root.UnsavedChanged, Is.True, "Root should be unsaved after adding file");

            // Simulate save via PFS — we can't call SavePackContainer without disk I/O,
            // so trigger the event directly through the event hub
            var eventHub = _runner.ServiceProvider.GetRequiredService<IEventHub>();
            eventHub.PublishGlobalEvent(new Core.Events.Global.PackFileContainerSavedEvent(container));

            Assert.That(root.UnsavedChanged, Is.False, "Root should be cleared after save");
        }

        [Test]
        public void ContainerSaved_DoesNotForcePopulationOfUnloadedBranches()
        {
            CreatePackfiles(("foldera\\sub\\deep\\file.txt", "file.txt"));
            var root = _viewModel.Files[0];
            var container = _packageFileService.GetAllPackfileContainers().Last(x => x.Name == "test.pack");

            // Root is collapsed — only root exists as materialized node
            Assert.That(root.IsNodeExpanded, Is.False);

            var eventHub = _runner.ServiceProvider.GetRequiredService<IEventHub>();
            eventHub.PublishGlobalEvent(new Core.Events.Global.PackFileContainerSavedEvent(container));

            // After save, the tree should NOT have expanded/materialized children
            Assert.That(root.Children.All(c => c.Name == "<placeholder>" || c.NodeType == NodeType.Root),
                Is.True, "Save should not force population of collapsed branches");
        }

        [Test]
        public void AddFileToRootLevel_EnsureTreeViewUpdated()
        {
            var container = _packageFileService.CreateNewPackFileContainer("test.pack", PackFileVersion.PFH5, PackFileCAType.MOD, true);
            _packageFileService.AddContainer(container);

            var newFile = PackFile.CreateFromASCII("rootfile.txt", "content");
            _packageFileService.AddFilesToPack(container, [new NewPackFileEntry("", newFile)]);

            var root = _viewModel.Files.First(x => x.Name == "test.pack");
            root.IsNodeExpanded = true;

            var fileNode = PackFileBrowserViewModelTestHelper.GetFromPath(root, "rootfile.txt");
            Assert.That(fileNode, Is.Not.Null, "File added at root level should appear in tree");
            Assert.That(fileNode.NodeType, Is.EqualTo(NodeType.File));
        }

        [Test]
        public void AddSecondRootFileAfterExpandingInitiallyEmptyEditablePack_UpdatesMaterializedTree()
        {
            var container = _packageFileService.CreateNewPackFileContainer("test.pack", PackFileVersion.PFH5, PackFileCAType.MOD, true);
            _packageFileService.AddContainer(container);

            var firstFile = PackFile.CreateFromASCII("first.txt", "first");
            _packageFileService.AddFilesToPack(container, [new NewPackFileEntry("", firstFile)]);

            var root = _viewModel.Files.First(x => x.Name == "test.pack");
            Assert.That(root.IsMainEditabelPack, Is.True, "The new pack should be the editable pack for this scenario");

            root.IsNodeExpanded = true;

            var visibleChildrenAfterFirstAdd = root.Children.Where(x => x.NodeType == NodeType.File).Select(x => x.Name).ToList();
            Assert.That(visibleChildrenAfterFirstAdd, Is.EqualTo(new[] { "first.txt" }), "Expanding after the first add should materialize the first root file");

            var secondFile = PackFile.CreateFromASCII("second.txt", "second");
            _packageFileService.AddFilesToPack(container, [new NewPackFileEntry("", secondFile)]);

            var visibleChildrenAfterSecondAdd = root.Children.Where(x => x.NodeType == NodeType.File).Select(x => x.Name).ToList();

            Assert.That(root.IsNodeExpanded, Is.True, "Root should remain expanded after the second add");
            Assert.That(visibleChildrenAfterSecondAdd, Is.EqualTo(new[] { "first.txt", "second.txt" }), "An expanded root should immediately show the newly added root-level file without requiring collapse/re-expand");
        }

        [Test]
        public void DoubleClick_File_InvokesFileOpenEvent()
        {
            CreatePackfiles(("foldera\\file.txt", "file.txt"));
            var root = _viewModel.Files[0];
            root.IsNodeExpanded = true;

            var folderA = PackFileBrowserViewModelTestHelper.GetFromPath(root, "foldera");
            folderA.IsNodeExpanded = true;

            var fileNode = PackFileBrowserViewModelTestHelper.GetFromPath(root, "foldera\\file.txt");
            Assert.That(fileNode, Is.Not.Null);

            PackFile? openedFile = null;
            _viewModel.FileOpen += f => openedFile = f;

            _viewModel.DoubleClickCommand.Execute(fileNode);

            Assert.That(openedFile, Is.Not.Null, "FileOpen should be invoked on double-click");
            Assert.That(openedFile.Name, Is.EqualTo("file.txt"));
        }

        [Test]
        public void DoubleClick_Directory_TogglesExpansion()
        {
            CreatePackfiles(("foldera\\file.txt", "file.txt"));
            var root = _viewModel.Files[0];
            root.IsNodeExpanded = true;

            var folderA = PackFileBrowserViewModelTestHelper.GetFromPath(root, "foldera");
            Assert.That(folderA, Is.Not.Null);
            Assert.That(folderA.IsNodeExpanded, Is.False);

            _viewModel.DoubleClickCommand.Execute(folderA);
            Assert.That(folderA.IsNodeExpanded, Is.True);

            _viewModel.DoubleClickCommand.Execute(folderA);
            Assert.That(folderA.IsNodeExpanded, Is.False);
        }

        [Test]
        public void MoveFileUsingPfs_EnsureTreeViewUpdated()
        {
            CreatePackfiles(("source\\file.txt", "file.txt"), ("target\\other.txt", "other.txt"));
            var root = _viewModel.Files[0];
            root.IsNodeExpanded = true;
            var container = _packageFileService.GetAllPackfileContainers().Last(x => x.Name == "test.pack");

            var sourceFolder = PackFileBrowserViewModelTestHelper.GetFromPath(root, "source");
            Assert.That(sourceFolder, Is.Not.Null);
            sourceFolder.IsNodeExpanded = true;

            var fileNode = PackFileBrowserViewModelTestHelper.GetFromPath(root, "source\\file.txt");
            Assert.That(fileNode, Is.Not.Null);

            var file = _packageFileService.FindFile("source\\file.txt", container);
            _packageFileService.MoveFile(container, file, "target");

            // Old location gone
            var oldNode = PackFileBrowserViewModelTestHelper.GetFromPath(root, "source\\file.txt");
            Assert.That(oldNode, Is.Null, "File should not be in old folder after move");

            // New location has the file
            var targetFolder = PackFileBrowserViewModelTestHelper.GetFromPath(root, "target");
            targetFolder.IsNodeExpanded = true;
            var newNode = PackFileBrowserViewModelTestHelper.GetFromPath(root, "target\\file.txt");
            Assert.That(newNode, Is.Not.Null, "File should appear in target folder after move");
        }

        [Test]
        public void DragDrop_FileOntoFile_NotAllowed()
        {
            CreatePackfiles(("foldera\\file1.txt", "file1.txt"), ("foldera\\file2.txt", "file2.txt"));
            var root = _viewModel.Files[0];
            root.IsNodeExpanded = true;

            var folderA = PackFileBrowserViewModelTestHelper.GetFromPath(root, "foldera");
            folderA.IsNodeExpanded = true;

            var file1 = PackFileBrowserViewModelTestHelper.GetFromPath(root, "foldera\\file1.txt");
            var file2 = PackFileBrowserViewModelTestHelper.GetFromPath(root, "foldera\\file2.txt");

            Assert.That(_viewModel.AllowDrop(file1, file2), Is.False, "Should not allow dropping file onto file");
        }

        [Test]
        public void DragDrop_FolderDrag_NotAllowed()
        {
            CreatePackfiles(("foldera\\file.txt", "file.txt"), ("folderb\\file2.txt", "file2.txt"));
            var root = _viewModel.Files[0];
            root.IsNodeExpanded = true;

            var folderA = PackFileBrowserViewModelTestHelper.GetFromPath(root, "foldera");
            var folderB = PackFileBrowserViewModelTestHelper.GetFromPath(root, "folderb");

            Assert.That(_viewModel.AllowDrop(folderA, folderB), Is.False, "Should not allow dragging folders");
        }

        [Test]
        public void ShowFoldersOnly_HidesFiles()
        {
            CreatePackfiles(("foldera\\file.txt", "file.txt"));
            var root = _viewModel.Files[0];

            _viewModel.Filter.ShowFoldersOnly = true;

            root.IsNodeExpanded = true;
            var folderA = PackFileBrowserViewModelTestHelper.GetFromPath(root, "foldera");
            Assert.That(folderA, Is.Not.Null, "Folder should be visible with ShowFoldersOnly");
            Assert.That(folderA.IsVisible, Is.True);

            folderA.IsNodeExpanded = true;

            // File nodes are not materialized when ShowFoldersOnly is active
            var fileNode = PackFileBrowserViewModelTestHelper.GetFromPath(root, "foldera\\file.txt");
            Assert.That(fileNode, Is.Null, "File should not be materialized with ShowFoldersOnly active");

            // Only folder children should be present
            Assert.That(folderA.Children.All(c => c.NodeType != NodeType.File || !c.IsVisible), Is.True,
                "No visible file nodes should exist under folder with ShowFoldersOnly");
        }

        [Test]
        public void SearchAutoExpandsWhenResultCountBelowThreshold()
        {
            // Create a small number of files (below default threshold of 25)
            CreatePackfiles(
                ("folderA\\sub\\file1.txt", "file1.txt"),
                ("folderA\\sub\\file2.txt", "file2.txt"),
                ("folderB\\file3.txt", "file3.txt"));

            var root = _viewModel.Files[0];
            Assert.That(root.IsNodeExpanded, Is.False, "Root should start collapsed");

            // Filter matches all 3 files — well below the 25 threshold
            _viewModel.Filter.FilterText = "file";

            // All matching nodes should be auto-expanded
            Assert.That(root.IsNodeExpanded, Is.True, "Root should be expanded when results < threshold");

            var folderA = PackFileBrowserViewModelTestHelper.GetFromPath(root, "foldera");
            Assert.That(folderA, Is.Not.Null);
            Assert.That(folderA.IsNodeExpanded, Is.True, "folderA should be auto-expanded");

            var sub = PackFileBrowserViewModelTestHelper.GetFromPath(root, "foldera\\sub");
            Assert.That(sub, Is.Not.Null);
            Assert.That(sub.IsNodeExpanded, Is.True, "sub should be auto-expanded");

            var folderB = PackFileBrowserViewModelTestHelper.GetFromPath(root, "folderb");
            Assert.That(folderB, Is.Not.Null);
            Assert.That(folderB.IsNodeExpanded, Is.True, "folderB should be auto-expanded");
        }

        [Test]
        public void SearchDoesNotAutoExpandWhenResultCountAboveThreshold()
        {
            // Create more files than the threshold
            var files = new List<(string Path, string FileName)>();
            for (var i = 0; i < 30; i++)
                files.Add(($"folder{i}\\file{i}.txt", $"file{i}.txt"));

            CreatePackfiles(files.ToArray());

            var root = _viewModel.Files[0];

            // Set threshold low to ensure we exceed it
            _viewModel.Filter.AutoExapandResultsAfterLimitedCount = 5;
            _viewModel.Filter.FilterText = "file";

            // Root gets expanded by ExpandForFilter (always happens), but children should NOT be recursively expanded
            Assert.That(root.IsNodeExpanded, Is.True, "Root is always expanded by ExpandForFilter");

            // Check that deep nodes are NOT expanded (since results exceed threshold)
            var expandedFolders = root.Children.Where(c => c.IsNodeExpanded).ToList();
            Assert.That(expandedFolders.Count, Is.EqualTo(0),
                "Folders should NOT be auto-expanded when results exceed threshold");
        }

        [Test]
        public void DoubleClickCollapseAndReexpand_ChildrenRemainVisible()
        {
            // Scenario: user expands folders manually, collapses a parent,
            // then re-expands it — child folders that were expanded should still show their children.
            CreatePackfiles(
                ("folderA\\sub\\file1.txt", "file1.txt"),
                ("folderA\\sub\\file2.txt", "file2.txt"));

            var root = _viewModel.Files[0];

            // Expand the tree manually
            root.IsNodeExpanded = true;
            var folderA = PackFileBrowserViewModelTestHelper.GetFromPath(root, "foldera");
            Assert.That(folderA, Is.Not.Null);
            folderA.IsNodeExpanded = true;
            var sub = PackFileBrowserViewModelTestHelper.GetFromPath(root, "foldera\\sub");
            Assert.That(sub, Is.Not.Null);
            sub.IsNodeExpanded = true;

            // Verify files are visible
            var file1 = PackFileBrowserViewModelTestHelper.GetFromPath(root, "foldera\\sub\\file1.txt");
            Assert.That(file1, Is.Not.Null);

            // User double-clicks root to collapse
            _viewModel.DoubleClickCommand.Execute(root);
            Assert.That(root.IsNodeExpanded, Is.False);

            // User double-clicks root to re-expand
            _viewModel.DoubleClickCommand.Execute(root);
            Assert.That(root.IsNodeExpanded, Is.True);

            // folderA should still be visible and expanded with its children materialized
            folderA = PackFileBrowserViewModelTestHelper.GetFromPath(root, "foldera");
            Assert.That(folderA, Is.Not.Null, "folderA should be visible after re-expanding root");
            Assert.That(folderA.IsNodeExpanded, Is.True, "folderA should still be expanded");

            sub = PackFileBrowserViewModelTestHelper.GetFromPath(root, "foldera\\sub");
            Assert.That(sub, Is.Not.Null, "sub should be visible since folderA is expanded");
            Assert.That(sub.IsNodeExpanded, Is.True, "sub should still be expanded");

            // Files inside sub should be materialized
            file1 = PackFileBrowserViewModelTestHelper.GetFromPath(root, "foldera\\sub\\file1.txt");
            Assert.That(file1, Is.Not.Null, "file1 should be visible inside expanded sub folder");
        }

        [Test]
        public void OverwriteFileInCollapsedFolder_NoDuplicateNodeInTree()
        {
            // Arrange: file exists in a folder that is NOT expanded (lazy state).
            CreatePackfiles(("foldera\\file.txt", "file.txt"));
            var root = _viewModel.Files[0];
            var container = _packageFileService.GetAllPackfileContainers().Last(x => x.Name == "test.pack");

            // Folder is collapsed — BackingChildren are NOT yet loaded.
            Assert.That(root.IsNodeExpanded, Is.False);

            // Act: add a second file with the same name (simulates Save As / overwrite import).
            var replacement = PackFile.CreateFromASCII("file.txt", "updated content");
            _packageFileService.AddFilesToPack(container, [new NewPackFileEntry("foldera", replacement)]);

            // Expand to materialise children and assert only ONE node with that name exists.
            root.IsNodeExpanded = true;
            var folderA = PackFileBrowserViewModelTestHelper.GetFromPath(root, "foldera");
            Assert.That(folderA, Is.Not.Null);
            folderA.IsNodeExpanded = true;

            var duplicates = folderA.Children
                .Where(n => n.NodeType == NodeType.File && n.Name.Equals("file.txt", StringComparison.OrdinalIgnoreCase))
                .ToList();

            Assert.That(duplicates.Count, Is.EqualTo(1), "Overwriting a file in a collapsed folder must not create a duplicate node");
            Assert.That(duplicates[0].Item, Is.SameAs(replacement), "The surviving node should reference the replacement PackFile");
        }

        [Test]
        public void OverwriteFileInExpandedFolder_NoDuplicateNodeInTree()
        {
            // Arrange: file exists in a folder that IS already expanded (children materialised).
            CreatePackfiles(("foldera\\file.txt", "file.txt"));
            var root = _viewModel.Files[0];
            var container = _packageFileService.GetAllPackfileContainers().Last(x => x.Name == "test.pack");

            root.IsNodeExpanded = true;
            var folderA = PackFileBrowserViewModelTestHelper.GetFromPath(root, "foldera");
            Assert.That(folderA, Is.Not.Null);
            folderA.IsNodeExpanded = true;

            Assert.That(folderA.Children.Count(n => n.NodeType == NodeType.File), Is.EqualTo(1));

            // Act: add a second file with the same name (simulates overwrite while folder is visible).
            var replacement = PackFile.CreateFromASCII("file.txt", "updated content");
            _packageFileService.AddFilesToPack(container, [new NewPackFileEntry("foldera", replacement)]);

            var duplicates = folderA.Children
                .Where(n => n.NodeType == NodeType.File && n.Name.Equals("file.txt", StringComparison.OrdinalIgnoreCase))
                .ToList();

            Assert.That(duplicates.Count, Is.EqualTo(1), "Overwriting a file in an expanded folder must not create a duplicate node");
            Assert.That(duplicates[0].Item, Is.SameAs(replacement), "The surviving node should reference the replacement PackFile");
        }
    }
}


