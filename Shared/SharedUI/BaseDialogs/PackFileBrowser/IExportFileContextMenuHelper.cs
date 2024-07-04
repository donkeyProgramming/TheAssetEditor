using Shared.Core.PackFiles.Models;

namespace Shared.Ui.BaseDialogs.PackFileBrowser
{
    public interface IExportFileContextMenuHelper
    {
        bool CanExportFile(PackFile packFile);
        void ShowDialog(PackFile packFile);
    }
}
