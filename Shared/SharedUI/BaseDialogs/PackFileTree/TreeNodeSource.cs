using System;
using System.Collections.Generic;
using Shared.Core.PackFiles.Models;

namespace Shared.Ui.BaseDialogs.PackFileTree
{
    internal sealed class TreeNodeSource
    {
        public string Name { get; set; }
        public NodeType NodeType { get; }
        public PackFileContainer FileOwner { get; }
        public PackFile? Item { get; }
        public TreeNodeSource? Parent { get; private set; }
        public List<TreeNodeSource> Children { get; } = [];
        public bool IsVisible { get; set; } = true;
        public bool UnsavedChanged { get; set; }
        public TreeNode? MaterializedNode { get; set; }

        public bool HasChildren => Children.Count > 0;

        public TreeNodeSource(string name, NodeType nodeType, PackFileContainer fileOwner, TreeNodeSource? parent, PackFile? item = null)
        {
            Name = name;
            NodeType = nodeType;
            FileOwner = fileOwner;
            Parent = parent;
            Item = item;

            if (string.IsNullOrWhiteSpace(name))
                throw new Exception($"Packfile name or folder is empty '{GetFullPath()}', this is not allowed! Please report as a bug if it happens outside of packfile loading! If it happens while loading clean up the packfile in RPFM");
        }

        public string GetFullPath()
        {
            if (NodeType == NodeType.Root)
                return string.Empty;

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

        public void AddChild(TreeNodeSource child)
        {
            child.Parent = this;
            Children.Add(child);
        }

        public void RemoveChild(TreeNodeSource child)
        {
            Children.Remove(child);
            child.Parent = null;
        }

        public IEnumerable<TreeNodeSource> EnumerateAllNodesDepthFirst()
        {
            var stack = new Stack<TreeNodeSource>();
            stack.Push(this);

            while (stack.Count > 0)
            {
                var current = stack.Pop();
                yield return current;

                for (var i = current.Children.Count - 1; i >= 0; i--)
                    stack.Push(current.Children[i]);
            }
        }

        public IEnumerable<TreeNodeSource> EnumerateFileNodesDepthFirst()
        {
            foreach (var node in EnumerateAllNodesDepthFirst())
            {
                if (node.NodeType == NodeType.File)
                    yield return node;
            }
        }
    }
}