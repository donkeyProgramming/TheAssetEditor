using System.IO;
using System.Windows;
using CommonControls.BaseDialogs;
using CommonControls.BaseDialogs.ErrorListDialog;
using Shared.Core.DependencyInjection;
using Shared.Core.ErrorHandling;
using Shared.Core.ErrorHandling.Exceptions;
using Shared.Core.Events;
using Shared.Core.PackFiles;
using Shared.Core.Services;
using Shared.Ui.BaseDialogs.PackFileTree;
using Shared.Ui.BaseDialogs.StandardDialog.PackFile;
using Shared.Ui.Common.Exceptions;

namespace Shared.Ui.BaseDialogs.StandardDialog
{
    public class StandardDialogs : IStandardDialogs
    {
        private readonly PackFileTreeViewFactory _packFileBrowserBuilder;
        private readonly IExceptionService _exceptionService;
        private readonly IScopeRepository _scopeRepository;
        private readonly IEventHub _eventHub;
        private readonly ScopeToken _scopeToken;
        public StandardDialogs(PackFileTreeViewFactory packFileBrowserBuilder, IExceptionService exceptionService, IScopeRepository scopeRepository, IEventHub eventHub, ScopeToken scopeToken)
        {
            _packFileBrowserBuilder = packFileBrowserBuilder;
            _exceptionService = exceptionService;
            _scopeRepository = scopeRepository;
            _eventHub = eventHub;
            _scopeToken = scopeToken;
        }

        public IWaitCursor ShowWaitCursor() => new AeWaitCursor();

        public SaveDialogResult DisplaySaveDialog(IPackFileService pfs, List<string> extensions)
        {
            using var browser = new SavePackFileWindow(pfs, _packFileBrowserBuilder);
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

        public SystemOpenFileDialogResult ShowSystemOpenFileDialog(bool multiselect = false, string filter = "All files|*.*")
        {
            using var dialog = new System.Windows.Forms.OpenFileDialog
            {
                Multiselect = multiselect,
                Filter = filter
            };

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                return new SystemOpenFileDialogResult(true, dialog.FileNames);

            return new SystemOpenFileDialogResult(false, Array.Empty<string>());
        }

        public SystemSaveFileDialogResult ShowSystemSaveFileDialog(string initialFileName, string filter, string defaultExt)
        {
            using var dialog = new System.Windows.Forms.SaveFileDialog
            {
                FileName = initialFileName,
                Filter = filter,
                DefaultExt = defaultExt
            };

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                return new SystemSaveFileDialogResult(true, dialog.FileName);

            return new SystemSaveFileDialogResult(false, null);
        }

        public SystemBrowseFolderDialogResult ShowSystemFolderBrowserDialog()
        {
            using var dialog = new System.Windows.Forms.FolderBrowserDialog();

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                return new SystemBrowseFolderDialogResult(true, dialog.SelectedPath);

            return new SystemBrowseFolderDialogResult(false, null);
        }

        public string ShowFolderNameDialog(IEnumerable<string> existingNames, string currentValue = "")
        {
            var isInputCorrect = false;
            var dialogTextBoxValue = currentValue;
            var normalizedExistingNames = new HashSet<string>(existingNames.Select(x => x.ToLower().Trim()));

            while (!isInputCorrect)
            {
                isInputCorrect = true;
                var window = new TextInputWindow("Create folder", dialogTextBoxValue, true);

                if (window.ShowDialog() == false)
                    return string.Empty;

                dialogTextBoxValue = window.TextValue;
                var newFolderName = window.TextValue.ToLower().Trim();

                if (string.IsNullOrWhiteSpace(newFolderName))
                {
                    System.Windows.MessageBox.Show("Folder name can not be empty! Please Try Again.");
                    isInputCorrect = false;
                }

                if (isInputCorrect && normalizedExistingNames.Contains(newFolderName))
                {
                    System.Windows.MessageBox.Show($"Folder with name '{newFolderName}' already exists in this folder!\nPlease Try Again.");
                    isInputCorrect = false;
                }

                if (isInputCorrect)
                {
                    var listOfInvalidChars = Path.GetInvalidFileNameChars();
                    foreach (var c in newFolderName)
                    {
                        if (listOfInvalidChars.Contains(c))
                        {
                            System.Windows.MessageBox.Show($"Folder name contains invalid character: {c}. \nPlease Try Again.");
                            isInputCorrect = false;
                            break;
                        }
                    }
                }

                if (isInputCorrect)
                    return newFolderName;
            }

            return string.Empty;
        }

        public void ShowExceptionWindow(Exception e, string userInfo = "")
        {
            var extendedException = _exceptionService.Create(e);
            var errorWindow = new CustomExceptionWindow(extendedException, this, _eventHub, _scopeToken, _scopeRepository);
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
            var result = System.Windows.MessageBox.Show(message, title, MessageBoxButton.YesNo);
            if (result == MessageBoxResult.Yes)
                return ShowMessageBoxResult.OK;
            return ShowMessageBoxResult.Cancel;
        }


        // Show Tool?
    }
}
