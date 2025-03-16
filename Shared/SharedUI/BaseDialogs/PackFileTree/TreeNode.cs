using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using Shared.Core.PackFiles.Models;

namespace Shared.Ui.BaseDialogs.PackFileTree
{
    public enum NodeType
    {
        Root,
        Directory,
        File
    }

    public partial class TreeNode : ObservableObject
    {
        public PackFileContainer FileOwner { get; private set; }
        public PackFile? Item { get; set; }
        public TreeNode? Parent { get; set; }

        [ObservableProperty] public partial ObservableCollection<TreeNode> Children { get; set; } = [];
        [ObservableProperty] public partial bool UnsavedChanged { get; set; }
        [ObservableProperty] public partial bool IsMainEditabelPack { get; set; }
        [ObservableProperty] public partial bool IsVisible { get; set; } = true;
        [ObservableProperty] public partial string Name { get; set; }
        [ObservableProperty] public partial bool IsNodeExpanded { get; set; } = false;
        [ObservableProperty] public partial NodeType NodeType { get; private set; }

        public TreeNode(string name, NodeType type, PackFileContainer ower, TreeNode? parent, PackFile? packFile = null)
        {
            Name = name;
            Item = packFile;
            FileOwner = ower;
            Parent = parent;
            NodeType = type;

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

        public override string ToString() => Name;

        public List<TreeNode> GetAllChildFileNodes()
        {
            var output = new List<TreeNode>();

            var nodes = new Stack<TreeNode>(new[] { this });
            while (nodes.Any())
            {
                var node = nodes.Pop();
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
