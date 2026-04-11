using Shared.Ui.BaseDialogs.PackFileTree.ContextMenu.External;

namespace Shared.Ui.BaseDialogs.PackFileTree.ContextMenu.Commands
{
    public class AdvancedExportCommand(IExportFileContextMenuHelper exportFileContextMenuHelper) : IContextMenuCommand
    {
        public string GetDisplayName(TreeNode node) => "Advanced Export";
        public bool IsEnabled(TreeNode node) => exportFileContextMenuHelper.CanExportFile(node.Item);

        public void Execute(TreeNode _selectedNode) => exportFileContextMenuHelper.ShowDialog(_selectedNode.Item);
    }
}
