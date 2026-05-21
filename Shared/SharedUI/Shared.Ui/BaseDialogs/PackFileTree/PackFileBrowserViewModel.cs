using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Shared.Core.Events;
using Shared.Core.Events.Global;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.Services;
using Shared.Core.Settings;
using Shared.Ui.BaseDialogs.PackFileTree.Commands;
using Shared.Ui.BaseDialogs.PackFileTree.ContextMenu;
using Shared.Ui.BaseDialogs.PackFileTree.Utility;
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
        private readonly ContextMenuType _contextMenuType;
        private readonly DoubleClickCommand _doubleClickCommand;

        public event FileSelectedDelegate FileOpen;
        public event NodeSelectedDelegate NodeSelected;

        public ObservableCollection<RootTreeNode> Files { get; set; } = [];
        public SearchFilter Filter { get; private set; }

        [ObservableProperty] TreeNode _selectedItem;
        [ObservableProperty] ObservableCollection<ContextMenuItem> _contextMenu = [];

        public bool ShowFoldersOnly { get; }

        public PackFileBrowserViewModel(
            ApplicationSettingsService applicationSettingsService, 
            PackFileContextMenuComposer contextMenuComposer, 
            ContextMenuType contextMenuType, 
            IPackFileService packFileService,
            IEventHub? eventHub, 
            IWindowsKeyboard windowKeyboard,
            bool showCaFiles, bool showFoldersOnly,
            IStandardDialogs? standardDialogs = null)
        {
            _packFileService = packFileService;
            _eventHub = eventHub;
            _windowKeyboard = windowKeyboard;
            _applicationSettingsService = applicationSettingsService;
            _contextMenuComposer = contextMenuComposer;
            _contextMenuType = contextMenuType;
            _doubleClickCommand = new DoubleClickCommand(packFileService, windowKeyboard);

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

            Filter = new SearchFilter(Files, standardDialogs)
            {
                ShowFoldersOnly = showFoldersOnly
            };

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

            PackFileTreeMutationService.RemoveNode(nodeToDelete);

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

                PackFileTreeMutationService.RemoveNode(node);
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
            _doubleClickCommand.Execute(node, SelectedItem, n => SelectedItem = n, file => FileOpen?.Invoke(file));
        }

        private void OnMainEditablePackChanged(PackFileContainerSetAsMainEditableEvent e)
        {
            foreach (var item in Files)
                item.IsMainEditabelPack = item.Owner == e.Container;
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
                    PackFileTreeMutationService.RemoveExistingFileNode(root, item.Name);
                    newNode = new TreeNode(item.Name, NodeType.File, root);
                    PackFileTreeMutationService.InsertChildSorted(root, newNode);
                }
                else
                {
                    var directory = fullPath.Substring(0, directoryEnd);
                    var folder = GetNodeFromPath(root, directory)!;
                    PackFileTreeMutationService.RemoveExistingFileNode(folder, item.Name);

                    newNode = new TreeNode(item.Name, NodeType.File, folder);
                    PackFileTreeMutationService.InsertChildSorted(folder, newNode);
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
                var newNode = PackFileTreeMutationService.CreateDirectoryChild(parent, nodeName);
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
            foreach (var file in Files)
            {
                if (file.Owner == container)
                {
                    Files.Remove(file);
                    break;
                }  
            }

            var skipWemFiles = container.IsCaPackFile && _applicationSettingsService.CurrentSettings.ShowCAWemFiles == false;

            var root = new RootTreeNode(container.Name, container);
            PackFileTreeBuilder.BuildTreeFromFiles(root, container, skipWemFiles);
            root.IsMainEditabelPack = _packFileService.GetEditablePack() == container;

            Files.Add(root);
            Filter.Reapply();
        }

        private void OnPackFileContainerRemoved(PackFileContainerRemovedEvent e)
        {
            foreach (var file in Files)
            {
                if (file.Owner == e.Container)
                {
                    Files.Remove(file);
                    break;
                }
            }
        }

        public bool AllowDrop(TreeNode node, TreeNode? targetNode = null) => DropHandler.AllowDrop(node, targetNode, _packFileService);

        public bool Drop(TreeNode node, TreeNode? targeNode) => DropHandler.Drop(node, targeNode, _packFileService);

        private TreeNode GetRootNode(IPackFileContainer container)
        {
            foreach (var node in Files)
            {
                if (node.Owner == container)
                {
                    return node;
                }
            }

            throw new Exception("Unable to find root node from Container where name = " + container.Name);
        }

        public IPackFileContainer? FindFileOwner(TreeNode? node)
        {
            if (node == null)
                return null;

            var root = TreeNodeHelper.GetRootNode(node);
            return root.Owner;
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
    }
}
