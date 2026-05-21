using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using Serilog;
using Shared.Core.ErrorHandling;
using Shared.Core.PackFiles.Models;

namespace Shared.Ui.BaseDialogs.PackFileTree
{
    public enum NodeType
    {
        Root,
        Directory,
        File
    }

    public partial class RootTreeNode : TreeNode
    {
        public IPackFileContainer Owner { get; }
        public UnsavedChangesTracker UnsavedChanges { get; } = new();

        [ObservableProperty] public partial bool IsMainEditabelPack { get; set; }

        public RootTreeNode(string name, IPackFileContainer owner) : 
            base(name, NodeType.Root, null)
        {

            Owner = owner;
        }
    }

    public partial class TreeNode : ObservableObject
    {
        private bool _isExpandedByFilter;

        public TreeNode? Parent { get; set; }

        public bool HasChildren => Children.Count > 0;

        [ObservableProperty] public partial ObservableCollection<TreeNode> Children { get; set; } = [];
        [ObservableProperty] public partial bool IsVisible { get; set; } = true;
        [ObservableProperty] public partial string Name { get; set; }
        [ObservableProperty] public partial bool IsNodeExpanded { get; set; } = false;
        [ObservableProperty] public partial NodeType NodeType { get; private set; }

        public bool UnsavedChanged => Utility.TreeNodeHelper.GetRootNode(this)?.UnsavedChanges.IsChanged(this) ?? false;

        public void NotifyUnsavedChangedChanged() => OnPropertyChanged(nameof(UnsavedChanged));

        public TreeNode(string name, NodeType type, TreeNode? parent)
        {
            Name = name;
            Parent = parent;
            NodeType = type;

            if (string.IsNullOrWhiteSpace(name))
            {
                throw new Exception($"Packfile name or folder is empty '{GetFullPath()}', this is not allowed! Please report as a bug if it happens outside of packfile loading! If it happens while loading clean up the packfile in RPFM");
            }
        }

        public void AddChild(TreeNode child)
        {
            child.Parent = this;
            Children.Add(child);
        }

        public void RemoveChild(TreeNode child)
        {
            Children.Remove(child);
            child.Parent = null;
        }

        internal void SetChildren(List<TreeNode> children)
        {
            Children = new ObservableCollection<TreeNode>(children);
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
            return EnumerateFileNodesDepthFirst().ToList();
        }

        public IEnumerable<TreeNode> EnumerateAllNodesDepthFirst()
        {
            var stack = new Stack<TreeNode>();
            stack.Push(this);

            while (stack.Count > 0)
            {
                var current = stack.Pop();
                yield return current;

                for (var i = current.Children.Count - 1; i >= 0; i--)
                    stack.Push(current.Children[i]);
            }
        }

        public IEnumerable<TreeNode> EnumerateFileNodesDepthFirst()
        {
            foreach (var node in EnumerateAllNodesDepthFirst())
            {
                if (node.NodeType == NodeType.File)
                    yield return node;
            }
        }

        public void RemoveSelf()
        {
            foreach (var child in Children.ToList())
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

        public void ExpandIfVisible(bool includeChildren = true, bool markAsFilterExpansion = false)
        {
            if (!IsVisible)
                return;

            if (markAsFilterExpansion)
                ExpandForFilter();
            else
                IsNodeExpanded = true;

            if (!includeChildren)
                return;

            foreach (var child in Children)
                child.ExpandIfVisible(includeChildren, markAsFilterExpansion);
        }

        internal void ExpandForFilter()
        {
            if (!IsNodeExpanded)
                _isExpandedByFilter = true;

            IsNodeExpanded = true;
        }

        internal void ClearFilterExpansion()
        {
            foreach (var child in Children)
                child.ClearFilterExpansion();

            if (_isExpandedByFilter)
            {
                _isExpandedByFilter = false;
                IsNodeExpanded = false;
            }
        }

        internal void AbsorbFilterExpansion()
        {
            foreach (var child in Children)
                child.AbsorbFilterExpansion();

            _isExpandedByFilter = false;
        }

    
    }
}
