using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using Shared.Core.PackFiles.Models;

namespace Shared.Ui.BaseDialogs.PackFileBrowser
{
    public enum NodeType
    {
        Root,
        Directory,
        File
    }

    public partial class TreeNode : ObservableObject
    {
        public PackFileContainer FileOwner { get; set; }
        public PackFile? Item { get; set; }
        public TreeNode Parent { get; set; }

        [ObservableProperty] ObservableCollection<TreeNode> _children = [];
        [ObservableProperty] bool _unsavedChanged;
        [ObservableProperty] bool _isMainEditabelPack;
        [ObservableProperty] bool _isVisible = true;
        [ObservableProperty] string _name = "";
        [ObservableProperty] bool _isNodeExpanded = false;
        [ObservableProperty] NodeType _nodeType;
        
        public TreeNode(string name, NodeType type, PackFileContainer ower, TreeNode parent, PackFile? packFile = null)
        {
            Name = name;
            Item = packFile;
            FileOwner = ower;
            Parent = parent;
            NodeType = type;

            if (string.IsNullOrWhiteSpace(name))
                throw new Exception($"Packfile name or folder is empty '{GetFullPath()}', this is not allowed! Please report as a bug if it happens outside of packfile loading! If it happens while loading clean up the packfile in RPFM");
        }

        public NodeType GetNodeType() => NodeType;

        public string GetFullPath()
        {
            if (GetNodeType() == NodeType.Root)
                return "";

            var currentParent = Parent;
            var path = Name;
            while (currentParent != null)
            {
                if (currentParent.GetNodeType() == NodeType.Root)
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
                var node = nodes.Pop();
                if (node.GetNodeType() == NodeType.File)
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
