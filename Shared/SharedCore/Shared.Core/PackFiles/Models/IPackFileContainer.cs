namespace Shared.Core.PackFiles.Models
{
    public interface IPackFileContainer
    {
        string Name { get; }
        bool IsCaPackFile { get; set; }
        string? SystemFilePath { get; }


        int GetFileCount();
        PackFile? FindFile(string path);
        bool ContainsFile(string path);
        string? GetFullPath(PackFile file);
  
        Dictionary<string, PackFile> GetAllFiles();
    
   
        List<(string Path, PackFile File)> SearchFiles(string? textFilter, IReadOnlyList<string>? extensions);
    }
}
