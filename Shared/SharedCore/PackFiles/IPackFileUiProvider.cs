using Shared.Core.PackFiles.Models;

namespace Shared.Core.PackFiles
{
    public interface IPackFileUiProvider
    {
        bool DisplaySaveDialog(PackFileService pfs, List<string> extensions, out PackFile? selectedFile, out string? filePath);
    }
}
