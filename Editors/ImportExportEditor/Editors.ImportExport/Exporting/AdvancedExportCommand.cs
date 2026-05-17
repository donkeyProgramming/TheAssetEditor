using Shared.Ui.BaseDialogs.PackFileTree.ContextMenu.Commands;
using Shared.Ui.BaseDialogs.PackFileTree;
using Editors.ImportExport.ContextMenu;
using Shared.Core.PackFiles.Models;

namespace Editors.ImportExport.Exporting
{
    public class AdvancedExportCommand(IExportFileContextMenuHelper exportFileContextMenuHelper) : IContextMenuCommand
    {
        public string GetDisplayName(TreeNode node, PackFile? packFile) => "Advanced Export";
        public bool ShouldAdd(TreeNode node, PackFile? packFile) => node.NodeType == NodeType.File && packFile != null;
        public bool IsEnabled(TreeNode node, PackFile? packFile) => packFile != null && exportFileContextMenuHelper.CanExportFile(packFile);

        public void Execute(TreeNode selectedNode, PackFile? packFile)
        {
            if (packFile == null)
                return;

            exportFileContextMenuHelper.ShowDialog(packFile);
        }
    }
}
