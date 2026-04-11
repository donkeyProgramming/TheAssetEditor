using System.Collections.ObjectModel;
using Shared.Core.Events;
using Shared.Core.Events.Global;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.Settings;
using Shared.Ui.BaseDialogs.PackFileTree;
using Shared.Ui.BaseDialogs.PackFileTree.ContextMenu;

namespace AssetEditorTests.PackFileTree
{
    [TestClass]
    public class PackFileBrowserViewModelTests
    {
        [TestMethod]
        public void TreeStartsWithOnlyRootNodesMaterialized()
        {
            var viewModel = CreateViewModel(("folderA\\file1.txt", "file1.txt"), ("folderB\\file2.txt", "file2.txt"));

            Assert.AreEqual(1, viewModel.Files.Count);

            var root = viewModel.Files[0];
            Assert.IsNull(viewModel.GetFromPath(root, "foldera"));

            root.IsNodeExpanded = true;

            Assert.IsNotNull(viewModel.GetFromPath(root, "foldera"));
            Assert.IsNotNull(viewModel.GetFromPath(root, "folderb"));
        }

        [TestMethod]
        public void GetFromPathFindsNestedFileWhenFoldersAreCollapsed()
        {
            var viewModel = CreateViewModel(("animations\\battle\\humanoid01\\test.anim", "test.anim"));
            var root = viewModel.Files[0];

            Assert.IsFalse(root.IsNodeExpanded);

            var fileNode = viewModel.GetFromPath(root, "animations\\battle\\humanoid01\\test.anim");

            Assert.IsNotNull(fileNode);
            Assert.AreEqual(NodeType.File, fileNode.NodeType);
            Assert.AreEqual("test.anim", fileNode.Name);
            Assert.IsFalse(root.IsNodeExpanded);
        }

        [TestMethod]
        public void CollapsingNodeUnloadsItsChildren()
        {
            var viewModel = CreateViewModel(("folderA\\sub\\file2.txt", "file2.txt"));
            var root = viewModel.Files[0];

            root.IsNodeExpanded = true;
            var folderA = viewModel.GetFromPath(root, "foldera");
            Assert.IsNotNull(folderA);

            folderA.IsNodeExpanded = true;
            Assert.IsNotNull(viewModel.GetFromPath(root, "foldera\\sub"));

            folderA.IsNodeExpanded = false;

            Assert.IsNull(viewModel.GetFromPath(root, "foldera\\sub"));
        }

        [TestMethod]
        public void SearchMaterializesOnlyMatchingBranchAndClearsBackToLazyState()
        {
            var viewModel = CreateViewModel(
                ("folderA\\sub\\match_file.txt", "match_file.txt"),
                ("folderB\\other_file.txt", "other_file.txt"));

            var root = viewModel.Files[0];
            Assert.IsFalse(root.IsNodeExpanded);

            viewModel.Filter.FilterText = "match_file";

            Assert.IsTrue(root.IsNodeExpanded);
            Assert.IsNotNull(viewModel.GetFromPath(root, "foldera\\sub\\match_file.txt"));
            Assert.IsNull(viewModel.GetFromPath(root, "folderb"));

            viewModel.Filter.FilterText = string.Empty;

            Assert.IsFalse(root.IsNodeExpanded);
            Assert.IsNull(viewModel.GetFromPath(root, "foldera"));
        }

        [TestMethod]
        public void SearchDoesNotCollapseNodesExpandedByUserBeforeFiltering()
        {
            var viewModel = CreateViewModel(("folderA\\match_file.txt", "match_file.txt"), ("folderB\\other_file.txt", "other_file.txt"));
            var root = viewModel.Files[0];

            root.IsNodeExpanded = true;

            viewModel.Filter.FilterText = "match_file";
            viewModel.Filter.FilterText = string.Empty;

            Assert.IsTrue(root.IsNodeExpanded);
            Assert.IsNotNull(viewModel.GetFromPath(root, "foldera"));
        }

