using System;
using System.Collections.Generic;
using System.Windows.Forms;
using CommonControls.BaseDialogs;
using CommonControls.BaseDialogs.ErrorListDialog;
using CommonControls.PackFileBrowser;
using Shared.Core.ErrorHandling;
using Shared.Core.ErrorHandling.Exceptions;
using Shared.Core.PackFiles;
using Shared.Ui.BaseDialogs.PackFileBrowser;

namespace Shared.Ui.BaseDialogs.StandardDialog
{




    internal class StandardDialogs : IPackFileUiProvider
    {
        private readonly IPackFileService _pfs;
        private readonly PackFileTreeViewFactory _packFileBrowserBuilder;
        private readonly IExceptionService _exceptionService;

        public StandardDialogs(IPackFileService pfs, PackFileTreeViewFactory packFileBrowserBuilder, IExceptionService exceptionService)
        {
            _pfs = pfs;
            _packFileBrowserBuilder = packFileBrowserBuilder;
            _exceptionService = exceptionService;
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

        public void ShowExceptionWindow(Exception e, string userInfo = "")
        {
            _exceptionService.CreateDialog(e, userInfo);
        }

        public void ShowErrorViewDialog(string title, ErrorList errorItems, bool modal = true)
        {
            ErrorListWindow.ShowDialog(title, errorItems, modal);
        }

        public TextInputDialogResult ShowTextInputDialog(string title, string initialText = "")
        {
            var window = new TextInputWindow(title, initialText);
            var result = window.ShowDialog();

            return new TextInputDialogResult(result!.Value, window.TextValue);
        }

        public void ShowDialogBox(string message, string title)
        {
            MessageBox.Show(message, title);
        }

        public ShowMessageBoxResult ShowYesNoBox(string message, string title)
        {
            var result = MessageBox.Show(message, title, MessageBoxButtons.YesNo);
            if (result == DialogResult.Yes)
                return ShowMessageBoxResult.OK;
            return ShowMessageBoxResult.Cancel;
        }


        // Show Tool?
    }
}
