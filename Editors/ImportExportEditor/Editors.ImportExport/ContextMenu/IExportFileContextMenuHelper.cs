using Shared.Core.PackFiles.Models;

namespace Editors.ImportExport.ContextMenu
{
    public interface IExportFileContextMenuHelper
    {
        bool CanExportFile(PackFile packFile);
        void ShowDialog(PackFile packFile);
    }
}