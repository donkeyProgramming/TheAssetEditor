using Common;
using CommonControls.Common;
using CommonControls.Simple;
using FileTypes.PackFiles.Models;
using FileTypes.PackFiles.Services;
using GalaSoft.MvvmLight.Command;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

namespace CommonControls.PackFileBrowser
{
    public delegate void FileSelectedDelegate(IPackFile file);

    public class PackFileBrowserViewModel : NotifyPropertyChangedImpl, IDisposable
    {
        protected PackFileService _packFileService;
        public event FileSelectedDelegate FileOpen;

        bool _packFileTreeRebuildSuspended = false;

        public ObservableCollection<TreeNode> Files { get; set; } = new ObservableCollection<TreeNode>();
        public PackFileFilter Filter { get; private set; }
        public ICommand DoubleClickCommand { get; set; }

        TreeNode _selectedItem;
        public TreeNode SelectedItem { get => _selectedItem; set => SetAndNotify(ref _selectedItem, value); }


        Visibility _contextMenuVisibility = Visibility.Visible;
        public Visibility ContextMenuVisibility { get => _contextMenuVisibility; set => SetAndNotify(ref _contextMenuVisibility, value); }

        public ICommand RenameNodeCommand { get; set; }
        public ICommand AddFilesFromDirectory { get; set; }
        public ICommand AddFilesCommand { get; set; }
        
        public ICommand CloseNodeCommand { get; set; }
        public ICommand DeleteCommand { get; set; }

        public ICommand SavePackFileCommand { get; set; }

        public ICommand CopyNodePathCommand { get; set; }
        public ICommand CopyToEditablePackCommand { get; set; }

        public ICommand SetAsEditabelPackCommand { get; set; }
        public ICommand ExpandAllChildrenCommand { get; set; }

        public PackFileBrowserViewModel(PackFileService packFileService)
        {
            RenameNodeCommand = new RelayCommand<TreeNode>(OnRenameNode);
            AddFilesCommand = new RelayCommand<TreeNode>(OnAddFilesCommand);
            AddFilesFromDirectory = new RelayCommand<TreeNode>(OnAddFilesFromDirectory);
            CloseNodeCommand = new RelayCommand<TreeNode>(CloseNode);
            DeleteCommand = new RelayCommand<TreeNode>(DeleteNode);
            SavePackFileCommand = new RelayCommand<TreeNode>(SavePackFile);
            CopyNodePathCommand = new RelayCommand<TreeNode>(CopyNodePath);
            CopyToEditablePackCommand = new RelayCommand<TreeNode>(CopyToEditablePack);
            SetAsEditabelPackCommand = new RelayCommand<TreeNode>(SetAsEditabelPack);
            ExpandAllChildrenCommand = new RelayCommand<TreeNode>(ExpandAllChildren);
            DoubleClickCommand = new RelayCommand<TreeNode>(OnDoubleClick);

            _packFileService = packFileService;
            _packFileService.Database.PackFileContainerLoaded += PackFileContainerLoaded;
            _packFileService.Database.PackFileContainerRemoved += PackFileContainerRemoved;
            _packFileService.Database.ContainerUpdated += ContainerUpdated;

            Filter = new PackFileFilter(Files);

            foreach (var item in _packFileService.Database.PackFiles)
                PackFileContainerLoaded(item);
        }

        void OnRenameNode(TreeNode treeNode)
        {
            if (treeNode.FileOwner.IsCaPackFile)
            {
                MessageBox.Show("Unable to edit CA packfile");
                return;
            }

            if (treeNode.NodeType == NodeType.Directory)
            {
                MessageBox.Show("Not possible to rename a directory at this point");
            }
            else if (treeNode.NodeType == NodeType.File)
            {
                TextInputWindow window = new TextInputWindow("Rename file", treeNode.Item.Name);
                if (window.ShowDialog() == true)
                {
                    _packFileService.RenameFile(treeNode.FileOwner, treeNode.Item, window.TextValue);
                    treeNode.Name = treeNode.Item.Name;
                }
            }
        }

