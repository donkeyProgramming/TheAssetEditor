using Shared.Core.PackFiles.Models;

namespace Shared.Ui.BaseDialogs.PackFileTree.ContextMenu.External
{
    public interface IExportFileContextMenuHelper
    {
        bool CanExportFile(PackFile packFile);
        void ShowDialog(PackFile packFile);
    }
}
