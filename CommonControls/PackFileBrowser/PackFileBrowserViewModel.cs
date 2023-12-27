// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows.Input;
using CommonControls.Common;
using CommonControls.FileTypes.PackFiles.Models;
using CommonControls.Services;
using CommunityToolkit.Mvvm.Input;

namespace CommonControls.PackFileBrowser
{
    public delegate void FileSelectedDelegate(PackFile file);
    public delegate void NodeSelectedDelegate(TreeNode node);

    public class PackFileBrowserViewModel : NotifyPropertyChangedImpl, IDisposable, IDropTarget<TreeNode>
    {
        protected PackFileService _packFileService;
        public event FileSelectedDelegate FileOpen;
        public event NodeSelectedDelegate NodeSelected;

        public ObservableCollection<TreeNode> Files { get; set; } = new ObservableCollection<TreeNode>();
        public PackFileFilter Filter { get; private set; }
        public ICommand DoubleClickCommand { get; set; }
        public ICommand ClearTextCommand { get; set; }

        TreeNode _selectedItem;
        public TreeNode SelectedItem
        {
            get => _selectedItem;
            set
            {
                SetAndNotify(ref _selectedItem, value);
                ContextMenu?.Create(value);
                NodeSelected?.Invoke(_selectedItem);
            }
        }

        public ContextMenuHandler ContextMenu { get; set; }

        public PackFileBrowserViewModel(PackFileService packFileService, bool ignoreCaFiles = false)
        {
            DoubleClickCommand = new RelayCommand<TreeNode>(OnDoubleClick);
            ClearTextCommand = new RelayCommand(OnClearText);

            _packFileService = packFileService;
            _packFileService.Database.PackFileContainerLoaded += ReloadTree;
            _packFileService.Database.PackFileContainerRemoved += PackFileContainerRemoved;
            _packFileService.Database.ContainerUpdated += ContainerUpdated;

            _packFileService.Database.PackFilesUpdated += Database_PackFilesUpdated;
            _packFileService.Database.PackFilesAdded += Database_PackFilesAdded;
            _packFileService.Database.PackFilesRemoved += Database_PackFilesRemoved;
            _packFileService.Database.PackFileFolderRemoved += Database_PackFileFolderRemoved;
            _packFileService.Database.PackFileFolderRenamed += Database_PackFileFolderRenamed;

            Filter = new PackFileFilter(Files);

            foreach (var item in _packFileService.Database.PackFiles)
            {
                bool loadFile = true;
                if (ignoreCaFiles)
                    loadFile = !item.IsCaPackFile;

                if (loadFile)
                    ReloadTree(item);
            }
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

        private void Database_PackFilesUpdated(PackFileContainer container, List<PackFile> files)
        {
            foreach (var file in files)
            {
                var rootNode = GetPackFileCollectionRootNode(container);
                rootNode.UnsavedChanged = true;
                var node = GetNodeFromPackFile(container, file);
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
                if (SelectedItem.NodeType == NodeType.File)
                {
                    FileOpen?.Invoke(SelectedItem.Item);
                }
                else if (SelectedItem.NodeType == NodeType.Directory && Keyboard.IsKeyDown(Key.LeftCtrl))
                {
                    SelectedItem.ExpandIfVisible(true);
                }
            }
        }

        private void ContainerUpdated(PackFileContainer pf)
        {
            foreach (var item in Files)
                item.IsMainEditabelPack = false;

            Files.FirstOrDefault(x => x.FileOwner == pf).IsMainEditabelPack = true;
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

                        TreeNode lastNode = root;
                        for (int i = 0; i < numSeperators; i++)
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

        private bool PackFileContainerRemoved(PackFileContainer container)
        {
            var node = Files.FirstOrDefault(x => x.FileOwner == container);
            Files.Remove(node);
            return true;
        }

        public void Dispose()
        {
            _packFileService.Database.PackFileContainerLoaded -= ReloadTree;
            _packFileService.Database.PackFileContainerRemoved -= PackFileContainerRemoved;
            _packFileService.Database.ContainerUpdated -= ContainerUpdated;

            _packFileService.Database.PackFilesUpdated -= Database_PackFilesUpdated;
            _packFileService.Database.PackFilesAdded -= Database_PackFilesAdded;
            _packFileService.Database.PackFilesRemoved -= Database_PackFilesRemoved;
            _packFileService.Database.PackFileFolderRemoved -= Database_PackFileFolderRemoved;
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
