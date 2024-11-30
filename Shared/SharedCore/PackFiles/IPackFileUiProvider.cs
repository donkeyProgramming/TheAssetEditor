using Shared.Core.ErrorHandling;
using Shared.Core.PackFiles.Models;

namespace Shared.Core.PackFiles
{

   /* public interface IDialogProvider
    {
        IPackFileUiProvider Dialogs { get; }
    }


    public class DialogProvider : IDialogProvider
    {
        private readonly IPackFileUiProvider _packFileUiProvider;

        public DialogProvider(IPackFileUiProvider packFileUiProvider)
        {
            _packFileUiProvider = packFileUiProvider;
        }

        public IPackFileUiProvider Dialogs { get => _packFileUiProvider; }
    }*/



    public interface IPackFileUiProvider
    {
        SaveDialogResult DisplaySaveDialog(IPackFileService pfs, List<string> extensions);
        BrowseDialogResult DisplayBrowseDialog(List<string> extensions);

        void ShowExceptionWindow(Exception e, string userInfo = "");
        void ShowErrorViewDialog(string title, ErrorList errorItems, bool modal = true);

        TextInputDialogResult ShowTextInputDialog(string title, string initialText = "");
        void ShowDialogBox(string message, string title);
        ShowMessageBoxResult ShowYesNoBox(string message, string title);
    }

    public record SaveDialogResult(bool Result, PackFile? SelectedPackFile, string? SelectedFilePath);
    public record BrowseDialogResult(bool Result, PackFile File);
    public record TextInputDialogResult(bool Result, string Text);

    public enum ShowMessageBoxResult
    { 
        OK,
        Cancel,
    }

}
