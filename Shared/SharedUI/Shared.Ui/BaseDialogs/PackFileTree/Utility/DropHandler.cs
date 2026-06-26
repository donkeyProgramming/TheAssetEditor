using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;

namespace Shared.Ui.BaseDialogs.PackFileTree.Utility
{
    public static class DropHandler
    {
        public static bool AllowDrop(TreeNode node, TreeNode? targetNode, IPackFileService packFileService)
        {
            if (targetNode == null)
                return false;

            if (node.NodeType != NodeType.File)
                return false;

            var sourceContainer = FindFileOwner(node);
            var targetContainer = FindFileOwner(targetNode);
            if (sourceContainer == null || sourceContainer != targetContainer)
                return false;

            if (sourceContainer.IsReadOnly)
                return false;

            if (targetNode.NodeType == NodeType.File)
                return false;

            if (FindPackFile(node, packFileService) == null)
                return false;

            return true;
        }

        public static bool Drop(TreeNode node, TreeNode? targetNode, IPackFileService packFileService)
        {
            if (targetNode == null)
                return false;

            var container = FindFileOwner(node);
            if (container == null)
                return false;

            var draggedFile = FindPackFile(node, packFileService);
            if (draggedFile == null)
                return false;

            var dropPath = targetNode.GetFullPath();

            var newFullPath = string.IsNullOrWhiteSpace(dropPath)
                ? draggedFile.Name
                : dropPath + "\\" + draggedFile.Name;
            if (newFullPath == packFileService.GetFullPath(draggedFile, container))
                return false;

            packFileService.MoveFile(container, draggedFile, dropPath);

            return true;
        }

        private static IPackFileContainer? FindFileOwner(TreeNode? node)
        {
            if (node == null)
                return null;

            var root = Utility.TreeNodeHelper.GetRootNode(node);
            return root.Owner;
        }

        private static PackFile? FindPackFile(TreeNode? node, IPackFileService packFileService)
        {
            if (node == null || node.NodeType != NodeType.File)
                return null;

            var container = FindFileOwner(node);
            if (container == null)
                return null;

            return packFileService.FindFile(node.GetFullPath(), container);
        }
    }
}
