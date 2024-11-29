using System.Collections.Generic;
using CommonControls.PackFileBrowser;
using Shared.Core.PackFiles;

namespace Shared.Ui.BaseDialogs.PackFileBrowser
{
    public class PackFileUiProvider : IPackFileUiProvider
    {
        private readonly IPackFileService _pfs;
        private readonly PackFileTreeViewBuilder _packFileBrowserBuilder;

        public PackFileUiProvider(IPackFileService pfs, PackFileTreeViewBuilder packFileBrowserBuilder)
        {
            _pfs = pfs;
            _packFileBrowserBuilder = packFileBrowserBuilder;
        }

        public SaveDialogResult DisplaySaveDialog(IPackFileService remove, List<string> extensions)
        { 
            using var browser = new SavePackFileWindow(_pfs, _packFileBrowserBuilder);
            browser.ViewModel.Filter.SetExtentions(extensions);

            if (browser.ShowDialog() == true)
                return new SaveDialogResult(true, browser.SelectedFile, browser.FilePath);

            return new SaveDialogResult(false, null, null);
        }

        public BrowseDialogResult DisplayBrowseDialog(List<string> extensions)
        {
            using var browser = new PackFileBrowserWindow(_packFileBrowserBuilder, extensions);

            var saveResult = browser.ShowDialog();
            var output = new BrowseDialogResult(saveResult, browser.SelectedFile);
            return output;
        }

        // ExceptionWindow
        // Messagebox
        // ErrorWindow
        // Tool?
    }
}
