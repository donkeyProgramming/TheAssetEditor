using System.Windows.Input;
using AssetEditor.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.Settings;
using Shared.Ui.BaseDialogs.PackFileTree;
using Test.TestingUtility.Shared;
using Test.TestingUtility.TestUtility;

namespace Shared.UiTest
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
            var fileToMove = _viewModel.GetFromPath(packRootNode, @"animations\battle\humanoid01\2handed_hammer\stand\hu1_2hh_stand_idle_01.anim");
            var destinationNode = _viewModel.GetFromPath(packRootNode, @"animations");
            _viewModel.Drop(fileToMove, destinationNode);

            // Assert
            // Get file in packfileservice
            var movedFile = _runner.PackFileService.FindFile(@"animations\hu1_2hh_stand_idle_01.anim");
            Assert.That(movedFile, Is.Not.Null);

            // Get file node from 
            var movedNode = _viewModel.GetFromPath(packRootNode, @"animations\hu1_2hh_stand_idle_01.anim");
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
            Assert.That(_viewModel.GetFromPath(root, "foldera"), Is.Null);

            root.IsNodeExpanded = true;

            Assert.That(_viewModel.GetFromPath(root, "foldera"), Is.Not.Null);
            Assert.That(_viewModel.GetFromPath(root, "folderb"), Is.Not.Null);
        }

        [Test]
        public void GetFileFromCollapsedNode()
        {
            CreatePackfiles(("animations\\battle\\humanoid01\\test.anim", "test.anim"));
            var root = _viewModel.Files[0];

            Assert.That(root.IsNodeExpanded, Is.False);

            var fileNode = _viewModel.GetFromPath(root, "animations\\battle\\humanoid01\\test.anim");

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
            var folderA = _viewModel.GetFromPath(root, "foldera");
            Assert.That(folderA, Is.Not.Null);

            folderA.IsNodeExpanded = true;
            Assert.That(_viewModel.GetFromPath(root, "foldera\\sub"), Is.Not.Null);

            folderA.IsNodeExpanded = false;

            Assert.That(_viewModel.GetFromPath(root, "foldera\\sub"), Is.Null);
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
            Assert.That(_viewModel.GetFromPath(root, "foldera\\sub\\match_file.txt"), Is.Not.Null);
            Assert.That(_viewModel.GetFromPath(root, "folderb"), Is.Null);

            _viewModel.Filter.FilterText = string.Empty;

            // After clearing, filter-expanded folders are preserved (absorbed as user expansions)
            Assert.That(root.IsNodeExpanded, Is.True);
            Assert.That(_viewModel.GetFromPath(root, "foldera"), Is.Not.Null);
            // folderB is now also visible and materialized since filter is cleared
            Assert.That(_viewModel.GetFromPath(root, "folderb"), Is.Not.Null);
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
            Assert.That(_viewModel.GetFromPath(root, "foldera"), Is.Not.Null);
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
            var folderA = _viewModel.GetFromPath(root, "foldera");
            Assert.That(folderA, Is.Not.Null);
            folderA.IsNodeExpanded = true;

            // Search for a specific file — folderA/sub gets expanded by filter, folderB hidden
            _viewModel.Filter.FilterText = "match_file";

            var sub = _viewModel.GetFromPath(root, "foldera\\sub");
            Assert.That(sub, Is.Not.Null);
            Assert.That(sub.IsNodeExpanded, Is.True, "sub should be expanded by filter");

            // Clear filter
            _viewModel.Filter.FilterText = string.Empty;

            // User-expanded nodes stay expanded
            Assert.That(root.IsNodeExpanded, Is.True, "Root was user-expanded, should stay open");
            Assert.That(folderA.IsNodeExpanded, Is.True, "folderA was user-expanded, should stay open");

            // Filter-expanded nodes are preserved (absorbed)
            sub = _viewModel.GetFromPath(root, "foldera\\sub");
            Assert.That(sub, Is.Not.Null, "sub should still be materialized");
            Assert.That(sub.IsNodeExpanded, Is.True, "sub was filter-expanded, now absorbed as user expansion");

            // Previously hidden folder is now visible and materialized
            var folderB = _viewModel.GetFromPath(root, "folderb");
            Assert.That(folderB, Is.Not.Null, "folderB should be materialized after filter cleared");
            Assert.That(folderB.IsVisible, Is.True, "folderB should be visible after filter cleared");

            // All files in expanded folders are visible
            var matchFile = _viewModel.GetFromPath(root, "foldera\\sub\\match_file.txt");
            var otherA = _viewModel.GetFromPath(root, "foldera\\sub\\other_a.txt");
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

            var folderA = _viewModel.GetFromPath(root, "foldera");
            Assert.That(folderA, Is.Not.Null);

            // Intentionally set a different selected item to verify command uses clicked node argument.
            _runner.Keyboard.SetKeyDown(Key.LeftCtrl, true);
            _viewModel.SelectedItem = root;
            _viewModel.DoubleClickCommand.Execute(folderA);

            var subA = _viewModel.GetFromPath(root, "foldera\\suba");
            var subB = _viewModel.GetFromPath(root, "foldera\\subb");

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

            var folderA = _viewModel.GetFromPath(root, "foldera");
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

            var folderA = _viewModel.GetFromPath(root, "foldera");
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
            var oldNode = _viewModel.GetFromPath(root, "foldera\\file.txt");
            var renamedNode = _viewModel.GetFromPath(root, "foldera\\file_renamed.txt");

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
            var addedNode = _viewModel.GetFromPath(root, saveAsTargetPath);

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
                container.AddOrUpdateFile(path.ToLowerInvariant(), PackFile.CreateFromASCII(fileName, fileName));
            }

            _packageFileService.AddContainer(container);
        }
    }
}

