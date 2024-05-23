using SharedCore.PackFiles.Models;

namespace SharedCore.PackFiles
{
    public interface IPackFileUiProvider
    {
        bool DisplaySaveDialog(PackFileService pfs, List<string> extensions, out PackFile? selectedFile, out string? filePath);
    }
}
