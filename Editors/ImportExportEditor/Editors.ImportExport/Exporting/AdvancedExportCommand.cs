using Shared.Ui.BaseDialogs.PackFileTree.ContextMenu.Commands;
using Shared.Ui.BaseDialogs.PackFileTree;
using Editors.ImportExport.ContextMenu;
using Shared.Core.PackFiles.Models;

namespace Editors.ImportExport.Exporting
{
    public class AdvancedExportCommand(IExportFileContextMenuHelper exportFileContextMenuHelper) : IContextMenuCommand
    {
        public string GetDisplayName(TreeNode node) => "Advanced Export";
        public bool ShouldAdd(TreeNode node) => node.NodeType == NodeType.File && TreeNodeHelper.GetPackFile(node) != null;
        public bool IsEnabled(TreeNode node)
        {
            var packFile = TreeNodeHelper.GetPackFile(node);
            return packFile != null && exportFileContextMenuHelper.CanExportFile(packFile);
        }

        public void Execute(TreeNode selectedNode)
        {
            var packFile = TreeNodeHelper.GetPackFile(selectedNode);
            if (packFile == null)
                return;

            exportFileContextMenuHelper.ShowDialog(packFile);
        }
    }
}