        void OnAddFilesCommand(TreeNode node)
        {
            if (node.FileOwner.IsCaPackFile)
            {
                MessageBox.Show("Unable to edit CA packfile");
                return;
            }

            var dialog = new CommonOpenFileDialog();
            dialog.IsFolderPicker = false;
            dialog.Multiselect = true;
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                var parentPath = node.GetFullPath();
                var files = dialog.FileNames;
                foreach(var file in files)
                {
                    var fileName = Path.GetFileName(file);
                    var packFile = new PackFile(fileName, new MemorySource(File.ReadAllBytes(file)));
                    _packFileService.AddFileToPack(node.FileOwner, parentPath, packFile);
                }
            }
        }

        void OnAddFilesFromDirectory(TreeNode node)
        {
            if (node.FileOwner.IsCaPackFile)
            {
                MessageBox.Show("Unable to edit CA packfile");
                return;
            }

            var dialog = new CommonOpenFileDialog();
            dialog.IsFolderPicker = true;
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                var parentPath = node.GetFullPath();
                _packFileService.AddFolderContent(node.FileOwner, parentPath, dialog.FileName);
            }
        }

        void DeleteNode(TreeNode node)
        {
            if (node.FileOwner.IsCaPackFile)
            {
                MessageBox.Show("Unable to edit CA packfile");
                return;
            }
        }


        void CloseNode(TreeNode node)
        {
            _packFileService.UnloadPackContainer(node.FileOwner);
        }

        void SavePackFile(TreeNode node)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            if (saveFileDialog.ShowDialog() == true)
                _packFileService.Save(node.FileOwner, saveFileDialog.FileName);
        }


        void CopyNodePath(TreeNode node)
        {
            if (node.Item != null)
            {
                var path = _packFileService.GetFullPath(node.Item as PackFile);
                Clipboard.SetText(path);
            }
        }

        void CopyToEditablePack(TreeNode node)
        {
            if (_packFileService.GetEditablePack() == null)
            {
                MessageBox.Show("No editable pack selected!");
                return;
            }

            try
            {
                _packFileTreeRebuildSuspended = true;

                using (new WaitCursor())
                {
                    var files = node.GetAllChildFileNodes();
                    foreach (var file in files)
                        _packFileService.CopyFileFromOtherPackFile(file.FileOwner, file.GetFullPath(), _packFileService.GetEditablePack());
                }
            }
            finally
            {
                _packFileTreeRebuildSuspended = false;
                PackFileContainerLoaded(_packFileService.GetEditablePack());
            }

            
        }

        void SetAsEditabelPack(TreeNode node)
        {
            if (node.FileOwner.IsCaPackFile)
            {
                MessageBox.Show("Unable to edit CA packfile");
                return;
            }

            _packFileService.SetEditablePack(node.FileOwner);

            foreach (var item in Files)
                item.IsMainEditabelPack = false;

            var rootNode = node;
            while (rootNode.Parent != null)
                rootNode = rootNode.Parent;
            rootNode.IsMainEditabelPack = true;
        }

        void ExpandAllChildren(TreeNode node)
        {
            node.IsNodeExpanded = true;
            foreach (var child in node.Children)
                ExpandAllChildren(child);
        }

        protected virtual void OnDoubleClick(TreeNode node) 
        {
            // using command parmeter to get node causes memory leaks, using selected node for now
            if (SelectedItem.NodeType == NodeType.File)
                FileOpen?.Invoke(SelectedItem.Item); 
        }

        private void ContainerUpdated(PackFileContainer pf)
        {
            PackFileContainerLoaded(pf);
        }

        private void PackFileContainerLoaded(PackFileContainer container)
        {
            if (_packFileTreeRebuildSuspended)
                return;
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
            root.IsNodeExpanded = true;
        }

        private void PackFileContainerRemoved(PackFileContainer container)
        {
            var node = Files.FirstOrDefault(x => x.FileOwner == container);
            Files.Remove(node);
        }

        public void Dispose()
        {
            _packFileService.Database.PackFileContainerLoaded -= PackFileContainerLoaded;
            _packFileService.Database.PackFileContainerRemoved -= PackFileContainerRemoved;
            _packFileService.Database.ContainerUpdated -= ContainerUpdated;
        }
    }
}
