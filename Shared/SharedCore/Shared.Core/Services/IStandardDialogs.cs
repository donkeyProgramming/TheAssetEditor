using Shared.Core.ErrorHandling;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;

namespace Shared.Core.Services
{
    public interface IStandardDialogs
    {
        SaveDialogResult DisplaySaveDialog(IPackFileService pfs, List<string> extensions);
        BrowseDialogResultFile DisplayBrowseDialog(List<string> extensions);
        BrowseDialogResultFolder DisplayBrowseFolderDialog();
        SystemOpenFileDialogResult ShowSystemOpenFileDialog(bool multiselect = false, string filter = "All files|*.*");
        SystemSaveFileDialogResult ShowSystemSaveFileDialog(string initialFileName, string filter, string defaultExt);
        SystemBrowseFolderDialogResult ShowSystemFolderBrowserDialog();
        string ShowFolderNameDialog(IEnumerable<string> existingNames, string currentValue = "");

        void ShowExceptionWindow(Exception e, string userInfo = "");
        void ShowErrorViewDialog(string title, ErrorList errorItems, bool modal = true);

        TextInputDialogResult ShowTextInputDialog(string title, string initialText = "");
        void ShowDialogBox(string message, string title = "Error");
        ShowMessageBoxResult ShowYesNoBox(string message, string title);
        IWaitCursor ShowWaitCursor();
    }

    public record SaveDialogResult(bool Result, PackFile? SelectedPackFile, string? SelectedFilePath);
    public record BrowseDialogResultFile(bool Result, PackFile File);
    public record BrowseDialogResultFolder(bool Result, string Folder);
    public record SystemOpenFileDialogResult(bool Result, IReadOnlyList<string> FilePaths);
    public record SystemSaveFileDialogResult(bool Result, string? FilePath);
    public record SystemBrowseFolderDialogResult(bool Result, string? FolderPath);
    public record TextInputDialogResult(bool Result, string Text);

    public enum ShowMessageBoxResult
    {
        OK,
        Cancel,
    }

    public interface IWaitCursor : IDisposable{}
}
