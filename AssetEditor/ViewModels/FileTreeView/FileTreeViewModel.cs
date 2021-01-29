using Common;
using CommonControls.Simple;
using FileTypes.PackFiles.Models;
using FileTypes.PackFiles.Services;
using GalaSoft.MvvmLight.CommandWpf;
using Microsoft.WindowsAPICodePack.Dialogs;
using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;

namespace AssetEditor.ViewModels.FileTreeView
{
    public delegate void FileSelectedDelegate(IPackFile file);

    class FileTreeViewModel : NotifyPropertyChangedImpl
    {
        ILogger _logger = Logging.Create<FileTreeViewModel>();
        PackFileService _packFileService;

        public event FileSelectedDelegate FileOpen;

        public ICollectionView PackFileNodes { get; set; }
        ObservableCollection<TreeNode> _nodes = new ObservableCollection<TreeNode>();
        public PackFileFilter Filter { get; private set; }


        public ICommand DoubleClickCommand { get; set; }
        public ICommand RenameNodeCommand { get; set; }
        public ICommand AddEmptyFolderCommand { get; set; }
        public ICommand AddFilesFromDirectory { get; set; }
        


        TreeNode _selectedItem;
        public TreeNode SelectedItem{get => _selectedItem; set => SetAndNotify(ref _selectedItem, value);}


        public FileTreeViewModel(PackFileService packFileService)
        {
            _packFileService = packFileService;
            _packFileService.Database.PackFileContainerLoaded += PackFileContainerLoaded;
            _packFileService.Database.FileAdded += FileAdded;

            PackFileNodes = CollectionViewSource.GetDefaultView(_nodes);
            Filter = new PackFileFilter(PackFileNodes);

            DoubleClickCommand = new RelayCommand<TreeNode>(OnDoubleClick);
            RenameNodeCommand = new RelayCommand<TreeNode>(OnRenameNode);
            AddEmptyFolderCommand = new RelayCommand<TreeNode>(OnAddNewFolder);
            AddFilesFromDirectory = new RelayCommand<TreeNode>(OnAddFilesFromDirectory);
        }

        private void FileAdded(IPackFile newNode, IPackFile parentNode)
        {
            var parent = Find(parentNode, _nodes.First());
            parent.Build(parentNode);
            parent.IsNodeExpanded = true;
        }

        private void PackFileContainerLoaded(PackFileContainer container)
        {
            TreeNode collectionNode = new TreeNode(container);
            ObservableCollection<TreeNode> collectionChildren = new ObservableCollection<TreeNode>();
            
            foreach (var file in container.Children)
                collectionChildren.Add(new TreeNode(file));
            
            collectionNode.Children = CollectionViewSource.GetDefaultView(collectionChildren);

            _nodes.Add(collectionNode);
            _nodes.Last().IsNodeExpanded = true;
        } 

        void OnRenameNode(TreeNode treeNode) 
        {
            TextInputWindow window = new TextInputWindow("Rename file", treeNode.Item.Name);
            if (window.ShowDialog() == true)
                _packFileService.RenameFile(treeNode.Item, window.TextValue);
        }

        void OnDoubleClick(TreeNode node)
        {
            if (node.Item.PackFileType() == PackFileType.Data)
                FileOpen?.Invoke(node.Item);
        }

        void OnAddNewFolder(TreeNode node)
        {
            if (node.Item.PackFileType() != PackFileType.Data)
                _packFileService.AddEmptyFolder(node.Item as PackFileDirectory, "name");
        }

        void OnAddFilesFromDirectory(TreeNode node)
        {
           var dialog = new CommonOpenFileDialog();
           dialog.IsFolderPicker = true;
           if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
           {
               _logger.Here().Information($"Adding content of {dialog.FileName} to pack");
               _packFileService.AddFolderContent(node.Item, dialog.FileName);
           }
        }

        TreeNode Find(IPackFile pack, TreeNode node)
        {
            if (node.Item == pack)
                return node;

            if (node.Children != null)
            {
                foreach (TreeNode child in node.Children)
                {
                    var res = Find(pack, child);
                    if (res != null)
                        return res;
                }
            }

            return null;
        }
    }
}
