using Shared.Ui.BaseDialogs.PackFileTree.ContextMenu.Commands;
using Shared.Ui.BaseDialogs.PackFileTree;
using Editors.ImportExport.ContextMenu;
using Shared.Core.PackFiles.Models;
using Shared.Ui.BaseDialogs.PackFileTree.Utility;

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

        private TreeNode _node = null!;

        public void Configure(TreeNode node)
        {
            _node = node;
        }

        public void Execute()
        {
            var packFile = TreeNodeHelper.GetPackFile(_node);
            if (packFile == null)
                return;

            exportFileContextMenuHelper.ShowDialog(packFile);
        }
    }
}
