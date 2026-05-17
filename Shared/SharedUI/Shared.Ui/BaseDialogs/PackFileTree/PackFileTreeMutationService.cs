using System;
using System.Collections.Generic;
using System.Linq;
using Shared.Core.PackFiles.Models;

namespace Shared.Ui.BaseDialogs.PackFileTree
{
    public class PackFileTreeMutationService
    {
        private static readonly Comparison<TreeNode> ChildComparison = (left, right) =>
        {
            var nodeTypeComparison = left.NodeType.CompareTo(right.NodeType);
            if (nodeTypeComparison != 0)
                return nodeTypeComparison;

            return StringComparer.CurrentCultureIgnoreCase.Compare(left.Name, right.Name);
        };

        public TreeNode CreateDirectoryChild(TreeNode parent, string name)
        {
            var newNode = new TreeNode(name, NodeType.Directory, parent.FileOwner, parent);
            InsertChildSorted(parent, newNode);
            return newNode;
        }

        public void InsertChildSorted(TreeNode parent, TreeNode child)
        {
            parent.AddChild(child);
            SortChildren(parent);
        }

        public void RemoveExistingFileNode(TreeNode parent, string fileName, PackFile packFile)
        {
            var existingFile = parent.Children.FirstOrDefault(node =>
                node.NodeType == NodeType.File &&
                (node.Item == packFile || node.Name == fileName));

            if (existingFile == null)
                return;

            RemoveNode(existingFile);
        }

        public void RemoveNode(TreeNode node)
        {
            var parent = node.Parent;
            parent?.RemoveChild(node);
            node.RemoveSelf();
        }

        private static void SortChildren(TreeNode parent)
        {
            var sortedChildren = parent.Children
                .OrderBy(child => child, Comparer<TreeNode>.Create(ChildComparison))
                .ToList();

            parent.Children.Clear();
            foreach (var child in sortedChildren)
                parent.Children.Add(child);
        }
    }
}