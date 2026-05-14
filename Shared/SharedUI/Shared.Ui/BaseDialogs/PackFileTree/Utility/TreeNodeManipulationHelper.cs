using System;
using System.Linq;
using Shared.Core.PackFiles.Models;

namespace Shared.Ui.BaseDialogs.PackFileTree
{
    /// <summary>
    /// Helper class for tree node manipulation operations including sorting and removal.
    /// </summary>
    public static class TreeNodeManipulationHelper
    {
        /// <summary>
        /// Comparison function for sorting tree nodes by type and then by name.
        /// </summary>
        public static readonly Comparison<TreeNode> TreeNodeComparison = (left, right) =>
        {
            var nodeTypeComparison = left.NodeType.CompareTo(right.NodeType);
            if (nodeTypeComparison != 0)
                return nodeTypeComparison;

            return StringComparer.CurrentCultureIgnoreCase.Compare(left.Name, right.Name);
        };

        /// <summary>
        /// Inserts a child node into the parent's children collection in sorted order.
        /// </summary>
        /// <param name="parent">The parent node</param>
        /// <param name="child">The child node to insert</param>
        public static void InsertChildSorted(TreeNode parent, TreeNode child)
        {
            parent.EnsureChildrenPopulated();
            parent.AddChild(child);
            parent.BackingChildren.Sort(TreeNodeComparison);
        }

        /// <summary>
        /// Removes an existing file node from the parent's children collection.
        /// </summary>
        /// <param name="parent">The parent node</param>
        /// <param name="fileName">The name of the file to remove</param>
        /// <param name="packFile">The pack file object to match</param>
        public static void RemoveExistingFileNode(TreeNode parent, string fileName, PackFile packFile)
        {
            parent.EnsureChildrenPopulated();
            var existingFile = parent.BackingChildren.FirstOrDefault(node => 
                node.NodeType == NodeType.File && 
                (node.Item == packFile || node.Name == fileName));

            if (existingFile == null)
                return;

            parent.RemoveChild(existingFile);
            parent.Children.Remove(existingFile);
            existingFile.RemoveSelf();
        }
    }
}
