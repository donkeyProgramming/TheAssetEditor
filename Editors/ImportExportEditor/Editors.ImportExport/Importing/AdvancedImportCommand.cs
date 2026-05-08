using Editors.ImportExport.ContextMenu;
using Shared.Ui.BaseDialogs.PackFileTree;
using Shared.Ui.BaseDialogs.PackFileTree.ContextMenu.Commands;

namespace Editors.ImportExport.Importing
{
    public class AdvancedImportCommand(IImportFileContextMenuHelper importFileContextMenuHelper) : IContextMenuCommand
    {
        public string GetDisplayName(TreeNode node) => "Advanced Import";
        public bool ShouldAdd(TreeNode node) => node.NodeType == NodeType.Directory && !node.FileOwner.IsCaPackFile;
        public bool IsEnabled(TreeNode node) => true;

        public void Execute(TreeNode selectedNode) => importFileContextMenuHelper.ShowDialog(selectedNode);
    }
}
