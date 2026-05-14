using Shared.Ui.BaseDialogs.PackFileTree.ContextMenu.Commands;
using Shared.Ui.BaseDialogs.PackFileTree;
using Editors.ImportExport.ContextMenu;

namespace Editors.ImportExport.Exporting
{
    public class AdvancedExportCommand(IExportFileContextMenuHelper exportFileContextMenuHelper) : IContextMenuCommand
    {
        public string GetDisplayName(TreeNode node) => "Advanced Export";
        public bool ShouldAdd(TreeNode node) => node.NodeType == NodeType.File && node.Item != null;
        public bool IsEnabled(TreeNode node) => exportFileContextMenuHelper.CanExportFile(node.Item);

        public void Execute(TreeNode selectedNode) => exportFileContextMenuHelper.ShowDialog(selectedNode.Item);
    }
}
