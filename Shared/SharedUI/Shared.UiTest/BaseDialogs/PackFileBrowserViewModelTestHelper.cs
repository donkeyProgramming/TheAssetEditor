using Shared.Ui.BaseDialogs.PackFileTree;

namespace Shared.UiTest.BaseDialogs
{
    /// <summary>
    /// Test utility helper for PackFileBrowserViewModel testing.
    /// Provides helper methods for navigating the tree structure created by the view model.
    /// </summary>
    public static class PackFileBrowserViewModelTestHelper
    {
        /// <summary>
        /// Finds a node by its path within the tree hierarchy.
        /// </summary>
        /// <param name="parent">The parent node to search from</param>
        /// <param name="path">The path to search for (e.g., "folder1/folder2/file")</param>
        /// <returns>The found node or null if not found. For files, requires materialization. For directories, only returns if materialized.</returns>
        public static TreeNode? GetFromPath(TreeNode parent, string path)
        {
            if (path.Length == 0)
                return parent;

            // First determine what the target is by searching through backing children
            var target = TreeNodePathHelper.FindInBackingChildren(parent, path);
            if (target == null)
                return null;

            if (target.NodeType == NodeType.File)
                return TreeNodePathHelper.GetFromPathViaMaterialization(parent, path);

            // Directory: only return if already materialized (visible in the WPF tree)
            return TreeNodePathHelper.FindInMaterializedChildren(parent, path);
        }
    }
}
