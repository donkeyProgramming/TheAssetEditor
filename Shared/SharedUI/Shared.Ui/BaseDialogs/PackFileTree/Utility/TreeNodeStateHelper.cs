namespace Shared.Ui.BaseDialogs.PackFileTree
{
    /// <summary>
    /// Helper class for tree node state management operations.
    /// </summary>
    public static class TreeNodeStateHelper
    {
        /// <summary>
        /// Recursively clears the UnsavedChanged flag on a node and all its loaded children.
        /// Only processes children that have already been loaded (materialized).
        /// </summary>
        /// <param name="node">The node to start clearing from</param>
        public static void ClearUnsavedOnLoadedNodes(TreeNode node)
        {
            node.UnsavedChanged = false;
            if (!node.ChildrenLoaded)
                return;

            foreach (var child in node.BackingChildren)
                ClearUnsavedOnLoadedNodes(child);
        }
    }
}
