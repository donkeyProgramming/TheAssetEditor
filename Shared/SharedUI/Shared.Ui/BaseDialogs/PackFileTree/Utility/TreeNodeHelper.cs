using System;
using System.Collections.Generic;
using System.Linq;
using Shared.Core.PackFiles.Models;
using Shared.Core.PackFiles.Utility;

namespace Shared.Ui.BaseDialogs.PackFileTree.Utility
{
    public static class TreeNodeHelper
    {
        public static PackFile? GetPackFile(TreeNode? node)
        {
            if (node == null || node.NodeType != NodeType.File)
                return null;

            var container = GetPackFileContainer(node);
            return container?.FindFile(node.GetFullPath());
        }

        public static IPackFileContainer? GetPackFileContainer(TreeNode? node)
        {
            var root = GetRootNode(node);
            return (root as RootTreeNode)?.Owner;
        }

        public static RootTreeNode? GetRootNode(TreeNode? node)
        {
            var current = node;
            while (current?.Parent != null)
                current = current.Parent;

            return current as RootTreeNode;
        }

        /// <summary>
        /// Recursively searches for a node in the tree by path.
        /// </summary>
        /// <param name="parent">The parent node to search from</param>
        /// <param name="path">The path to search for (e.g., "folder1/folder2/file")</param>
        /// <returns>The found node or null if not found</returns>
        public static TreeNode? FindInTree(TreeNode parent, string path)
        {
            if (path.Length == 0)
                return parent;

            var separatorIndex = path.IndexOf(System.IO.Path.DirectorySeparatorChar);
            var nodeName = separatorIndex == -1 ? path : path.Substring(0, separatorIndex);
            var remainingPath = separatorIndex == -1 ? string.Empty : path.Substring(separatorIndex + 1);

            var child = parent.Children.FirstOrDefault(x => x.Name == nodeName);
            return child == null ? null : FindInTree(child, remainingPath);
        }

        internal static TreeNode FindNode(PackFileBrowserViewModel viewModel, IPackFileContainer container, string fullPathName)
        {
            var root = viewModel.Files.First(x=>(x as RootTreeNode)!.Owner == container);

            var normalizedPath = PathNormalization.NormalizeFileName(fullPathName);
            var splits = normalizedPath.Split('\\');

            TreeNode? currentNode = root;
            for (var i = 0; i < splits.Length; i++)
            {
                var nextNode = currentNode.Children.Where(x => x.Name == splits[i]).FirstOrDefault();
                if (nextNode == null)
                    throw new Exception("Could not find node for path: " + fullPathName);


                currentNode = nextNode;
            }

            return currentNode;
        }
    }
}
