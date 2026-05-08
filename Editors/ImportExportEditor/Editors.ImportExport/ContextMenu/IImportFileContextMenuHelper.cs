using Shared.Core.PackFiles.Models;
using TreeNode = Shared.Ui.BaseDialogs.PackFileTree.TreeNode;

namespace Editors.ImportExport.ContextMenu
{
    public interface IImportFileContextMenuHelper
    {
        bool CanImportFile(PackFile file);
        void ShowDialog(TreeNode node);
    }
}