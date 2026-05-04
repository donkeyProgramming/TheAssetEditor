using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Shared.Core.Events;
using Shared.Core.Events.Global;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.Settings;
using Shared.Ui.BaseDialogs.PackFileTree.ContextMenu;
using Shared.Ui.Common;
using Shared.Ui.Common.MenuSystem;

namespace Shared.Ui.BaseDialogs.PackFileTree
{
    public delegate void FileSelectedDelegate(PackFile file);
    public delegate void NodeSelectedDelegate(TreeNode node);

    public partial class PackFileBrowserViewModel : ObservableObject, IDisposable, IDropTarget<TreeNode>
    {
        private sealed class PackFileTreeState
        {
            public TreeNodeSource RootSource { get; }
            public TreeNode RootNode { get; }

            public PackFileTreeState(TreeNodeSource rootSource, TreeNode rootNode)
            {
                RootSource = rootSource;
                RootNode = rootNode;
            }
        }

        protected IPackFileService _packFileService;
        private readonly IEventHub? _eventHub;
        private readonly IWindowsKeyboard _windowKeyboard;
        private readonly ApplicationSettingsService _applicationSettingsService;
        private readonly IContextMenuBuilder _contextMenuBuilder;
        private readonly Dictionary<IPackFileContainer, PackFileTreeState> _treeStates = [];

        public event FileSelectedDelegate FileOpen;
        public event NodeSelectedDelegate NodeSelected;

        public ObservableCollection<TreeNode> Files { get; set; } = [];
        public SearchFilter Filter { get; private set; }

        [ObservableProperty] TreeNode _selectedItem;
        [ObservableProperty] ObservableCollection<ContextMenuItem2> _contextMenu = [];

        public bool ShowFoldersOnly { get; }

        public PackFileBrowserViewModel(ApplicationSettingsService applicationSettingsService, IContextMenuBuilder contextMenuBuilder, IPackFileService packFileService, IEventHub? eventHub, IWindowsKeyboard windowKeyboard, bool showCaFiles, bool showFoldersOnly)
        {
            _packFileService = packFileService;
            _eventHub = eventHub;
            _windowKeyboard = windowKeyboard;
            _applicationSettingsService = applicationSettingsService;
            _contextMenuBuilder = contextMenuBuilder;

            ShowFoldersOnly = showFoldersOnly;

            _eventHub?.Register<PackFileContainerSetAsMainEditableEvent>(this, MainEditablePackChanged);
            _eventHub?.Register<PackFileContainerRemovedEvent>(this, PackFileContainerRemoved);
            _eventHub?.Register<PackFileContainerAddedEvent>(this, x => ReloadTree(x.Container));
            _eventHub?.Register<PackFileContainerFilesUpdatedEvent>(this, Database_PackFilesUpdated);
            _eventHub?.Register<PackFileContainerFilesAddedEvent>(this, x => AddFiles(x.Container, x.AddedFiles));
            _eventHub?.Register<PackFileContainerFilesRemovedEvent>(this, x => Database_PackFilesRemoved(x.Container, x.RemovedFiles));
            _eventHub?.Register<PackFileContainerFolderRemovedEvent>(this, x => Database_PackFileFolderRemoved(x.Container, x.Folder));
            _eventHub?.Register<PackFileContainerFolderRenamedEvent>(this, x => Database_PackFileFolderRenamed(x.Container, x.OldNodePath, x.NewNodePath));
            _eventHub?.Register<PackFileContainerSavedEvent>(this, ContainerSaved);

            Filter = new SearchFilter(Files, () => _treeStates.Values.Select(x => x.RootSource));
            Filter.ShowFoldersOnly = ShowFoldersOnly;
            foreach (var item in _packFileService.GetAllPackfileContainers())
            {
                var loadFile = true;
                if (!showCaFiles)
                    loadFile = !item.IsCaPackFile;

                if (loadFile)
                    ReloadTree(item);
            }
        }

