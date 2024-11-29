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
using Shared.Ui.BaseDialogs.PackFileBrowser.ContextMenu;
using Shared.Ui.Common;

namespace Shared.Ui.BaseDialogs.PackFileBrowser
{
    public delegate void FileSelectedDelegate(PackFile file);
    public delegate void NodeSelectedDelegate(TreeNode node);

    public partial class PackFileBrowserViewModel : ObservableObject, IDisposable, IDropTarget<TreeNode>
    {
        protected IPackFileService _packFileService;
        private readonly IEventHub? _eventHub;
        private readonly IContextMenuBuilder _contextMenuBuilder;

        public event FileSelectedDelegate FileOpen;
        public event NodeSelectedDelegate NodeSelected;

        public ObservableCollection<TreeNode> Files { get; set; } = [];
        public PackFileFilter Filter { get; private set; }
        public ICommand DoubleClickCommand { get; set; }
        public ICommand ClearTextCommand { get; set; }

        [ObservableProperty] TreeNode _selectedItem;
        [ObservableProperty] ObservableCollection<ContextMenuItem2> _contextMenu = [];

        public PackFileBrowserViewModel(IContextMenuBuilder contextMenuBuilder, IPackFileService packFileService, IEventHub? eventHub, bool showCaFiles)
        {
            DoubleClickCommand = new RelayCommand<TreeNode>(OnDoubleClick);
            ClearTextCommand = new RelayCommand(OnClearText);

            _packFileService = packFileService;
            _eventHub = eventHub;
            _contextMenuBuilder = contextMenuBuilder;

            _eventHub?.Register<PackFileContainerSetAsMainEditableEvent>(this, ContainerUpdated);
            _eventHub?.Register<PackFileContainerRemovedEvent>(this, PackFileContainerRemoved);
            _eventHub?.Register<PackFileContainerAddedEvent>(this, x => ReloadTree(x.Container));
            _eventHub?.Register<PackFileContainerFilesUpdatedEvent>(this, Database_PackFilesUpdated);
            _eventHub?.Register<PackFileContainerFilesAddedEvent>(this, x=> AddFiles(x.Container, x.AddedFiles));
            _eventHub?.Register<PackFileContainerFilesRemovedEvent>(this, x => Database_PackFilesRemoved(x.Container, x.RemovedFiles));
                         
            _eventHub?.Register<PackFileContainerFolderRemovedEvent>(this, x => Database_PackFileFolderRemoved(x.Container, x.Folder));
            _eventHub?.Register<PackFileContainerFolderRenamedEvent>(this, x => Database_PackFileFolderRenamed(x.Container, x.NewNodePath));
       
            Filter = new PackFileFilter(Files);

            foreach (var item in _packFileService.GetAllPackfileContainers())
            {
                var loadFile = true;
                if (!showCaFiles)
                    loadFile = !item.IsCaPackFile;

                if (loadFile)
                    ReloadTree(item);
            }
        }

        partial void OnSelectedItemChanged(TreeNode value)
        {
            ContextMenu = _contextMenuBuilder.Build(value);
            NodeSelected?.Invoke(_selectedItem);
        }

        private void Database_PackFileFolderRemoved(PackFileContainer container, string folder)
        {
            var root = GetPackFileCollectionRootNode(container);
            var nodeToDelete = GetNodeFromPath(root, container, folder, false);

            var parent = nodeToDelete.Parent;
            parent.Children.Remove(nodeToDelete);
            nodeToDelete.RemoveSelf();
        }

        private void Database_PackFileFolderRenamed(PackFileContainer container, string folder)
        {
            var root = GetPackFileCollectionRootNode(container);
            var node = GetNodeFromPath(root, container, folder, false);

            node.UnsavedChanged = true;
        }

        private void Database_PackFilesRemoved(PackFileContainer container, List<PackFile> files)
        {
            foreach (var file in files)
            {
                var node = GetNodeFromPackFile(container, file, false);
                node.Parent.Children.Remove(node);
            }
        }

        private void Database_PackFilesAdded(PackFileContainer container, List<PackFile> files)
        {
            AddFiles(container, files);
        }

        private void Database_PackFilesUpdated(PackFileContainerFilesUpdatedEvent e)
        {
            foreach (var file in e.ChangedFiles)
            {
                var rootNode = GetPackFileCollectionRootNode(e.Container);
                rootNode.UnsavedChanged = true;
                var node = GetNodeFromPackFile(e.Container, file);
                node.Name = file.Name;
                node.UnsavedChanged = true;

                var parent = node.Parent;
                while (parent != rootNode)
                {
                    parent.UnsavedChanged = true;
                    parent = parent.Parent;
                }
            }
        }

        protected virtual void OnClearText()
        {
            Filter.FilterText = "";
        }

        protected virtual void OnDoubleClick(TreeNode node)
        {
            // using command parmeter to get node causes memory leaks, using selected node for now
            if (SelectedItem != null)
            {
                if (SelectedItem.GetNodeType() == NodeType.File)
                {
                    FileOpen?.Invoke(SelectedItem.Item);
                }
                else if (SelectedItem.GetNodeType() == NodeType.Directory && Keyboard.IsKeyDown(Key.LeftCtrl))
                {
                    SelectedItem.ExpandIfVisible(true);
                }
            }
        }

