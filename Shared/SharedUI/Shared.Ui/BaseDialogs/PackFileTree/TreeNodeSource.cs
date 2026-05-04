using System;
using System.Collections.Generic;
using Shared.Core.PackFiles.Models;

namespace Shared.Ui.BaseDialogs.PackFileTree
{
    /// <summary>
    /// Represents the lightweight backing data model for the pack-file tree.
    /// It stores the canonical hierarchy and metadata independent of UI state, and may optionally
    /// reference a materialized <see cref="TreeNode"/> when that branch has been created for display.
    /// Children are loaded lazily from the container on first access of a directory/root node.
    /// </summary>
    internal sealed class TreeNodeSource
    {
        private Func<TreeNodeSource, bool>? _childLoader;
        private bool _childrenLoaded;

        public string Name { get; set; }
        public NodeType NodeType { get; }
        public IPackFileContainer FileOwner { get; }
        public PackFile? Item { get; }
        public TreeNodeSource? Parent { get; private set; }
        public List<TreeNodeSource> Children { get; } = [];
        public bool IsVisible { get; set; } = true;
        public bool UnsavedChanged { get; set; }
        public TreeNode? MaterializedNode { get; set; }

        /// <summary>
        /// Returns true if this node has children (loaded or potentially unloaded).
        /// </summary>
        public bool HasChildren => Children.Count > 0 || (!_childrenLoaded && _childLoader != null && NodeType != NodeType.File);

        /// <summary>
        /// Returns true if children have been loaded from the container.
        /// </summary>
        public bool ChildrenLoaded => _childrenLoaded;

        public TreeNodeSource(string name, NodeType nodeType, IPackFileContainer fileOwner, TreeNodeSource? parent, PackFile? item = null)
        {
            Name = name;
            NodeType = nodeType;
            FileOwner = fileOwner;
            Parent = parent;
            Item = item;

            if (string.IsNullOrWhiteSpace(name))
                throw new Exception($"Packfile name or folder is empty '{GetFullPath()}', this is not allowed! Please report as a bug if it happens outside of packfile loading! If it happens while loading clean up the packfile in RPFM");
        }

        /// <summary>
        /// Sets the child loader delegate used for lazy population of children.
        /// The delegate receives this node and returns true if children were loaded.
        /// </summary>
        public void SetChildLoader(Func<TreeNodeSource, bool> childLoader)
        {
            _childLoader = childLoader;
        }

        /// <summary>
        /// Ensures children are populated. If not yet loaded and a child loader is set, loads them now.
        /// </summary>
        public void EnsureChildrenPopulated()
        {
            if (_childrenLoaded || _childLoader == null || NodeType == NodeType.File)
                return;

            _childrenLoaded = _childLoader(this);
        }

        /// <summary>
        /// Recursively ensures all descendants are populated.
        /// </summary>
        public void EnsureFullyPopulated()
        {
            EnsureChildrenPopulated();
            foreach (var child in Children)
                child.EnsureFullyPopulated();
        }

        /// <summary>
        /// Marks children as already loaded (used when children are added externally).
        /// </summary>
        public void MarkChildrenLoaded()
        {
            _childrenLoaded = true;
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
                current.EnsureChildrenPopulated();
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