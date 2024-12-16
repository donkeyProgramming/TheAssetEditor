using System;
using System.Collections.Generic;
using System.Windows;
using CommonControls.BaseDialogs;
using CommonControls.BaseDialogs.ErrorListDialog;
using Shared.Core.ErrorHandling;
using Shared.Core.ErrorHandling.Exceptions;
using Shared.Core.PackFiles;
using Shared.Core.Services;
using Shared.Ui.BaseDialogs.PackFileTree;
using Shared.Ui.BaseDialogs.StandardDialog.PackFile;
using Shared.Ui.Common.Exceptions;

namespace Shared.Ui.BaseDialogs.StandardDialog
{
    public class StandardDialogs : IStandardDialogs
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
            browser.ViewModel.Filter.SetExtensions(extensions);

            if (browser.ShowDialog() == true)
                return new SaveDialogResult(true, browser.SelectedFile, browser.FilePath);

            return new SaveDialogResult(false, null, null);
        }

        public BrowseDialogResultFile DisplayBrowseDialog(List<string> extensions)
        {
            using var browser = new PackFileBrowserWindow(_packFileBrowserBuilder, extensions, showCaFiles: true, showFoldersOnly: false);

            var saveResult = browser.ShowDialog();
            var output = new BrowseDialogResultFile(saveResult, browser.SelectedFile);
            return output;
        }

        public BrowseDialogResultFolder DisplayBrowseFolderDialog()
        {
            using var browser = new PackFileBrowserWindow(_packFileBrowserBuilder, null, showCaFiles: false, showFoldersOnly: true);

            var saveResult = browser.ShowDialog();
            var output = new BrowseDialogResultFolder(saveResult, browser.SelectedFolder);
            return output;
        }

        public void ShowExceptionWindow(Exception e, string userInfo = "")
        {
            var extendedException = _exceptionService.Create(e);
            var errorWindow = new CustomExceptionWindow(extendedException);
            if (Application.Current.MainWindow != null)
            {
                if (errorWindow != Application.Current.MainWindow)
                {
                    errorWindow.Owner = Application.Current.MainWindow;
                }
            }
            errorWindow.ShowDialog();
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
            var result = MessageBox.Show(message, title, MessageBoxButton.YesNo);
            if (result == MessageBoxResult.Yes)
                return ShowMessageBoxResult.OK;
            return ShowMessageBoxResult.Cancel;
        }


        // Show Tool?
    }
}
