using Common;
using System;
using System.Collections.Generic;
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

        public ICollectionView Children { get; set; }

        public TreeNode(IPackFile source)
        {
            Item = source;
            if (Item.Children.Count() != 0)
            {
                var temp_childList = new List<TreeNode>(Item.Children.Count());
                foreach (var child in Item.Children)
                    temp_childList.Add(new TreeNode(child));

                Children = CollectionViewSource.GetDefaultView(temp_childList);
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
