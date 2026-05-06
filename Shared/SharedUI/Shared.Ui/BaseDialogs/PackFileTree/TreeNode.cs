using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Diagnostics;
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
        private Func<TreeNode, bool>? _childLoader;
        private bool _childrenLoaded;
        private bool _isExpandedByFilter;
        private readonly bool _isPlaceholder;
        private readonly Func<bool>? _isFilterActive;
        private Func<TreeNode, bool>? _childVisibilityPredicate;

        public IPackFileContainer FileOwner { get; private set; }
        public PackFile? Item { get; set; }
        public TreeNode? Parent { get; set; }
        public List<TreeNode> BackingChildren { get; } = [];

        public bool HasChildren => BackingChildren.Count > 0 || (!_childrenLoaded && _childLoader != null && NodeType != NodeType.File);
        public bool ChildrenLoaded => _childrenLoaded;

        [ObservableProperty] public partial ObservableCollection<TreeNode> Children { get; set; } = [];
        [ObservableProperty] public partial bool UnsavedChanged { get; set; }
        [ObservableProperty] public partial bool IsMainEditabelPack { get; set; }
        [ObservableProperty] public partial bool IsVisible { get; set; } = true;
        [ObservableProperty] public partial string Name { get; set; }
        [ObservableProperty] public partial bool IsNodeExpanded { get; set; } = false;
        [ObservableProperty] public partial NodeType NodeType { get; private set; }

        internal bool HasMaterializedChildren => Children.Any(x => x._isPlaceholder == false);

        public TreeNode(string name, NodeType type, IPackFileContainer owner, TreeNode? parent, PackFile? packFile = null)
        {
            Name = name;
            Item = packFile;
            FileOwner = owner;
            Parent = parent;
            NodeType = type;

            if (string.IsNullOrWhiteSpace(name))
                throw new Exception($"Packfile name or folder is empty '{GetFullPath()}', this is not allowed! Please report as a bug if it happens outside of packfile loading! If it happens while loading clean up the packfile in RPFM");
        }

        internal TreeNode(string name, NodeType type, IPackFileContainer owner, TreeNode? parent, Func<bool> isFilterActive, PackFile? packFile = null)
        {
            _isFilterActive = isFilterActive;
            Name = name;
            Item = packFile;
            FileOwner = owner;
            Parent = parent;
            NodeType = type;

            if (string.IsNullOrWhiteSpace(name))
                throw new Exception($"Packfile name or folder is empty '{GetFullPath()}', this is not allowed! Please report as a bug if it happens outside of packfile loading! If it happens while loading clean up the packfile in RPFM");

            if (ShouldUsePlaceholder())
                AddPlaceholderChild();
        }

        private TreeNode(TreeNode parent)
        {
            _isPlaceholder = true;
            Name = "<placeholder>";
            FileOwner = parent.FileOwner;
            Parent = parent;
            NodeType = NodeType.File;
            IsVisible = false;
        }

        public void SetChildLoader(Func<TreeNode, bool> childLoader)
        {
            _childLoader = childLoader;
        }

        public void SetChildVisibilityPredicate(Func<TreeNode, bool>? predicate)
        {
            _childVisibilityPredicate = predicate;
        }

        public void MarkChildrenLoaded()
        {
            _childrenLoaded = true;
        }

        public void ResetChildrenLoaded()
        {
            _childrenLoaded = false;
        }

        public void EnsureChildrenPopulated()
        {
            if (_childrenLoaded || _childLoader == null || NodeType == NodeType.File)
                return;

            _childrenLoaded = _childLoader(this);
        }

        public void EnsureFullyPopulated()
        {
            EnsureChildrenPopulated();
            foreach (var child in BackingChildren)
                child.EnsureFullyPopulated();
        }

        public void AddChild(TreeNode child)
        {
            child.Parent = this;
            BackingChildren.Add(child);
        }

        public void RemoveChild(TreeNode child)
        {
            BackingChildren.Remove(child);
            child.Parent = null;
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
                current.EnsureChildrenPopulated();
                yield return current;

                for (var i = current.BackingChildren.Count - 1; i >= 0; i--)
                    stack.Push(current.BackingChildren[i]);
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
            foreach (var child in Children)
                child.RemoveSelf();

            Children.Clear();
            Parent = null;
        }

        public void ForeachNode(Action<TreeNode> func)
        {
            func.Invoke(this);
            foreach (var child in Children)
            {
                if (child._isPlaceholder)
                    continue;

                child.ForeachNode(func);
            }
        }

        public void ExpandIfVisible(bool includeChildren = true, bool markAsFilterExpansion = false)
        {
            if (IsVisible)
            {
                if (markAsFilterExpansion)
                    ExpandForFilter();
                else
                    IsNodeExpanded = true;

                MaterializeChildren();
                if (includeChildren)
                {
                    foreach (var child in Children)
                    {
                        if (child._isPlaceholder)
                            continue;

                        child.ExpandIfVisible(includeChildren);
                    }
                }
            }
        }

        public TreeNode AddDirectoryChild(string name)
        {
            EnsureChildrenPopulated();
            var newNode = new TreeNode(name, NodeType.Directory, FileOwner, this, _isFilterActive ?? (() => false));
            newNode.MarkChildrenLoaded();
            InsertChildSorted(this, newNode);

            if (HasMaterializedChildren || IsNodeExpanded || (_isFilterActive?.Invoke() ?? false))
                MaterializeChildren();

            if (Children.Count == 1 && Children[0]._isPlaceholder)
                MaterializeChildren();

            return newNode;
        }

        internal void RefreshLoadedBranch()
        {
            if (!HasMaterializedChildren)
                return;

            MaterializeChildren();
            foreach (var child in Children)
            {
                if (child._isPlaceholder)
                    continue;

                child.RefreshLoadedBranch();
            }
        }

        internal void MaterializeChildren()
        {
            EnsureChildrenPopulated();

            if (!HasChildren)
                return;

            // Propagate the visibility predicate to newly loaded children
            if (_childVisibilityPredicate != null)
            {
                foreach (var child in BackingChildren)
                {
                    if (child._childVisibilityPredicate != _childVisibilityPredicate)
                        child._childVisibilityPredicate = _childVisibilityPredicate;
                }
            }

            var childNodes = BackingChildren
                .Where(ShouldMaterializeChild)
                .ToList();

            Children.Clear();
            foreach (var child in childNodes)
            {
                child.Parent = this;
                Children.Add(child);

                if (child.IsNodeExpanded && !child.HasMaterializedChildren)
                    child.MaterializeChildren();
            }

            if (Children.Count == 0 && ShouldUsePlaceholder())
                AddPlaceholderChild();
        }

        internal void EnsureChildrenLoaded()
        {
            MaterializeChildren();
        }

        internal void NormalizeLazyState()
        {
            if (IsNodeExpanded)
            {
                MaterializeChildren();
                foreach (var child in Children)
                {
                    if (child._isPlaceholder)
                        continue;

                    child.NormalizeLazyState();
                }
            }
            else
            {
                UnloadChildren();
            }
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
            {
                if (child._isPlaceholder)
                    continue;

                child.ClearFilterExpansion();
            }

            if (_isExpandedByFilter)
            {
                _isExpandedByFilter = false;
                IsNodeExpanded = false;
            }
        }

        internal void AbsorbFilterExpansion()
        {
            foreach (var child in Children)
            {
                if (child._isPlaceholder)
                    continue;

                child.AbsorbFilterExpansion();
            }

            _isExpandedByFilter = false;
        }

        partial void OnIsNodeExpandedChanged(bool value)
        {
            if (value)
                MaterializeChildren();
            else
                UnloadChildren();

            LogLoadedNodeCount(value);
        }

        private void UnloadChildren()
        {
            foreach (var child in Children)
                child.RemoveSelf();

            Children.Clear();

            if (ShouldUsePlaceholder())
                AddPlaceholderChild();
        }

        private bool ShouldMaterializeChild(TreeNode child)
        {
            if (_isFilterActive?.Invoke() != true)
                return _childVisibilityPredicate?.Invoke(child) ?? true;

            if (_childVisibilityPredicate != null && !_childVisibilityPredicate(child))
                return false;

            return child.IsVisible;
        }

        private bool ShouldUsePlaceholder()
        {
            return HasChildren && !IsNodeExpanded && (_isFilterActive?.Invoke() != true);
        }

        private void AddPlaceholderChild()
        {
            if (Children.Any(x => x._isPlaceholder))
                return;

            Children.Add(new TreeNode(this));
        }

        public int CountLoadedNodes()
        {
            var count = 1;
            foreach (var child in Children)
            {
                if (child._isPlaceholder)
                    continue;

                count += child.CountLoadedNodes();
            }

            return count;
        }

        [Conditional("DEBUG")]
        private void LogLoadedNodeCount(bool expanded)
        {
            var root = this;
            while (root.Parent != null)
                root = root.Parent;

            var loadedNodes = root.CountLoadedNodes();
            var action = expanded ? "expanded" : "collapsed";
            Console.WriteLine($"[PackFileTree] Node '{Name}' {action}. Loaded nodes: {loadedNodes}");
        }

        private static readonly Comparison<TreeNode> ChildComparison = (left, right) =>
        {
            var nodeTypeComparison = left.NodeType.CompareTo(right.NodeType);
            if (nodeTypeComparison != 0)
                return nodeTypeComparison;

            return StringComparer.CurrentCultureIgnoreCase.Compare(left.Name, right.Name);
        };

        private static void InsertChildSorted(TreeNode parent, TreeNode child)
        {
            parent.EnsureChildrenPopulated();
            parent.AddChild(child);
            parent.BackingChildren.Sort(ChildComparison);
        }
    }
}
