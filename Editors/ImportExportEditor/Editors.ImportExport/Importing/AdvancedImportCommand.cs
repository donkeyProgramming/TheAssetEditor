using Editors.ImportExport.ContextMenu;
using Shared.Core.PackFiles.Models;
using Shared.Ui.BaseDialogs.PackFileTree;
using Shared.Ui.BaseDialogs.PackFileTree.ContextMenu.Commands;

namespace Editors.ImportExport.Importing
{
    public class AdvancedImportCommand(IImportFileContextMenuHelper importFileContextMenuHelper) : IContextMenuCommand
    {
        public string GetDisplayName(TreeNode node, PackFile? packFile) => "Advanced Import";
        public bool ShouldAdd(TreeNode node, PackFile? packFile) => node.NodeType == NodeType.Directory && !node.FileOwner.IsCaPackFile;
        public bool IsEnabled(TreeNode node, PackFile? packFile) => true;

        public void Execute(TreeNode selectedNode, PackFile? packFile) => importFileContextMenuHelper.ShowDialog(selectedNode);
    }
}