        public void Dispose()
        {
            _eventHub?.UnRegister(this);
        }

        partial void OnSelectedItemChanged(TreeNode value)
        {
            ContextMenu = _contextMenuBuilder.Build(value);
            NodeSelected?.Invoke(_selectedItem);
        }

        private void Database_PackFileFolderRemoved(IPackFileContainer container, string folder)
        {
            var state = GetPackFileTreeState(container);
            var root = state.RootNode;
            var nodeToDelete = GetNodeFromPath(state.RootSource, folder, false);
            if (nodeToDelete == null)
                return;

            var materializedFolder = nodeToDelete.MaterializedNode;
            nodeToDelete.Parent?.RemoveChild(nodeToDelete);
            if (materializedFolder != null)
            {
                materializedFolder.Parent?.Children.Remove(materializedFolder);
                materializedFolder.RemoveSelf();
            }

            state.RootSource.UnsavedChanged = true;
            root.UnsavedChanged = true;
            Filter.Reapply();
        }

        private void Database_PackFileFolderRenamed(IPackFileContainer container, string oldPath, string newPath)
        {
            var state = GetPackFileTreeState(container);
            var node = GetNodeFromPath(state.RootSource, oldPath, false);
            if (node == null)
                return;

            var newLeafName = newPath;
            var lastSep = newPath.LastIndexOf(Path.DirectorySeparatorChar);
            if (lastSep != -1)
                newLeafName = newPath.Substring(lastSep + 1);

            node.Name = newLeafName;
            node.UnsavedChanged = true;
            Filter.Reapply();
        }

        private void ContainerSaved(PackFileContainerSavedEvent e)
        {
            var state = GetPackFileTreeState(e.Container);
            var root = state.RootNode;

            ClearUnsavedOnLoadedSourceNodes(state.RootSource);

            root.UnsavedChanged = false;
            root.ForeachNode((node) => node.UnsavedChanged = false);
            Filter.Reapply();
        }

        private static void ClearUnsavedOnLoadedSourceNodes(TreeNodeSource node)
        {
            node.UnsavedChanged = false;
            if (!node.ChildrenLoaded)
                return;

            foreach (var child in node.Children)
                ClearUnsavedOnLoadedSourceNodes(child);
        }

        private void Database_PackFilesRemoved(IPackFileContainer container, List<PackFile> files)
        {
            var state = GetPackFileTreeState(container);
            var root = state.RootNode;
            root.UnsavedChanged = true;
            state.RootSource.UnsavedChanged = true;

            foreach (var file in files)
            {
                var node = GetNodeFromPackFile(container, file, false);
                if (node == null)
                    continue;

                var materializedNode = node.MaterializedNode;
                node.Parent?.RemoveChild(node);
                if (materializedNode != null)
                {
                    materializedNode.Parent?.Children.Remove(materializedNode);
                    materializedNode.RemoveSelf();
                }
            }

            Filter.Reapply();
        }

        private void Database_PackFilesUpdated(PackFileContainerFilesUpdatedEvent e)
        {
            foreach (var file in e.ChangedFiles)
            {
                var state = GetPackFileTreeState(e.Container);
                var rootNode = state.RootNode;
                rootNode.UnsavedChanged = true;
                state.RootSource.UnsavedChanged = true;
                var node = GetNodeFromPackFile(e.Container, file);
                if (node == null)
                    continue;
                node.Name = file.Name;
                node.UnsavedChanged = true;

                var parent = node.Parent;
                while (parent != null && parent != state.RootSource)
                {
                    parent.UnsavedChanged = true;
                    parent = parent.Parent;
                }
            }

            Filter.Reapply();
        }

        [RelayCommand]
        protected virtual void OnClearText()
        {
            Filter.FilterText = "";
        }

