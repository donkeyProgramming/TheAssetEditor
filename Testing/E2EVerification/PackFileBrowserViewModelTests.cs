using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Ui.BaseDialogs.PackFileTree;
using Shared.Ui.BaseDialogs.PackFileTree.ContextMenu;
using Test.TestingUtility.Shared;

namespace Test.E2EVerification
{
    internal class PackFileBrowserViewModelTests
    {
        private AssetEditorTestRunner _runner;
        private IPackFileService _packageFileService;
        private PackFileBrowserViewModel _viewModel;

        [SetUp]
        public void Setup()
        {
            _runner = new AssetEditorTestRunner();
            _runner.PackFileService.EnforceGameFilesMustBeLoaded = false;
            _packageFileService = _runner.PackFileService;

            var builder = _runner.ServiceProvider.GetRequiredService<PackFileTreeViewFactory>();
            _viewModel = builder.Create(ContextMenuType.None, true, false);
        }

        [TearDown]
        public void Cleanup()
        {
            _viewModel.Dispose();
        }

        [Test]
        public void TreeStartsWithOnlyRootNodesMaterialized()
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
        public void GetFromPathFindsNestedFileWhenFoldersAreCollapsed()
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

            Assert.That(root.IsNodeExpanded, Is.False);
            Assert.That(_viewModel.GetFromPath(root, "foldera"), Is.Null);
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
        public void DeletingFileRemovesItsNodeFromTheTree()
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

        private void CreatePackfiles(params (string Path, string FileName)[] files)
        {
            var container = CreateContainer(files);
            _packageFileService.AddContainer(container);


        }

        private static PackFileContainer CreateContainer(params (string Path, string FileName)[] files)
        {
            var container = new PackFileContainer("test.pack")
            {
                SystemFilePath = "test.pack",
                Header = new PFHeader("PFH5", PackFileCAType.MOD)
            };

            foreach (var (path, fileName) in files)
            {
                container.FileList[path.ToLowerInvariant()] = PackFile.CreateFromASCII(fileName, fileName);
            }

            return container;
        }

    }
}

