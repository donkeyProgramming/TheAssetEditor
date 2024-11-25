using System.Collections.Generic;
using CommonControls.PackFileBrowser;
using Shared.Core.PackFiles;

namespace Shared.Ui.BaseDialogs.PackFileBrowser
{
    public class PackFileUiProvider : IPackFileUiProvider
    {
        public SaveDialogResult DisplaySaveDialog(IPackFileService pfs, List<string> extensions)
        {
            using var browser = new SavePackFileWindow(pfs);
            browser.ViewModel.Filter.SetExtentions(extensions);

            if (browser.ShowDialog() == true)
                return new SaveDialogResult(true, browser.SelectedFile, browser.FilePath);

            return new SaveDialogResult(false, null, null);
        }
    }
}
