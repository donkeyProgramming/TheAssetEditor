using Common;
using CommonControls.Simple;
using FileTypes.PackFiles.Models;
using FileTypes.PackFiles.Services;
using GalaSoft.MvvmLight.Command;
using Microsoft.WindowsAPICodePack.Dialogs;
using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Windows.Input;

namespace CommonControls.PackFileBrowser
{
    public class PackFileBrowserViewModel : NotifyPropertyChangedImpl
    {
        protected PackFileService _packFileService;

        public ICollectionView PackFileNodes { get; set; }
        ObservableCollection<TreeNode> _nodes = new ObservableCollection<TreeNode>();
        public PackFileFilter Filter { get; private set; }
        public ICommand DoubleClickCommand { get; set; }

        TreeNode _selectedItem;
        public TreeNode SelectedItem { get => _selectedItem; set => SetAndNotify(ref _selectedItem, value); }


        public PackFileBrowserViewModel(PackFileService packFileService)
        {
            _packFileService = packFileService;
            _packFileService.Database.PackFileContainerLoaded += PackFileContainerLoaded;
            _packFileService.Database.FileAdded += FileAdded;

            PackFileNodes = CollectionViewSource.GetDefaultView(_nodes);
            Filter = new PackFileFilter(PackFileNodes);

            DoubleClickCommand = new RelayCommand<TreeNode>(OnDoubleClick);
        }

        protected virtual void OnDoubleClick(TreeNode node) { }

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
