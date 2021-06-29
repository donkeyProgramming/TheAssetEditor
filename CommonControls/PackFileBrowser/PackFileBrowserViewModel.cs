using Common;
using CommonControls.Common;
using CommonControls.Services;
using CommonControls.Simple;
using FileTypes.PackFiles.Models;
using GalaSoft.MvvmLight.Command;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace CommonControls.PackFileBrowser
{
    public delegate void FileSelectedDelegate(IPackFile file);
    public delegate void NodeSelectedDelegate(TreeNode node);

    public class PackFileBrowserViewModel : NotifyPropertyChangedImpl, IDisposable
    {
        protected PackFileService _packFileService;
        public event FileSelectedDelegate FileOpen;
        public event NodeSelectedDelegate NodeSelected;

        public ObservableCollection<TreeNode> Files { get; set; } = new ObservableCollection<TreeNode>();
        public PackFileFilter Filter { get; private set; }
        public ICommand DoubleClickCommand { get; set; }

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

            _packFileService = packFileService;
            _packFileService.Database.PackFileContainerLoaded += ReloadTree;
            _packFileService.Database.PackFileContainerRemoved += PackFileContainerRemoved;
            _packFileService.Database.ContainerUpdated += ContainerUpdated;

            _packFileService.Database.PackFilesUpdated += Database_PackFilesUpdated;
            _packFileService.Database.PackFilesAdded += Database_PackFilesAdded;
            _packFileService.Database.PackFilesRemoved += Database_PackFilesRemoved;
            _packFileService.Database.PackFileFolderRemoved += Database_PackFileFolderRemoved;

            Filter = new PackFileFilter(Files);

            foreach (var item in _packFileService.Database.PackFiles)
            {
                bool loadFile = true;
                if (ignoreCaFiles)
                    loadFile = !item.IsCaPackFile;

                if(loadFile)
                    ReloadTree(item);
            }
        }

        private void Database_PackFileFolderRemoved(PackFileContainer container, string folder)
        {
            var root = GetPackFileCollectionRootNode(container);
            var node = GetNodeFromPath(root, container, folder, false);
            
            node.Parent.Children.Remove(node);
            node.RemoveSelf();
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

        protected virtual void OnDoubleClick(TreeNode node) 
        {
            // using command parmeter to get node causes memory leaks, using selected node for now
            if (SelectedItem != null && SelectedItem.NodeType == NodeType.File)
                FileOpen?.Invoke(SelectedItem.Item); 
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
                    var folder = GetNodeFromPath(root, container,directory);
                    newNode = new TreeNode(fileName, NodeType.File, container, folder, item);
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
            if(existingNode != null)
                Files.Remove(existingNode);

            var root = new TreeNode(container.Name, NodeType.Root, container, null);
            root.IsMainEditabelPack = _packFileService.GetEditablePack() == container;
            Dictionary<string, TreeNode> directoryMap = new Dictionary<string, TreeNode>();
           
            foreach (var item in container.FileList)
            {
                var fullPath = item.Key;
                var numSeperators = fullPath.Count(x=> x == Path.DirectorySeparatorChar);

                var directoryEnd = fullPath.LastIndexOf(Path.DirectorySeparatorChar);
                var fileName = fullPath.Substring(directoryEnd + 1);

                if (numSeperators == 0)
                {
                    root.Children.Add(new TreeNode(fileName, NodeType.File, container, root, item.Value));
                }
                else
                {
                    var directory = fullPath.Substring(0, directoryEnd);
                    var res = directoryMap.TryGetValue(directory, out var node);
                    if (!res)
                    {
                        var currentIndex = 0;
                        var lastIndex = 0;

                        TreeNode lastNode = root;
                        for (int i = 0; i < numSeperators; i++)
                        {
                            currentIndex = fullPath.IndexOf(Path.DirectorySeparatorChar, currentIndex);
                            var subStr = fullPath.Substring(0, currentIndex);
                            if (directoryMap.ContainsKey(subStr) == false)
                            {
                                var nodeName = subStr;
                                if(lastIndex != 0)
                                    nodeName = fullPath.Substring(lastIndex+1 , currentIndex - lastIndex-1);
                                var currentNode = new TreeNode(nodeName, NodeType.Directory, container, lastNode);
                                lastNode.Children.Add(currentNode);
                                directoryMap.Add(subStr, currentNode);
                                lastNode = currentNode;
                            }
                            else
                            {
                                lastNode = directoryMap[subStr];
                            }
                            lastIndex = currentIndex;
                            currentIndex++;
                        }
                    }
                    directoryMap[directory].Children.Add(new TreeNode(fileName, NodeType.File, container, directoryMap[directory], item.Value));
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
    }
}
