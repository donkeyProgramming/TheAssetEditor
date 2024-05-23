using System.Collections.Generic;
using SharedCore.PackFiles;
using SharedCore.PackFiles.Models;

namespace CommonControls.PackFileBrowser
{
    public class PackFileUiProvider : IPackFileUiProvider
    {
        public bool DisplaySaveDialog(PackFileService pfs, List<string> extensions, out PackFile? selectedFile, out string? filePath)
        {
            selectedFile = null;
            filePath = null;

            using var browser = new SavePackFileWindow(pfs);
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
