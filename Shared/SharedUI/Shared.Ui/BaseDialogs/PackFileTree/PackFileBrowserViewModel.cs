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
        protected IPackFileService _packFileService;
        private readonly IEventHub? _eventHub;
        private readonly IWindowsKeyboard _windowKeyboard;
        private readonly ApplicationSettingsService _applicationSettingsService;
        private readonly PackFileContextMenuComposer _contextMenuComposer;
        private readonly PackFileTreeMutationService _treeMutationService;
        private readonly ContextMenuType _contextMenuType;
        private readonly Dictionary<IPackFileContainer, TreeNode> _treeRoots = [];

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

            Filter = new SearchFilter(Files, () => _treeRoots.Values);
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
                var node = GetNodeFromPackFile(e.Container, file);
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

        private void OnMainEditablePackChanged(PackFileContainerSetAsMainEditableEvent e)
        {
            foreach (var item in Files)
                item.IsMainEditabelPack = false;

            var newContiner = Files.FirstOrDefault(x => x.FileOwner == e.Container);
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
                    _treeMutationService.RemoveExistingFileNode(root, item.Name, item);
                    newNode = new TreeNode(item.Name, NodeType.File, container, root, item);
                    _treeMutationService.InsertChildSorted(root, newNode);
                }
                else
                {
                    var directory = fullPath.Substring(0, directoryEnd);
                    var folder = GetNodeFromPath(root, directory)!;
                    _treeMutationService.RemoveExistingFileNode(folder, item.Name, item);

                    newNode = new TreeNode(item.Name, NodeType.File, container, folder, item);
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
            if (_treeRoots.TryGetValue(container, out var existingRoot))
            {
                Files.Remove(existingRoot);
                _treeRoots.Remove(container);
            }

            var skipWemFiles = container.IsCaPackFile && _applicationSettingsService.CurrentSettings.ShowCAWemFiles == false;

            var root = new TreeNode(container.Name, NodeType.Root, container, null);
            BuildTreeFromFiles(root, container, skipWemFiles);
            root.IsMainEditabelPack = _packFileService.GetEditablePack() == container;

            _treeRoots[container] = root;
            Files.Add(root);
            Filter.Reapply();
        }

        private static void BuildTreeFromFiles(TreeNode root, IPackFileContainer container, bool skipWemFiles)
        {
            var allFiles = container.GetAllFiles();
            var directoryMap = new Dictionary<string, TreeNode>(allFiles.Count, StringComparer.OrdinalIgnoreCase)
            {
                [string.Empty] = root
            };
            var pendingDirectories = new List<(string FolderName, string FullFolderPath)>(10);

            foreach (var item in allFiles)
            {
                var pathSpan = item.Key.AsSpan();
                if (skipWemFiles && pathSpan.EndsWith(".wem", StringComparison.InvariantCultureIgnoreCase))
                    continue;

                var parentNode = root;
                pendingDirectories.Clear();

                var end = pathSpan.Length - 1;
                while (end >= 0)
                {
                    var separatorIndex = pathSpan[..(end + 1)].LastIndexOf(Path.DirectorySeparatorChar);
                    if (separatorIndex == -1)
                        break;

                    var fullFolderPath = pathSpan[..separatorIndex].ToString();
                    if (directoryMap.TryGetValue(fullFolderPath, out var existingDirectory))
                    {
                        parentNode = existingDirectory;
                        break;
                    }

                    var folderNameStart = fullFolderPath.LastIndexOf(Path.DirectorySeparatorChar);
                    var folderName = folderNameStart == -1
                        ? fullFolderPath
                        : fullFolderPath[(folderNameStart + 1)..];
                    pendingDirectories.Add((folderName, fullFolderPath));

                    end = separatorIndex - 1;
                }

                for (var i = pendingDirectories.Count - 1; i >= 0; i--)
                {
                    var currentDirectory = pendingDirectories[i];
                    var currentNode = new TreeNode(currentDirectory.FolderName, NodeType.Directory, container, parentNode);
                    parentNode.AddChild(currentNode);
                    parentNode = currentNode;
                    directoryMap[currentDirectory.FullFolderPath] = currentNode;
                }

                var fileNode = new TreeNode(item.Value.Name, NodeType.File, container, parentNode, item.Value);
                parentNode.AddChild(fileNode);
            }

            SortTree(root);
        }

        private static readonly Comparison<TreeNode> TreeNodeComparison = (left, right) =>
        {
            var nodeTypeComparison = left.NodeType.CompareTo(right.NodeType);
            if (nodeTypeComparison != 0)
                return nodeTypeComparison;

            return StringComparer.CurrentCultureIgnoreCase.Compare(left.Name, right.Name);
        };

        private static void SortTree(TreeNode node)
        {
            var sortedChildren = node.Children
                .OrderBy(child => child, Comparer<TreeNode>.Create(TreeNodeComparison))
                .ToList();

            node.Children.Clear();
            foreach (var child in sortedChildren)
            {
                node.Children.Add(child);
                SortTree(child);
            }
        }

        private void OnPackFileContainerRemoved(PackFileContainerRemovedEvent e)
        {
            if (_treeRoots.TryGetValue(e.Container, out var root))
            {
                Files.Remove(root);
                _treeRoots.Remove(e.Container);
            }
        }

        public bool AllowDrop(TreeNode node, TreeNode? targetNode = null)
        {
            if (targetNode == null)
                return false;

            if (node.Item == null)
                return false;

            if (node.FileOwner != targetNode.FileOwner)
                return false;

            if (node.FileOwner.IsCaPackFile)
                return false;

            if (targetNode.Item != null)
                return false;

            return true;
        }

        public bool Drop(TreeNode node, TreeNode? targeNode)
        {
            if (targeNode == null)
                return false;

            var container = node.FileOwner;
            var draggedFile = node.Item;
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
    }
}
