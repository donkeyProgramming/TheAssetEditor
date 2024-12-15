using Shared.Ui.BaseDialogs.PackFileTree.ContextMenu.External;

namespace Shared.Ui.BaseDialogs.PackFileTree.ContextMenu.Commands
{
    public class AdvancedImportCommand(IImportFileContextMenuHelper importFileContextMenuHelper) : IContextMenuCommand
    {
        public string GetDisplayName(TreeNode node) => "Advanced Import";
        public bool IsEnabled(TreeNode node) => true;

        public void Execute(TreeNode _selectedNode) => importFileContextMenuHelper.ShowDialog(_selectedNode);
    }
}