        [TestMethod]
        public void CtrlDoubleClickDirectoryExpandsAllChildren()
        {
            var viewModel = CreateViewModelWithCtrlState(
                ctrlPressed: true,
                ("folderA\\subA\\file1.txt", "file1.txt"),
                ("folderA\\subB\\file2.txt", "file2.txt"));

            var root = viewModel.Files[0];
            root.IsNodeExpanded = true;

            var folderA = viewModel.GetFromPath(root, "foldera");
            Assert.IsNotNull(folderA);

            // Intentionally set a different selected item to verify command uses clicked node argument.
            viewModel.SelectedItem = root;
            viewModel.DoubleClickCommand.Execute(folderA);

            var subA = viewModel.GetFromPath(root, "foldera\\suba");
            var subB = viewModel.GetFromPath(root, "foldera\\subb");

            Assert.IsTrue(folderA.IsNodeExpanded);
            Assert.IsNotNull(subA);
            Assert.IsNotNull(subB);
            Assert.IsTrue(subA.IsNodeExpanded);
            Assert.IsTrue(subB.IsNodeExpanded);
        }

        [TestMethod]
        public void DeletingFileRemovesItsNodeFromTheTree()
        {
            // Arrange: file nested inside a folder
            var (vm, container, eventHub) = CreateViewModelWithEventHub(("foldera\\file.txt", "file.txt"));
            var root = vm.Files[0];
            root.IsNodeExpanded = true;

            var folderA = vm.GetFromPath(root, "foldera");
            Assert.IsNotNull(folderA);
            folderA.IsNodeExpanded = true;

            // Confirm the file node is visible in the WPF tree before deletion
            Assert.AreEqual(1, folderA.Children.Count(x => x.NodeType == NodeType.File && x.Item != null),
                "File node should be present before delete");

            // Act: simulate what PackFileService.DeleteFile does
            // The event is fired BEFORE the key is removed from FileList (matching real service behaviour)
            var filePackFile = container.FileList["foldera\\file.txt"];
            eventHub.PublishGlobalEvent(new PackFileContainerFilesRemovedEvent(container, new List<PackFile> { filePackFile }));
            container.FileList.Remove("foldera\\file.txt");

            // Assert: the file node must be gone from the WPF tree
            Assert.AreEqual(0, folderA.Children.Count(x => x.NodeType == NodeType.File && x.Item != null),
                "File node should be removed after delete");
        }

        private static PackFileBrowserViewModel CreateViewModel(params (string Path, string FileName)[] files)
        {
            return CreateViewModelWithCtrlState(false, files);
        }

        private static TestablePackFileBrowserViewModel CreateViewModelWithCtrlState(bool ctrlPressed, params (string Path, string FileName)[] files)
        {
            return CreateViewModelInternal(ctrlPressed, null, files);
        }

        private static (TestablePackFileBrowserViewModel Vm, PackFileContainer Container, FakeEventHub EventHub) CreateViewModelWithEventHub(params (string Path, string FileName)[] files)
        {
            var eventHub = new FakeEventHub();
            var container = new PackFileContainer("test.pack")
            {
                SystemFilePath = "test.pack",
                Header = new PFHeader("PFH5", PackFileCAType.MOD)
            };

            foreach (var (path, fileName) in files)
                container.FileList[path.ToLowerInvariant()] = PackFile.CreateFromASCII(fileName, fileName);

            var packFileService = new FakePackFileService(container);
            var settingsService = new ApplicationSettingsService();

            var vm = new TestablePackFileBrowserViewModel(
                settingsService,
                new EmptyContextMenuBuilder(),
                packFileService,
                eventHub,
                showCaFiles: true,
                showFoldersOnly: false,
                ctrlPressed: false);

            return (vm, container, eventHub);
        }

        private static TestablePackFileBrowserViewModel CreateViewModelInternal(bool ctrlPressed, FakeEventHub? eventHub, params (string Path, string FileName)[] files)
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

            var packFileService = new FakePackFileService(container);
            var settingsService = new ApplicationSettingsService();

            return new TestablePackFileBrowserViewModel(
                settingsService,
                new EmptyContextMenuBuilder(),
                packFileService,
                eventHub,
                showCaFiles: true,
                showFoldersOnly: false,
                ctrlPressed: ctrlPressed);
        }

        private sealed class TestablePackFileBrowserViewModel : PackFileBrowserViewModel
        {
            private readonly bool _ctrlPressed;