        [RelayCommand]
        protected virtual void OnDoubleClick(TreeNode node)
        {
            var targetNode = node ?? SelectedItem;
            if (targetNode == null)
                return;

            if (!ReferenceEquals(SelectedItem, targetNode))
                SelectedItem = targetNode;

            var maxExpandCount = 200;
            if (targetNode.NodeType == NodeType.File)
            {
                FileOpen?.Invoke(targetNode.Item!);
            }
            else if (targetNode.NodeType == NodeType.Directory || targetNode.NodeType == NodeType.Root)
            {
                targetNode.IsNodeExpanded = !targetNode.IsNodeExpanded;

                if (_windowKeyboard.IsKeyDown(Key.LeftCtrl))
                {
                    var numChildren = targetNode.GetAllChildFileNodes().Count;
                    if (numChildren < maxExpandCount)
                        targetNode.ExpandIfVisible(true);
                }
            }
        }

        private void MainEditablePackChanged(PackFileContainerSetAsMainEditableEvent e)
        {
            foreach (var item in Files)
                item.IsMainEditabelPack = false;

            var newContiner = Files.FirstOrDefault(x => x.FileOwner == e.Container);
            if (newContiner != null)
                newContiner.IsMainEditabelPack = true;
        }

        private void AddFiles(IPackFileContainer container, List<PackFile> files)
        {
            var state = GetPackFileTreeState(container);
            var root = state.RootNode;
            root.UnsavedChanged = true;
            state.RootSource.UnsavedChanged = true;

            foreach (var item in files)
            {
                if (container.IsCaPackFile && _applicationSettingsService.CurrentSettings.ShowCAWemFiles == false)
                {
                    var isWemFile = item.Name.EndsWith(".wem", StringComparison.InvariantCultureIgnoreCase);
                    if (isWemFile)
                        continue;
                }

                var fullPath = _packFileService.GetFullPath(item, container);
                var numSeperators = fullPath.Count(x => x == Path.DirectorySeparatorChar);

                var directoryEnd = fullPath.LastIndexOf(Path.DirectorySeparatorChar);

                // Check if alreayd added - this happens moving files.

                TreeNodeSource newNode;
                if (numSeperators == 0)
                {
                    RemoveExistingFileNode(state.RootSource, item.Name, item);
                    newNode = new TreeNodeSource(item.Name, NodeType.File, container, state.RootSource, item);
                    InsertChildSorted(state.RootSource, newNode);
                }
                else
                {
                    var directory = fullPath.Substring(0, directoryEnd);
                    var folder = GetNodeFromPath(state.RootSource, directory)!;
                    newNode = new TreeNodeSource(item.Name, NodeType.File, container, folder, item);

                    // remove any existing files with same name
                    var existingFile = folder.Children.FirstOrDefault(node => node.Name == item.Name && node.NodeType == NodeType.File);
                    if (existingFile != null)
                    {
                        folder.RemoveChild(existingFile);
                        existingFile.MaterializedNode?.RemoveSelf();
                    }

                    InsertChildSorted(folder, newNode);
                }

                newNode.UnsavedChanged = true;
                var parent = newNode.Parent;
                while (parent != null && parent != state.RootSource)
                {
                    parent.UnsavedChanged = true;
                    parent = parent.Parent;
                }
            }

            Filter.Reapply();
        }

        public TreeNode? GetFromPath(TreeNode parent, string path)
        {
            if (parent.Source != null)
            {
                var sourceNode = GetAnyNodeFromPath(parent.Source, path);
                if (sourceNode == null)
                    return null;

                if (sourceNode.NodeType == NodeType.File)
                    return GetFromPathMaterialized(parent, path);

                return sourceNode.MaterializedNode;
            }

            var numSeperators = path.Count(x => x == Path.DirectorySeparatorChar);
            if (path.Length == 0)
                return parent;

            var nodeName = path;
            var remainingStr = "";

            if (numSeperators != 0)
            {
                var currentIndex = path.IndexOf(Path.DirectorySeparatorChar, 0);
                nodeName = path.Substring(0, currentIndex);
                remainingStr = path.Substring(currentIndex + 1);
            }

            foreach (var child in parent.Children)
            {
                if (child.Name == nodeName)
                    return GetFromPath(child, remainingStr);
            }

            return null;
        }

