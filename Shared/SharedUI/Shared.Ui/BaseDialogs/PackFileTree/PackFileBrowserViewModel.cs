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
using Shared.Core.PackFiles.Utility;
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
        private readonly IContextMenuBuilder _contextMenuBuilder;
        private readonly Dictionary<IPackFileContainer, TreeNode> _treeRoots = [];

        public event FileSelectedDelegate FileOpen;
        public event NodeSelectedDelegate NodeSelected;

        public ObservableCollection<TreeNode> Files { get; set; } = [];
        public SearchFilter Filter { get; private set; }

        [ObservableProperty] TreeNode _selectedItem;
        [ObservableProperty] ObservableCollection<ContextMenuItem> _contextMenu = [];

        public bool ShowFoldersOnly { get; }

        public PackFileBrowserViewModel(ApplicationSettingsService applicationSettingsService, IContextMenuBuilder contextMenuBuilder, IPackFileService packFileService, IEventHub? eventHub, IWindowsKeyboard windowKeyboard, bool showCaFiles, bool showFoldersOnly)
        {
            _packFileService = packFileService;
            _eventHub = eventHub;
            _windowKeyboard = windowKeyboard;
            _applicationSettingsService = applicationSettingsService;
            _contextMenuBuilder = contextMenuBuilder;

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
        }

        partial void OnSelectedItemChanged(TreeNode value)
        {
            ContextMenu = _contextMenuBuilder.Build(value);
            NodeSelected?.Invoke(_selectedItem);
        }

        private void OnPackFileContainerAddedEvent(PackFileContainerAddedEvent e) => ReloadTree(e.Container);

        private void OnPackFileContainerFolderRemovedEvent(IPackFileContainer container, string folder)
        {
            var root = GetRootNode(container);
            var nodeToDelete = GetNodeFromPath(root, folder, false);
            if (nodeToDelete == null)
                return;

            var parentNode = nodeToDelete.Parent;
            parentNode?.RemoveChild(nodeToDelete);
            parentNode?.Children.Remove(nodeToDelete);
            nodeToDelete.RemoveSelf();

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
            node.UnsavedChanged = true;
            Filter.Reapply();
        }

        private void OnPackFileContainerSavedEvent(PackFileContainerSavedEvent e)
        {
            var root = GetRootNode(e.Container);

            TreeNodeStateHelper.ClearUnsavedOnLoadedNodes(root);

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

                var parentNode = node.Parent;
                parentNode?.RemoveChild(node);
                parentNode?.Children.Remove(node);
                node.RemoveSelf();
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
                    // EnsureChildrenPopulated runs inside InsertChildSorted and may load the
                    // just-added file from the container, so remove any duplicate after population.
                    root.EnsureChildrenPopulated();
                    TreeNodeManipulationHelper.RemoveExistingFileNode(root, item.Name, item);
                    newNode = new TreeNode(item.Name, NodeType.File, container, root, item);
                    TreeNodeManipulationHelper.InsertChildSorted(root, newNode);
                }
                else
                {
                    var directory = fullPath.Substring(0, directoryEnd);
                    var folder = GetNodeFromPath(root, directory)!;

                    // Populate the folder first so that any duplicate loaded from the container
                    // by EnsureChildrenPopulated (inside InsertChildSorted) is visible here.
                    folder.EnsureChildrenPopulated();
                    TreeNodeManipulationHelper.RemoveExistingFileNode(folder, item.Name, item);

                    newNode = new TreeNode(item.Name, NodeType.File, container, folder, item);
                    TreeNodeManipulationHelper.InsertChildSorted(folder, newNode);
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

            foreach (var child in parent.BackingChildren)
            {
                if (child.Name == nodeName && child.NodeType == NodeType.Directory)
                    return GetNodeFromPath(child, remainingStr, createIfMissing);
            }

            if (createIfMissing)
            {
                var newNode = new TreeNode(nodeName, NodeType.Directory, parent.FileOwner, parent, () => Filter.HasActiveFilter);
                newNode.MarkChildrenLoaded();
                TreeNodeManipulationHelper.InsertChildSorted(parent, newNode);
                return GetNodeFromPath(newNode, remainingStr, createIfMissing);
            }
            return null;
        }

        private TreeNode? GetNodeFromPackFile(IPackFileContainer container, PackFile pf, bool createIfMissing = true)
        {
            var root = GetRootNode(container);
            var fullPath = _packFileService.GetFullPath(pf, container);
            var numSeperators = fullPath.Count(x => x == Path.DirectorySeparatorChar);

            root.EnsureChildrenPopulated();

            if (numSeperators == 0)
            {
                return root.BackingChildren.FirstOrDefault(x => x.Item == pf);
            }
            else
            {
                var directoryEnd = fullPath.LastIndexOf(Path.DirectorySeparatorChar);
                var directory = fullPath.Substring(0, directoryEnd);
                var parent = GetNodeFromPath(root, directory, createIfMissing);

                return parent?.BackingChildren.FirstOrDefault(x => x.Item == pf);
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

            var root = new TreeNode(container.Name, NodeType.Root, container, null, () => Filter.HasActiveFilter);
            root.SetChildLoader(node => LoadChildrenFromContainer(node, container, skipWemFiles));
            root.IsMainEditabelPack = _packFileService.GetEditablePack() == container;

            _treeRoots[container] = root;
            Files.Add(root);
            Filter.Reapply();
        }

        private bool LoadChildrenFromContainer(TreeNode node, IPackFileContainer container, bool skipWemFiles)
        {
            var directoryPath = node.GetFullPath();
            var split = PackFileServiceUtility.SplitDirectoryEntries(container, directoryPath);

            foreach (var folderName in split.SubFolders)
            {
                var childNode = new TreeNode(folderName, NodeType.Directory, container, node, () => Filter.HasActiveFilter);
                childNode.SetChildLoader(n => LoadChildrenFromContainer(n, container, skipWemFiles));
                node.AddChild(childNode);
            }

            foreach (var fileEntry in split.Files)
            {
                var fileName = fileEntry.FileName;
                var file = fileEntry.File;
                if (skipWemFiles && fileName.EndsWith(".wem", StringComparison.OrdinalIgnoreCase))
                    continue;

                var fileNode = new TreeNode(fileName, NodeType.File, container, node, file);
                node.AddChild(fileNode);
            }

            return true;
        }

        private void OnPackFileContainerRemoved(PackFileContainerRemovedEvent e)
        {
            if (_treeRoots.TryGetValue(e.Container, out var root))
            {
                Files.Remove(root);
                _treeRoots.Remove(e.Container);
            }
        }

        public bool AllowDrop(TreeNode node, TreeNode targetNode = null)
        {
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

        private TreeNode GetRootNode(IPackFileContainer container)
        {
            return _treeRoots[container];
        }
    }
}
