using Shared.Core.PackFiles.Models;

namespace Shared.Core.PackFiles
{
    public interface IPackFileUiProvider
    {
        SaveDialogResult DisplaySaveDialog(IPackFileService pfs, List<string> extensions);
    }

    public record SaveDialogResult(bool Result, PackFile? SelectedPackFile, string? SelectedFilePath);

}
