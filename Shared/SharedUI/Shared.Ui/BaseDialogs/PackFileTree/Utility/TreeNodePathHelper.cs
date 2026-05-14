using System;
using System.Linq;

namespace Shared.Ui.BaseDialogs.PackFileTree
{
    /// <summary>
    /// Helper class for tree node path navigation and lookup operations.
    /// </summary>
    public static class TreeNodePathHelper
    {
        /// <summary>
        /// Recursively searches for a node in the backing children collection by path.
        /// </summary>
        /// <param name="parent">The parent node to search from</param>
        /// <param name="path">The path to search for (e.g., "folder1/folder2/file")</param>
        /// <returns>The found node or null if not found</returns>
        public static TreeNode? FindInBackingChildren(TreeNode parent, string path)
        {
            if (path.Length == 0)
                return parent;

            parent.EnsureChildrenPopulated();

            var separatorIndex = path.IndexOf(System.IO.Path.DirectorySeparatorChar);
            var nodeName = separatorIndex == -1 ? path : path.Substring(0, separatorIndex);
            var remainingPath = separatorIndex == -1 ? string.Empty : path.Substring(separatorIndex + 1);

            var child = parent.BackingChildren.FirstOrDefault(x => x.Name == nodeName);
            return child == null ? null : FindInBackingChildren(child, remainingPath);
        }

        /// <summary>
        /// Recursively searches for a node in the materialized children collection by path,
        /// materializing children as needed.
        /// </summary>
        /// <param name="parent">The parent node to search from</param>
        /// <param name="path">The path to search for</param>
        /// <returns>The found node or null if not found</returns>
        public static TreeNode? GetFromPathViaMaterialization(TreeNode parent, string path)
        {
            if (path.Length == 0)
                return parent;

            parent.MaterializeChildren();

            var separatorIndex = path.IndexOf(System.IO.Path.DirectorySeparatorChar);
            var nodeName = separatorIndex == -1 ? path : path.Substring(0, separatorIndex);
            var remainingPath = separatorIndex == -1 ? string.Empty : path.Substring(separatorIndex + 1);

            var child = parent.Children.FirstOrDefault(x => x.Name == nodeName);
            return child == null ? null : GetFromPathViaMaterialization(child, remainingPath);
        }

        /// <summary>
        /// Recursively searches for a node in the materialized children collection by path.
        /// Only returns results that are already materialized (visible in the WPF tree).
        /// </summary>
        /// <param name="parent">The parent node to search from</param>
        /// <param name="path">The path to search for</param>
        /// <returns>The found node or null if not found</returns>
        public static TreeNode? FindInMaterializedChildren(TreeNode parent, string path)
        {
            if (path.Length == 0)
                return parent;

            var separatorIndex = path.IndexOf(System.IO.Path.DirectorySeparatorChar);
            var nodeName = separatorIndex == -1 ? path : path.Substring(0, separatorIndex);
            var remainingPath = separatorIndex == -1 ? string.Empty : path.Substring(separatorIndex + 1);

            var child = parent.Children.FirstOrDefault(x => x.Name == nodeName);
            return child == null ? null : FindInMaterializedChildren(child, remainingPath);
        }
    }
}
