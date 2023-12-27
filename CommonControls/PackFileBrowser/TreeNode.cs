using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommonControls.Common;
using CommonControls.FileTypes.PackFiles.Models;

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
        public PackFile Item { get; set; }

        bool _isNodeExpanded = false;
        public bool IsNodeExpanded
        {
            get => _isNodeExpanded;
            set
            {
                SetAndNotify(ref _isNodeExpanded, value);
            }
        }

        public NodeType NodeType { get; set; }
        public TreeNode Parent { get; set; }
        public ObservableCollection<TreeNode> Children { get; set; } = new ObservableCollection<TreeNode>();

        bool _unsavedChanged;
        public bool UnsavedChanged { get => _unsavedChanged; set => SetAndNotify(ref _unsavedChanged, value); }

        bool _isMainEditabelPack;
        public bool IsMainEditabelPack { get => _isMainEditabelPack; set => SetAndNotify(ref _isMainEditabelPack, value); }

        bool _Visibility = true;
        public bool IsVisible { get => _Visibility; set => SetAndNotify(ref _Visibility, value); }

        string _name = "";
        public string Name { get => _name; set => SetAndNotify(ref _name, value); }
        public TreeNode(string name, NodeType type, PackFileContainer ower, TreeNode parent, PackFile packFile = null)
        {
            Name = name;
            NodeType = type;
            Item = packFile;
            FileOwner = ower;
            Parent = parent;

            if (string.IsNullOrWhiteSpace(name))
                throw new Exception($"Packfile name or folder is empty '{GetFullPath()}', this is not allowed! Please report as a bug if it happens outside of packfile loading! If it happens while loading clean up the packfile in RPFM");
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


        public List<TreeNode> GetAllChildFileNodes()
        {
            var output = new List<TreeNode>();

            var nodes = new Stack<TreeNode>(new[] { this });
            while (nodes.Any())
            {
                TreeNode node = nodes.Pop();
                if (node.NodeType == NodeType.File)
                    output.Add(node);

                foreach (var n in node.Children)
                    nodes.Push(n);
            }


            return output;
        }

        public void RemoveSelf()
        {
            foreach (var child in Children)
                child.RemoveSelf();

            Children.Clear();
            Parent = null;
        }

        public void ForeachNode(Action<TreeNode> func)
        {
            func.Invoke(this);
            foreach (var child in Children)
                child.ForeachNode(func);
        }

        public void ExpandIfVisible(bool includeChildren = true)
        {
            if (IsVisible)
            {
                IsNodeExpanded = true;
                if (includeChildren)
                {
                    foreach (var child in Children)
                        child.ExpandIfVisible(includeChildren);
                }
            }
        }
    }
}