        private TreeNode? GetFromPathMaterialized(TreeNode parent, string path)
        {
            if (path.Length == 0)
                return parent;

            var separatorIndex = path.IndexOf(Path.DirectorySeparatorChar);
            var nodeName = separatorIndex == -1 ? path : path.Substring(0, separatorIndex);
            var remainingPath = separatorIndex == -1 ? string.Empty : path.Substring(separatorIndex + 1);

            parent.EnsureChildrenLoaded();
            var child = parent.Children.FirstOrDefault(x => x.Name == nodeName);
            return child == null ? null : GetFromPathMaterialized(child, remainingPath);
        }

        private static TreeNodeSource? GetAnyNodeFromPath(TreeNodeSource parent, string path)
        {
            if (path.Length == 0)
                return parent;

            parent.EnsureChildrenPopulated();

            var separatorIndex = path.IndexOf(Path.DirectorySeparatorChar);
            if (separatorIndex == -1)
                return parent.Children.FirstOrDefault(x => x.Name == path);

            var nodeName = path.Substring(0, separatorIndex);
            var remainingPath = path.Substring(separatorIndex + 1);

            var childDirectory = parent.Children.FirstOrDefault(x => x.Name == nodeName && x.NodeType == NodeType.Directory);
            return childDirectory == null ? null : GetAnyNodeFromPath(childDirectory, remainingPath);
        }

        private TreeNodeSource? GetNodeFromPath(TreeNodeSource parent, string path, bool createIfMissing = true)
        {
            var numSeperators = path.Count(x => x == Path.DirectorySeparatorChar);
            if (path.Length == 0)
                return parent;

            parent.EnsureChildrenPopulated();

            var nodeName = path;
            var remainingStr = "";

            if (numSeperators != 0)
            {
                var currentIndex = path.IndexOf(Path.DirectorySeparatorChar, 0);
                nodeName = path.Substring(0, currentIndex);
                remainingStr = path.Substring(currentIndex + 1);
            }

            foreach (var child in parent.Children)
            {
                if (child.Name == nodeName && child.NodeType == NodeType.Directory)
                    return GetNodeFromPath(child, remainingStr, createIfMissing);
            }

            if (createIfMissing)
            {
                var newNode = new TreeNodeSource(nodeName, NodeType.Directory, parent.FileOwner, parent);
                newNode.MarkChildrenLoaded();
                InsertChildSorted(parent, newNode);
                return GetNodeFromPath(newNode, remainingStr, createIfMissing);
            }
            return null;
        }

        private TreeNodeSource? GetNodeFromPackFile(IPackFileContainer container, PackFile pf, bool createIfMissing = true)
        {
            var state = GetPackFileTreeState(container);
            var root = state.RootSource;
            var fullPath = _packFileService.GetFullPath(pf, container);
            var numSeperators = fullPath.Count(x => x == Path.DirectorySeparatorChar);

            root.EnsureChildrenPopulated();

            if (numSeperators == 0)
            {
                return root.Children.FirstOrDefault(x => x.Item == pf);
            }
            else
            {
                var directoryEnd = fullPath.LastIndexOf(Path.DirectorySeparatorChar);
                var directory = fullPath.Substring(0, directoryEnd);
                var parent = GetNodeFromPath(root, directory, createIfMissing);

                return parent?.Children.FirstOrDefault(x => x.Item == pf);
            }
        }

