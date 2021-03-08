using Common;
using FileTypes.PackFiles.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Data;

namespace CommonControls.PackFileBrowser
{
    public enum NodeType
    { 
        Root,
        Directory,
        File
    }

    public class TreeNode : NotifyPropertyChangedImpl
    {
        public PackFileContainer FileOwner { get; set; }
        public IPackFile Item { get; set; }

        bool _isExpanded = false;
        public bool IsNodeExpanded
        {
            get => _isExpanded;
            set => SetAndNotify(ref _isExpanded, value);
        }

        public NodeType NodeType { get; set; }
        public TreeNode Parent { get; set; }

        public ObservableCollection<TreeNode> Children { get; set; } = new ObservableCollection<TreeNode>();

        bool _Visibility = true;
        public bool IsVisible { get => _Visibility; set => SetAndNotify(ref _Visibility, value); }

        string _name = "";
        public string Name { get => _name; set => SetAndNotify(ref _name, value); }
        public TreeNode(string name, NodeType type, PackFileContainer ower, TreeNode parent, IPackFile packFile = null)
        {
            Name = name;
            NodeType = type;
            Item = packFile;
            FileOwner = ower;
            Parent = parent;
        }

        public string GetFullPath()
        {
            if (NodeType == NodeType.Root)
                return "";

            var currentParent = Parent;
            var path = Name;
            while (currentParent != null)
            {
                if (currentParent.NodeType == NodeType.Root)
                    break;

                path = currentParent.Name + "\\" + path;
                currentParent = currentParent.Parent;
            }

            return path;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
