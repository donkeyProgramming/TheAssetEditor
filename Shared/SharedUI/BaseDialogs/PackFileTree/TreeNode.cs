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
        private readonly TreeNodeSource? _source;
        private readonly Func<TreeNodeSource, TreeNode?, TreeNode>? _nodeFactory;
        private readonly Func<bool>? _isFilterActive;
        private bool _isExpandedByFilter;
        private readonly bool _isPlaceholder;

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

        internal TreeNodeSource? Source => _source;
        internal bool HasMaterializedChildren => Children.Any(x => x._isPlaceholder == false);

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

        internal TreeNode(TreeNodeSource source, TreeNode? parent, Func<TreeNodeSource, TreeNode?, TreeNode> nodeFactory, Func<bool> isFilterActive)
        {
            _source = source;
            _nodeFactory = nodeFactory;
            _isFilterActive = isFilterActive;

            Name = source.Name;
            Item = source.Item;
            FileOwner = source.FileOwner;
            Parent = parent;
            NodeType = source.NodeType;
            UnsavedChanged = source.UnsavedChanged;
            IsVisible = source.IsVisible;

            source.MaterializedNode = this;

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

        public string GetFullPath()
        {
            if (_source != null)
                return _source.GetFullPath();

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
            if (_source != null)
            {
                return _source.EnumerateFileNodesDepthFirst()
                    .Select(sourceNode => sourceNode.MaterializedNode ?? _nodeFactory!(sourceNode, null))
                    .ToList();
            }

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
            if (_source != null)
                _source.MaterializedNode = null;

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

                EnsureChildrenLoaded();
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
            var newNode = new TreeNode(name, NodeType.Directory, FileOwner, this);

            if (_source == null)
            {
                Children.Add(newNode);
                return newNode;
            }

            var sourceNode = new TreeNodeSource(name, NodeType.Directory, FileOwner, _source);
            _source.AddChild(sourceNode);

            if (HasMaterializedChildren || IsNodeExpanded || (_isFilterActive?.Invoke() ?? false))
                EnsureChildrenLoaded();

            if (Children.Count == 1 && Children[0]._isPlaceholder)
                EnsureChildrenLoaded();

            return sourceNode.MaterializedNode ?? newNode;
        }

        internal void SyncFromSource()
        {
            if (_source == null)
                return;

            Name = _source.Name;
            UnsavedChanged = _source.UnsavedChanged;
            IsVisible = _source.IsVisible;
        }

        internal void RefreshLoadedBranch()
        {
            SyncFromSource();

            if (!HasMaterializedChildren)
                return;

            EnsureChildrenLoaded();
            foreach (var child in Children)
            {
                if (child._isPlaceholder)
                    continue;

                child.RefreshLoadedBranch();
            }
        }

        internal void EnsureChildrenLoaded()
        {
            if (_source == null || !_source.HasChildren || _nodeFactory == null)
                return;

            var existingChildren = Children
                .Where(x => x._isPlaceholder == false && x._source != null)
                .ToDictionary(x => x._source!, x => x);

            var childSources = _source.Children
                .Where(ShouldMaterializeChild)
                .ToList();

            foreach (var removedChild in Children.Where(x => x._isPlaceholder == false && x._source != null && childSources.Contains(x._source) == false).ToList())
            {
                removedChild.RemoveSelf();
            }

            Children.Clear();
            foreach (var childSource in childSources)
            {
                if (!existingChildren.TryGetValue(childSource, out var childNode))
                    childNode = _nodeFactory(childSource, this);

                childNode.Parent = this;
                childNode.SyncFromSource();
                Children.Add(childNode);
            }

            if (Children.Count == 0 && ShouldUsePlaceholder())
                AddPlaceholderChild();
        }

        internal void NormalizeLazyState()
        {
            if (_source == null)
                return;

            if (IsNodeExpanded)
            {
                EnsureChildrenLoaded();
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

        partial void OnIsNodeExpandedChanged(bool value)
        {
            if (_source == null)
                return;

            if (value)
                EnsureChildrenLoaded();
            else
                UnloadChildren();

            LogLoadedNodeCount(value);
        }

        partial void OnNameChanged(string value)
        {
            if (_source != null && !_isPlaceholder)
                _source.Name = value;
        }

        partial void OnUnsavedChangedChanged(bool value)
        {
            if (_source != null && !_isPlaceholder)
                _source.UnsavedChanged = value;
        }

        partial void OnIsVisibleChanged(bool value)
        {
            if (_source != null && !_isPlaceholder)
                _source.IsVisible = value;
        }

        private void UnloadChildren()
        {
            if (_source == null)
                return;

            foreach (var child in Children)
                child.RemoveSelf();

            Children.Clear();

            if (ShouldUsePlaceholder())
                AddPlaceholderChild();
        }

        private bool ShouldMaterializeChild(TreeNodeSource childSource)
        {
            if (_isFilterActive?.Invoke() != true)
                return true;

            return childSource.IsVisible || childSource.MaterializedNode != null;
        }

        private bool ShouldUsePlaceholder()
        {
            return _source != null && _source.HasChildren && !IsNodeExpanded && (_isFilterActive?.Invoke() != true);
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
    }
}
