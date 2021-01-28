using Common;
using CommonControls.Simple;
using FileTypes.PackFiles.Models;
using FileTypes.PackFiles.Services;
using GalaSoft.MvvmLight.CommandWpf;
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


        TreeNode _selectedItem;
        public TreeNode SelectedItem{get => _selectedItem; set => SetAndNotify(ref _selectedItem, value);}


        public FileTreeViewModel(PackFileService packFileService)
        {
            _packFileService = packFileService;
            _packFileService.Database.PackFileContainerLoaded += PackFileContainerLoaded;

            PackFileNodes = CollectionViewSource.GetDefaultView(_nodes);
            Filter = new PackFileFilter(PackFileNodes);

            DoubleClickCommand = new RelayCommand<TreeNode>(OnDoubleClick);
            RenameNodeCommand = new RelayCommand<TreeNode>(OnRenameNode);
        }

        private void PackFileContainerLoaded(PackFileContainer container)
        {
            TreeNode collectionNode = new TreeNode(container);
            ObservableCollection<TreeNode> collectionChildren = new ObservableCollection<TreeNode>();
            
            foreach (var file in container.FileChildren)
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

        void OnDoubleClick(TreeNode t)
        {
            if (t.Item.PackFileType() == PackFileType.Data)
                FileOpen?.Invoke(t.Item);
        }
    }
}