            public TestablePackFileBrowserViewModel(
                ApplicationSettingsService applicationSettingsService,
                IContextMenuBuilder contextMenuBuilder,
                IPackFileService packFileService,
                Shared.Core.Events.IEventHub? eventHub,
                bool showCaFiles,
                bool showFoldersOnly,
                bool ctrlPressed)
                : base(applicationSettingsService, contextMenuBuilder, packFileService, eventHub, null, showCaFiles, showFoldersOnly)
            {
                _ctrlPressed = ctrlPressed;
            }
        }

        private sealed class EmptyContextMenuBuilder : IContextMenuBuilder
        {
            public ContextMenuType Type => ContextMenuType.None;

            public ObservableCollection<ContextMenuItem2> Build(TreeNode? node)
            {
                return [];
            }
        }

        private sealed class FakeEventHub : IEventHub
        {
            private readonly Dictionary<Type, List<Action<object>>> _handlers = new();

            public void Register<T>(object owner, Action<T> action)
            {
                if (!_handlers.TryGetValue(typeof(T), out var list))
                {
                    list = new List<Action<object>>();
                    _handlers[typeof(T)] = list;
                }
                list.Add(e => action((T)e!));
            }

            public void PublishGlobalEvent<T>(T e)
            {
                if (_handlers.TryGetValue(typeof(T), out var handlers))
                    foreach (var h in handlers)
                        h(e!);
            }

            public void Publish<T>(T e) => PublishGlobalEvent(e);
            public void UnRegister(object owner) { }
        }

        private sealed class FakePackFileService : IPackFileService
        {
            private readonly List<PackFileContainer> _containers;

            public bool EnableFileLookUpEvents { get; set; }
            public bool EnforceGameFilesMustBeLoaded { get; set; }

            public FakePackFileService(params PackFileContainer[] containers)
            {
                _containers = containers.ToList();
            }

            public PackFileContainer? AddContainer(PackFileContainer container, bool setToMainPackIfFirst = false)
            {
                _containers.Add(container);
                return container;
            }

            public void AddFilesToPack(PackFileContainer container, List<NewPackFileEntry> newFiles) => throw new NotImplementedException();
            public void CopyFileFromOtherPackFile(PackFileContainer source, string path, PackFileContainer target) => throw new NotImplementedException();
            public PackFileContainer CreateNewPackFileContainer(string name, PackFileVersion packFileVersion, PackFileCAType type, bool setEditablePack = false) => throw new NotImplementedException();
            public void DeleteFile(PackFileContainer pf, PackFile file) => throw new NotImplementedException();
            public void DeleteFolder(PackFileContainer pf, string folder) => throw new NotImplementedException();
            public PackFile? FindFile(string path, PackFileContainer? container = null) => throw new NotImplementedException();
            public List<PackFileContainer> GetAllPackfileContainers() => _containers.ToList();
            public PackFileContainer? GetEditablePack() => _containers.FirstOrDefault();

            public string GetFullPath(PackFile file, PackFileContainer? container = null)
            {
                container ??= _containers.FirstOrDefault(x => x.FileList.Values.Contains(file));
                if (container == null)
                    throw new InvalidOperationException("Could not find pack file container for file");

                var entry = container.FileList.FirstOrDefault(x => ReferenceEquals(x.Value, file));
                if (string.IsNullOrWhiteSpace(entry.Key))
                    throw new InvalidOperationException("Could not find file path for pack file");

                return entry.Key.Replace('/', '\\');
            }

            public PackFileContainer? GetPackFileContainer(PackFile file)
            {
                return _containers.FirstOrDefault(x => x.FileList.Values.Contains(file));
            }

            public void MoveFile(PackFileContainer pf, PackFile file, string newFolderPath) => throw new NotImplementedException();
            public void RenameDirectory(PackFileContainer pf, string currentNodeName, string newName) => throw new NotImplementedException();
            public void RenameFile(PackFileContainer pf, PackFile file, string newName) => throw new NotImplementedException();
            public void SaveFile(PackFile file, byte[] data) => throw new NotImplementedException();
            public void SavePackContainer(PackFileContainer pf, string path, bool createBackup, GameInformation gameInformation) => throw new NotImplementedException();
            public void SetEditablePack(PackFileContainer? pf) => throw new NotImplementedException();
            public void UnloadPackContainer(PackFileContainer pf) => throw new NotImplementedException();
        }
    }
}
