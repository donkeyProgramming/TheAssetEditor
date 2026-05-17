using Shared.Ui.BaseDialogs.PackFileTree;

namespace Shared.UiTest.BaseDialogs.PackFileTree.Utility
{
    /// <summary>
    /// Test utility helper for PackFileBrowserViewModel testing.
    /// Provides helper methods for navigating the tree structure created by the view model.
    /// </summary>
    public static class PackFileBrowserViewModelTestHelper
    {
        /// <summary>
        /// Finds a node by its path within the eagerly built tree hierarchy.
        /// </summary>
        /// <param name="parent">The parent node to search from</param>
        /// <param name="path">The path to search for (e.g., "folder1/folder2/file")</param>
        /// <returns>The found node or null if not found.</returns>
        public static TreeNode? GetFromPath(TreeNode parent, string path)
        {
            return TreeNodePathHelper.FindInTree(parent, path);
        }
    }
}