        private void ReloadTree(IPackFileContainer container)
        {
            if (_treeStates.TryGetValue(container, out var existingState))
            {
                Files.Remove(existingState.RootNode);
                _treeStates.Remove(container);
            }

            var skipWemFiles = container.IsCaPackFile && _applicationSettingsService.CurrentSettings.ShowCAWemFiles == false;

            var rootSource = new TreeNodeSource(container.Name, NodeType.Root, container, null);
            rootSource.SetChildLoader(node => LoadChildrenFromContainer(node, container, skipWemFiles));

            var root = CreateTreeNode(rootSource, null);
            root.IsMainEditabelPack = _packFileService.GetEditablePack() == container;

            _treeStates[container] = new PackFileTreeState(rootSource, root);
            Files.Add(root);
            Filter.Reapply();
        }

        private bool LoadChildrenFromContainer(TreeNodeSource node, IPackFileContainer container, bool skipWemFiles)
        {
            var directoryPath = node.GetFullPath();
            var content = container.GetDirectoryContent(directoryPath);

            foreach (var folderName in content.SubFolders)
            {
                var childNode = new TreeNodeSource(folderName, NodeType.Directory, container, node);
                childNode.SetChildLoader(n => LoadChildrenFromContainer(n, container, skipWemFiles));
                node.AddChild(childNode);
            }

            foreach (var (fileName, file) in content.Files)
            {
                if (skipWemFiles && fileName.EndsWith(".wem", StringComparison.OrdinalIgnoreCase))
                    continue;

                var fileNode = new TreeNodeSource(fileName, NodeType.File, container, node, file);
                node.AddChild(fileNode);
            }

            return true;
        }

        private void PackFileContainerRemoved(PackFileContainerRemovedEvent e)
        {
            if (_treeStates.TryGetValue(e.Container, out var state))
            {
                Files.Remove(state.RootNode);
                _treeStates.Remove(e.Container);
            }
        }



        public bool AllowDrop(TreeNode node, TreeNode targetNode = null)
        {
            if (node.Item == null) // dragging a folder not supported
                return false;

            if (node.FileOwner != targetNode.FileOwner) // dragging between different packs not supported
                return false;

            if (node.FileOwner.IsCaPackFile) // dragging inside CA pack not supported
                return false;

            if (targetNode.Item != null) // dragging file onto a file not supported
                return false;

            return true;
        }

        public bool Drop(TreeNode node, TreeNode targeNode)
        {
            var container = node.FileOwner;
            var draggedFile = node.Item;
            var dropPath = targeNode.GetFullPath();

            var newFullPath = dropPath + "\\" + draggedFile.Name;
            if (newFullPath == _packFileService.GetFullPath(draggedFile, container))
                return false;

            _packFileService.MoveFile(container, draggedFile, dropPath);

            return true;
        }

        private PackFileTreeState GetPackFileTreeState(IPackFileContainer container)
        {
            return _treeStates[container];
        }

        private TreeNode CreateTreeNode(TreeNodeSource source, TreeNode? parent)
        {
            return new TreeNode(source, parent, CreateTreeNode, () => Filter.HasActiveFilter);
        }

        private static readonly Comparison<TreeNodeSource> TreeNodeSourceComparison = (left, right) =>
        {
            var nodeTypeComparison = left.NodeType.CompareTo(right.NodeType);
            if (nodeTypeComparison != 0)
                return nodeTypeComparison;

            return StringComparer.CurrentCultureIgnoreCase.Compare(left.Name, right.Name);
        };

        private static void InsertChildSorted(TreeNodeSource parent, TreeNodeSource child)
        {
            parent.EnsureChildrenPopulated();
            parent.AddChild(child);
            parent.Children.Sort(TreeNodeSourceComparison);
        }

        private static void RemoveExistingFileNode(TreeNodeSource parent, string fileName, PackFile packFile)
        {
            parent.EnsureChildrenPopulated();
            var existingFile = parent.Children.FirstOrDefault(node => node.NodeType == NodeType.File && (node.Item == packFile || node.Name == fileName));
            if (existingFile == null)
                return;

            parent.RemoveChild(existingFile);
            existingFile.MaterializedNode?.RemoveSelf();
        }
    }
}
