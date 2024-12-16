namespace Shared.Ui.BaseDialogs.PackFileTree.ContextMenu.Commands
{
    public class CollapseNodeCommand() : IContextMenuCommand
    {
        public string GetDisplayName(TreeNode node) => "Collapse all";
        public bool IsEnabled(TreeNode node) => true;

        public void Execute(TreeNode _selectedNode) => CollapsAllRecursive(_selectedNode);

        void CollapsAllRecursive(TreeNode node)
        {
            node.IsNodeExpanded = false;
            foreach (var child in node.Children)
                CollapsAllRecursive(child);
        }

    }
}
