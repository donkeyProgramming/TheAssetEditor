using Editors.ImportExport.ContextMenu;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Ui.BaseDialogs.PackFileTree;
using Shared.Ui.BaseDialogs.PackFileTree.ContextMenu.Commands;

namespace Editors.ImportExport.Importing
{
    public class AdvancedImportCommand(IPackFileService packFileService, IImportFileContextMenuHelper importFileContextMenuHelper) : IContextMenuCommand
    {
        public string GetDisplayName(TreeNode node) => "Advanced Import";
        public bool ShouldAdd(TreeNode node)
        {
            var container = TreeNodeHelper.GetPackFileContainer(node);
            return node.NodeType == NodeType.Directory && container is { IsCaPackFile: false };
        }

        public bool IsEnabled(TreeNode node) => true;

        public void Execute(TreeNode selectedNode)
        {
            var container = TreeNodeHelper.GetPackFileContainer(selectedNode);
            if (container == null)
                return;

            importFileContextMenuHelper.ShowDialog(container, selectedNode);
        }
    }
}
