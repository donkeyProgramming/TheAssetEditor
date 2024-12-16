namespace Shared.Ui.BaseDialogs.PackFileTree.ContextMenu.Commands
{
    public class ExpandNodeCommand() : IContextMenuCommand
    {
        public string GetDisplayName(TreeNode node) => "Expand all";
        public bool IsEnabled(TreeNode node) => true;

        public void Execute(TreeNode _selectedNode) => ExpandAllRecursive(_selectedNode);

        void ExpandAllRecursive(TreeNode node)
        {
            node.IsNodeExpanded = true;
            foreach (var child in node.Children)
                ExpandAllRecursive(child);
        }

    }
}
