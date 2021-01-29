using Common;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows.Data;

namespace AssetEditor.ViewModels.FileTreeView
{
    class TreeNode : NotifyPropertyChangedImpl
    {
        public IPackFile Item { get; set; }
        bool _isExpanded = false;
        public bool IsNodeExpanded
        {
            get => _isExpanded;
            set => SetAndNotify(ref _isExpanded, value);
        }

        public bool IsPackContainer { get { return Item.PackFileType() == PackFileType.PackContainer; } }

        ICollectionView _children;
        public ICollectionView Children { get => _children; set => SetAndNotify(ref _children, value); }

        public TreeNode(IPackFile source)
        {
            Build(source);
        }

        public void Build(IPackFile source)
        {
            Item = source;
            if (Item.Children.Count() != 0)
            {
                var _internalChildList = new List<TreeNode>(Item.Children.Count());
                foreach (var child in Item.Children)
                    _internalChildList.Add(new TreeNode(child));
                Children = CollectionViewSource.GetDefaultView(_internalChildList);
            }
        }

        public void SetFilter(Predicate<object> filterFunc)
        {
            if (Children != null)
            {
                Children.Filter = filterFunc;
                foreach (var child in Children)
                    (child as TreeNode).SetFilter(filterFunc);
            }
        }
    }

}
