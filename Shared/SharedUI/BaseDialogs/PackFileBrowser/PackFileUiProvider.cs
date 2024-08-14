using System.Collections.Generic;
using CommonControls.PackFileBrowser;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;

namespace Shared.Ui.BaseDialogs.PackFileBrowser
{
    public class PackFileUiProvider : IPackFileUiProvider
    {
        public bool DisplaySaveDialog(PackFileService pfs, List<string> extensions, out PackFile selectedFile, out string filePath)
        {
            selectedFile = null;
            filePath = null;

            using var browser = new SavePackFileWindow(pfs as PackFileService);
            browser.ViewModel.Filter.SetExtentions(extensions);

            if (browser.ShowDialog() == true)
            {
                selectedFile = browser.SelectedFile;
                filePath = browser.FilePath;
                return true;
            }

            return false;
        }
    }
}
