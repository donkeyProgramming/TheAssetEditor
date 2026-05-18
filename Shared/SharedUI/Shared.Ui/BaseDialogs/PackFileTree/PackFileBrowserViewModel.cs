using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
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
        private readonly record struct PathPrefixKey(string Path, int Length)
        {
            public static readonly PathPrefixKey Empty = new(string.Empty, 0);

            public ReadOnlySpan<char> Span => Path.AsSpan(0, Length);
        }

        private sealed class PathPrefixKeyComparer : IEqualityComparer<PathPrefixKey>
        {
            public static readonly PathPrefixKeyComparer Ordinal = new();

            public bool Equals(PathPrefixKey x, PathPrefixKey y)
            {
                return x.Length == y.Length && x.Span.SequenceEqual(y.Span);
            }

            public int GetHashCode(PathPrefixKey obj)
            {
                var hash = new HashCode();
                foreach (var ch in obj.Span)
                    hash.Add(ch);

                return hash.ToHashCode();
            }
        }

        protected IPackFileService _packFileService;
        private readonly IEventHub? _eventHub;
        private readonly IWindowsKeyboard _windowKeyboard;
        private readonly ApplicationSettingsService _applicationSettingsService;
        private readonly PackFileContextMenuComposer _contextMenuComposer;
        private readonly PackFileTreeMutationService _treeMutationService;
        private readonly ContextMenuType _contextMenuType;
        private readonly Dictionary<IPackFileContainer, TreeNode> _treeRoots = [];
        private readonly Dictionary<TreeNode, IPackFileContainer> _rootOwners = [];

        public event FileSelectedDelegate FileOpen;
        public event NodeSelectedDelegate NodeSelected;

        public ObservableCollection<TreeNode> Files { get; set; } = [];
        public SearchFilter Filter { get; private set; }

        [ObservableProperty] TreeNode _selectedItem;
        [ObservableProperty] ObservableCollection<ContextMenuItem> _contextMenu = [];

        public bool ShowFoldersOnly { get; }

        public PackFileBrowserViewModel(ApplicationSettingsService applicationSettingsService, PackFileContextMenuComposer contextMenuComposer, ContextMenuType contextMenuType, IPackFileService packFileService, IEventHub? eventHub, PackFileTreeMutationService treeMutationService, IWindowsKeyboard windowKeyboard, bool showCaFiles, bool showFoldersOnly)
        {
            _packFileService = packFileService;
            _eventHub = eventHub;
            _treeMutationService = treeMutationService;
            _windowKeyboard = windowKeyboard;
            _applicationSettingsService = applicationSettingsService;
            _contextMenuComposer = contextMenuComposer;
            _contextMenuType = contextMenuType;

            ShowFoldersOnly = showFoldersOnly;

            _eventHub?.Register<PackFileContainerSetAsMainEditableEvent>(this, OnMainEditablePackChanged);
            _eventHub?.Register<PackFileContainerRemovedEvent>(this, OnPackFileContainerRemoved);
            _eventHub?.Register<PackFileContainerAddedEvent>(this, OnPackFileContainerAddedEvent);
            _eventHub?.Register<PackFileContainerFilesUpdatedEvent>(this, OnPackFileContainerFilesUpdatedEvent);
            _eventHub?.Register<PackFileContainerFilesAddedEvent>(this, x => OnPackFileContainerFilesAddedEvent(x.Container, x.AddedFiles));
            _eventHub?.Register<PackFileContainerFilesRemovedEvent>(this, x => OnPackFileContainerFilesRemovedEvent(x.Container, x.RemovedFiles));
            _eventHub?.Register<PackFileContainerFolderRemovedEvent>(this, x => OnPackFileContainerFolderRemovedEvent(x.Container, x.Folder));
            _eventHub?.Register<PackFileContainerFolderRenamedEvent>(this, x => OnPackFileContainerFolderRenamedEvent(x.Container, x.OldNodePath, x.NewNodePath));
            _eventHub?.Register<PackFileContainerSavedEvent>(this, OnPackFileContainerSavedEvent);

            Filter = new SearchFilter(Files, () => _treeRoots);
            Filter.ShowFoldersOnly = showFoldersOnly;
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
            Filter?.Dispose();
        }

        partial void OnSelectedItemChanged(TreeNode value)
        {
            ContextMenu = _contextMenuComposer.Build(_contextMenuType, value);
            NodeSelected?.Invoke(_selectedItem);
        }

        private void OnPackFileContainerAddedEvent(PackFileContainerAddedEvent e) => ReloadTree(e.Container);

        private void OnPackFileContainerFolderRemovedEvent(IPackFileContainer container, string folder)
        {
            var root = GetRootNode(container);
            var nodeToDelete = GetNodeFromPath(root, folder, false);
            if (nodeToDelete == null)
                return;

            _treeMutationService.RemoveNode(nodeToDelete);

            root.UnsavedChanged = true;
            Filter.Reapply();
        }

        private void OnPackFileContainerFolderRenamedEvent(IPackFileContainer container, string oldPath, string newPath)
        {
            var root = GetRootNode(container);
            var node = GetNodeFromPath(root, oldPath, false);
            if (node == null)
                return;

            var newLeafName = newPath;
            var lastSep = newPath.LastIndexOf(Path.DirectorySeparatorChar);
            if (lastSep != -1)
                newLeafName = newPath.Substring(lastSep + 1);

            node.Name = newLeafName;
            root.UnsavedChanged = true;
            node.UnsavedChanged = true;
            var parent = node.Parent;
            while (parent != null && parent != root)
            {
                parent.UnsavedChanged = true;
                parent = parent.Parent;
            }
            Filter.Reapply();
        }

        private void OnPackFileContainerSavedEvent(PackFileContainerSavedEvent e)
        {
            var root = GetRootNode(e.Container);

            root.ForeachNode((node) => node.UnsavedChanged = false);
            Filter.Reapply();
        }

        private void OnPackFileContainerFilesRemovedEvent(IPackFileContainer container, List<PackFile> files)
        {
            var root = GetRootNode(container);
            root.UnsavedChanged = true;

            foreach (var file in files)
            {
                var node = GetNodeFromPackFile(container, file, false);
                if (node == null)
                    continue;

                _treeMutationService.RemoveNode(node);
            }

            Filter.Reapply();
        }

        private void OnPackFileContainerFilesUpdatedEvent(PackFileContainerFilesUpdatedEvent e)
        {
            foreach (var file in e.ChangedFiles)
            {
                var root = GetRootNode(e.Container);
                root.UnsavedChanged = true;
                var node = GetNodeFromPackFile(e.Container, file, false) ?? FindRenamedFileNode(e.Container, file);
                if (node == null)
                    continue;

                node.Name = file.Name;
                node.UnsavedChanged = true;

                var parent = node.Parent;
                while (parent != null && parent != root)
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
                var selectedFile = FindPackFile(targetNode);
                if (selectedFile != null)
                    FileOpen?.Invoke(selectedFile);
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

        private void OnMainEditablePackChanged(PackFileContainerSetAsMainEditableEvent e)
        {
            foreach (var item in Files)
                item.IsMainEditabelPack = false;

            _treeRoots.TryGetValue(e.Container, out var newContiner);
            if (newContiner != null)
                newContiner.IsMainEditabelPack = true;
        }

        private void OnPackFileContainerFilesAddedEvent(IPackFileContainer container, List<PackFile> files)
        {
            var root = GetRootNode(container);
            root.UnsavedChanged = true;

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

                TreeNode newNode;
                if (numSeperators == 0)
                {
                    _treeMutationService.RemoveExistingFileNode(root, item.Name);
                    newNode = new TreeNode(item.Name, NodeType.File, root);
                    _treeMutationService.InsertChildSorted(root, newNode);
                }
                else
                {
                    var directory = fullPath.Substring(0, directoryEnd);
                    var folder = GetNodeFromPath(root, directory)!;
                    _treeMutationService.RemoveExistingFileNode(folder, item.Name);

                    newNode = new TreeNode(item.Name, NodeType.File, folder);
                    _treeMutationService.InsertChildSorted(folder, newNode);
                }

                newNode.UnsavedChanged = true;
                var parent = newNode.Parent;
                while (parent != null && parent != root)
                {
                    parent.UnsavedChanged = true;
                    parent = parent.Parent;
                }
            }

            Filter.Reapply();
        }

        private TreeNode? GetNodeFromPath(TreeNode parent, string path, bool createIfMissing = true)
        {
            if (path.Length == 0)
                return parent;

            var currentIndex = path.IndexOf(Path.DirectorySeparatorChar, 0);
            var nodeName = currentIndex == -1 ? path : path.Substring(0, currentIndex);
            var remainingStr = currentIndex == -1 ? string.Empty : path.Substring(currentIndex + 1);

            foreach (var child in parent.Children)
            {
                if (child.Name == nodeName && child.NodeType == NodeType.Directory)
                    return GetNodeFromPath(child, remainingStr, createIfMissing);
            }

            if (createIfMissing)
            {
                var newNode = _treeMutationService.CreateDirectoryChild(parent, nodeName);
                return GetNodeFromPath(newNode, remainingStr, createIfMissing);
            }
            return null;
        }

        private TreeNode? GetNodeFromPackFile(IPackFileContainer container, PackFile pf, bool createIfMissing = true)
        {
            var root = GetRootNode(container);
            var fullPath = _packFileService.GetFullPath(pf, container);
            var numSeperators = fullPath.Count(x => x == Path.DirectorySeparatorChar);
            var fileName = pf.Name;

            if (numSeperators == 0)
            {
                return root.Children.FirstOrDefault(x => x.NodeType == NodeType.File && x.Name.Equals(fileName, StringComparison.OrdinalIgnoreCase));
            }
            else
            {
                var directoryEnd = fullPath.LastIndexOf(Path.DirectorySeparatorChar);
                var directory = fullPath.Substring(0, directoryEnd);
                var parent = GetNodeFromPath(root, directory, createIfMissing);

                return parent?.Children.FirstOrDefault(x => x.NodeType == NodeType.File && x.Name.Equals(fileName, StringComparison.OrdinalIgnoreCase));
            }
        }

        private TreeNode? FindRenamedFileNode(IPackFileContainer container, PackFile file)
        {
            var root = GetRootNode(container);
            var fullPath = _packFileService.GetFullPath(file, container);
            var directoryEnd = fullPath.LastIndexOf(Path.DirectorySeparatorChar);
            var parent = directoryEnd == -1
                ? root
                : GetNodeFromPath(root, fullPath.Substring(0, directoryEnd), false);
            if (parent == null)
                return null;

            return parent.Children.FirstOrDefault(child =>
                child.NodeType == NodeType.File &&
                !child.Name.Equals(file.Name, StringComparison.OrdinalIgnoreCase) &&
                FindPackFile(child) == null);
        }

        private void ReloadTree(IPackFileContainer container)
        {
            if (_treeRoots.TryGetValue(container, out var existingRoot))
            {
                Files.Remove(existingRoot);
                _treeRoots.Remove(container);
                _rootOwners.Remove(existingRoot);
            }

            var skipWemFiles = container.IsCaPackFile && _applicationSettingsService.CurrentSettings.ShowCAWemFiles == false;

            var root = new RootTreeNode(container.Name, container);
            BuildTreeFromFiles(root, container, skipWemFiles);
            root.IsMainEditabelPack = _packFileService.GetEditablePack() == container;

            _treeRoots[container] = root;
            _rootOwners[root] = container;
            Files.Add(root);
            Filter.Reapply();
        }

        private static void BuildTreeFromFiles(TreeNode root, IPackFileContainer container, bool skipWemFiles)
        {
            var allFiles = container.GetAllFiles();
            var filesByFolder = GroupFilesByFolder(allFiles, skipWemFiles);
            var directoryMap = new Dictionary<PathPrefixKey, TreeNode>(filesByFolder.Count + 1, PathPrefixKeyComparer.Ordinal)
            {
                [PathPrefixKey.Empty] = root
            };
            var childrenByParent = new Dictionary<TreeNode, List<TreeNode>>(filesByFolder.Count + 1);
            var pendingDirectories = new List<(string FolderName, PathPrefixKey FullFolderPath)>(8);

            foreach (var folderPath in filesByFolder.Keys)
            {
                if (folderPath.Length == 0)
                    continue;

                EnsureDirectoryPath(root, folderPath, directoryMap, pendingDirectories, childrenByParent);
            }

            foreach (var folderEntry in filesByFolder)
            {
                var parentNode = directoryMap[folderEntry.Key];
                foreach (var file in folderEntry.Value)
                {
                    var fileNode = new TreeNode(file.Name, NodeType.File, parentNode);
                    AddChildForBuild(parentNode, fileNode, childrenByParent);
                }
            }

            FinalizeTree(root, childrenByParent);
        }

        private static Dictionary<PathPrefixKey, List<PackFile>> GroupFilesByFolder(Dictionary<string, PackFile> allFiles, bool skipWemFiles)
        {
            var filesByFolder = new Dictionary<PathPrefixKey, List<PackFile>>(PathPrefixKeyComparer.Ordinal)
            {
                [PathPrefixKey.Empty] = []
            };

            foreach (var item in allFiles)
            {
                var path = item.Key;
                if (skipWemFiles && path.EndsWith(".wem", StringComparison.OrdinalIgnoreCase))
                    continue;

                var separatorIndex = FindLastDirectorySeparatorIndex(path.AsSpan());
                var folderPath = separatorIndex == -1
                    ? PathPrefixKey.Empty
                    : new PathPrefixKey(path, separatorIndex);

                ref var files = ref CollectionsMarshal.GetValueRefOrAddDefault(filesByFolder, folderPath, out _);
                files ??= [];
                files.Add(item.Value);
            }

            return filesByFolder;
        }

        private static TreeNode EnsureDirectoryPath(TreeNode root, PathPrefixKey folderPath, Dictionary<PathPrefixKey, TreeNode> directoryMap, List<(string FolderName, PathPrefixKey FullFolderPath)> pendingDirectories, Dictionary<TreeNode, List<TreeNode>> childrenByParent)
        {
            if (directoryMap.TryGetValue(folderPath, out var existingDirectory))
                return existingDirectory;

            pendingDirectories.Clear();
            var currentFolderPath = folderPath;
            while (currentFolderPath.Length > 0 && !directoryMap.TryGetValue(currentFolderPath, out existingDirectory))
            {
                var currentPathSpan = currentFolderPath.Span;
                var separatorIndex = FindLastDirectorySeparatorIndex(currentPathSpan);
                var folderName = separatorIndex == -1
                    ? currentFolderPath.Path[..currentFolderPath.Length]
                    : currentFolderPath.Path.Substring(separatorIndex + 1, currentFolderPath.Length - separatorIndex - 1);
                pendingDirectories.Add((folderName, currentFolderPath));
                currentFolderPath = separatorIndex == -1
                    ? PathPrefixKey.Empty
                    : new PathPrefixKey(currentFolderPath.Path, separatorIndex);
            }

            var parentNode = currentFolderPath.Length == 0 ? root : directoryMap[currentFolderPath];
            for (var i = pendingDirectories.Count - 1; i >= 0; i--)
            {
                var currentDirectory = pendingDirectories[i];
                var currentNode = new TreeNode(currentDirectory.FolderName, NodeType.Directory, parentNode);
                AddChildForBuild(parentNode, currentNode, childrenByParent);
                directoryMap[currentDirectory.FullFolderPath] = currentNode;
                parentNode = currentNode;
            }

            return parentNode;
        }

        private static void AddChildForBuild(TreeNode parent, TreeNode child, Dictionary<TreeNode, List<TreeNode>> childrenByParent)
        {
            child.Parent = parent;

            ref var children = ref CollectionsMarshal.GetValueRefOrAddDefault(childrenByParent, parent, out _);
            children ??= [];
            children.Add(child);
        }

        private static int FindLastDirectorySeparatorIndex(ReadOnlySpan<char> path)
        {
            return path.LastIndexOfAny(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }

        private static readonly Comparison<TreeNode> TreeNodeComparison = (left, right) =>
        {
            var nodeTypeComparison = left.NodeType.CompareTo(right.NodeType);
            if (nodeTypeComparison != 0)
                return nodeTypeComparison;

            return StringComparer.CurrentCultureIgnoreCase.Compare(left.Name, right.Name);
        };

        private static void FinalizeTree(TreeNode node, Dictionary<TreeNode, List<TreeNode>> childrenByParent)
        {
            if (!childrenByParent.TryGetValue(node, out var children) || children.Count == 0)
                return;

            children.Sort(TreeNodeComparison);
            node.SetChildren(children);

            foreach (var child in children)
                FinalizeTree(child, childrenByParent);
        }

        private void OnPackFileContainerRemoved(PackFileContainerRemovedEvent e)
        {
            if (_treeRoots.TryGetValue(e.Container, out var root))
            {
                Files.Remove(root);
                _treeRoots.Remove(e.Container);
                _rootOwners.Remove(root);
            }
        }

        public bool AllowDrop(TreeNode node, TreeNode? targetNode = null)
        {
            if (targetNode == null)
                return false;

            if (node.NodeType != NodeType.File)
                return false;

            var sourceContainer = FindFileOwner(node);
            var targetContainer = FindFileOwner(targetNode);
            if (sourceContainer == null || sourceContainer != targetContainer)
                return false;

            if (sourceContainer.IsCaPackFile)
                return false;

            if (targetNode.NodeType == NodeType.File)
                return false;

            if (FindPackFile(node) == null)
                return false;

            return true;
        }

        public bool Drop(TreeNode node, TreeNode? targeNode)
        {
            if (targeNode == null)
                return false;

            var container = FindFileOwner(node);
            if (container == null)
                return false;

            var draggedFile = FindPackFile(node);
            if (draggedFile == null)
                return false;

            var dropPath = targeNode.GetFullPath();

            var newFullPath = string.IsNullOrWhiteSpace(dropPath)
                ? draggedFile.Name
                : dropPath + "\\" + draggedFile.Name;
            if (newFullPath == _packFileService.GetFullPath(draggedFile, container))
                return false;

            _packFileService.MoveFile(container, draggedFile, dropPath);

            return true;
        }

        private TreeNode GetRootNode(IPackFileContainer container)
        {
            return _treeRoots[container];
        }

        public IPackFileContainer? FindFileOwner(TreeNode? node)
        {
            if (node == null)
                return null;

            var root = GetTreeRoot(node);
            return _rootOwners.GetValueOrDefault(root);
        }

        public PackFile? FindPackFile(TreeNode? node)
        {
            if (node == null || node.NodeType != NodeType.File)
                return null;

            var container = FindFileOwner(node);
            if (container == null)
                return null;

            return _packFileService.FindFile(node.GetFullPath(), container);
        }

        private static TreeNode GetTreeRoot(TreeNode node)
        {
            var current = node;
            while (current.Parent != null)
                current = current.Parent;

            return current;
        }
    }
}