        private void ContainerUpdated(PackFileContainerSetAsMainEditableEvent e)
        {
            foreach (var item in Files)
                item.IsMainEditabelPack = false;

            var newContiner = Files.FirstOrDefault(x => x.FileOwner == e.Container);
            if(newContiner != null)
                newContiner.IsMainEditabelPack = true;
        }


        private void AddFiles(PackFileContainer container, List<PackFile> files)
        {
            var root = GetPackFileCollectionRootNode(container);
            root.UnsavedChanged = true;

            foreach (var item in files)
            {
                var fullPath = _packFileService.GetFullPath(item, container);
                var numSeperators = fullPath.Count(x => x == Path.DirectorySeparatorChar);

                var directoryEnd = fullPath.LastIndexOf(Path.DirectorySeparatorChar);
                var fileName = fullPath.Substring(directoryEnd + 1);

                TreeNode newNode;
                if (numSeperators == 0)
                {
                    newNode = new TreeNode(fileName, NodeType.File, container, root, item);
                    root.Children.Add(newNode);
                }
                else
                {
                    var directory = fullPath.Substring(0, directoryEnd);
                    var folder = GetNodeFromPath(root, container, directory);
                    newNode = new TreeNode(fileName, NodeType.File, container, folder, item);

                    // remove any existing files with same name
                    var existingFile = folder.Children.FirstOrDefault(node => node.Name == item.Name);
                    if (existingFile != null)
                    {
                        folder.Children.Remove(existingFile);
                    }

                    folder.Children.Add(newNode);
                }

                newNode.UnsavedChanged = true;
                var parent = newNode.Parent;
                while (parent != root)
                {
                    parent.UnsavedChanged = true;
                    parent = parent.Parent;
                }
            }
        }

        TreeNode GetNodeFromPath(TreeNode parent, PackFileContainer container, string path, bool createIfMissing = true)
        {
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
                    return GetNodeFromPath(child, container, remainingStr);
            }

            if (createIfMissing)
            {
                var newNode = new TreeNode(nodeName, NodeType.Directory, container, parent);
                parent.Children.Add(newNode);
                return GetNodeFromPath(newNode, container, remainingStr);
            }
            return null;
        }

        TreeNode GetPackFileCollectionRootNode(PackFileContainer container)
        {
            foreach (var child in Files)
            {
                if (child.FileOwner == container)
                    return child;
            }
            return null;
        }

        TreeNode GetNodeFromPackFile(PackFileContainer container, PackFile pf, bool createIfMissing = true)
        {
            var root = GetPackFileCollectionRootNode(container);
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
                var parent = GetNodeFromPath(root, container, directory, createIfMissing);

                return parent.Children.FirstOrDefault(x => x.Item == pf);
            }
        }

        private void ReloadTree(PackFileContainer container)
        {
            var existingNode = Files.FirstOrDefault(x => x.FileOwner == container);
            if (existingNode != null)
                Files.Remove(existingNode);

            var root = new TreeNode(container.Name, NodeType.Root, container, null);
            root.IsMainEditabelPack = _packFileService.GetEditablePack() == container;
            var directoryMap = new Dictionary<string, TreeNode>();

            foreach (var item in container.FileList)
            {
                var fullPath = item.Key;
                var pathParts = fullPath.Split(Path.DirectorySeparatorChar);

                if (pathParts.Length == 1)
                {
                    root.Children.Add(new TreeNode(pathParts[0], NodeType.File, container, root, item.Value));
                }
                else
                {
                    var directoryEnd = fullPath.LastIndexOf(Path.DirectorySeparatorChar);
                    var directory = fullPath.Substring(0, directoryEnd);
                    var numSeperators = pathParts.Length - 1;

                    var directoryLookupResult = directoryMap.ContainsKey(directory);
                    if (!directoryLookupResult)
                    {
                        var currentIndexComputed = 0;

                        var lastNode = root;
                        for (var i = 0; i < numSeperators; i++)
                        {
                            currentIndexComputed += pathParts[i].Length + 1;
                            var subDirString = fullPath.Substring(0, currentIndexComputed - 1);

                            if (directoryMap.TryGetValue(subDirString, out var lookUpNode) == false)
                            {
                                var folderNodeName = pathParts[i];
                                var currentNode = new TreeNode(folderNodeName, NodeType.Directory, container, lastNode);
                                lastNode.Children.Add(currentNode);
                                lastNode = currentNode;

                                directoryMap.Add(subDirString, currentNode);
                            }
                            else
                            {
                                lastNode = lookUpNode;
                            }
                        }
                    }

                    var fileName = pathParts.Last();
                    var treeNode = new TreeNode(fileName, NodeType.File, container, directoryMap[directory], item.Value);
                    directoryMap[directory].Children.Add(treeNode);
                }
            }
            Files.Add(root);
        }

        private void PackFileContainerRemoved(PackFileContainerRemovedEvent e)
        {
            var node = Files.FirstOrDefault(x => x.FileOwner == e.Container);
            Files.Remove(node);
        }

        public void Dispose()
        {
            _eventHub?.UnRegister(this);
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

            node.Parent.Children.Remove(node);
            targeNode.Children.Add(node);
            node.Parent = targeNode;

            var root = GetPackFileCollectionRootNode(container);
            root.UnsavedChanged = true;

            return true;
        }
    }
}
