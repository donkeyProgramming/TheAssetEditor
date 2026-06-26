namespace Shared.Core.PackFiles.Models
{
    public enum PackFileContainerType
    {
        Database,
        Normal,
        SystemFolder
    }

    public interface IPackFileContainer
    {
        string Name { get; }
        bool IsReadOnly { get; set; }
        bool IsCaPackFile { get; set; }
        string? SystemFilePath { get; }
        PackFileContainerType ContainerType { get; }


        int GetFileCount();
        PackFile? FindFile(string path);
        bool ContainsFile(string path);
        string? GetFullPath(PackFile file);
  
        Dictionary<string, PackFile> GetAllFiles();
        SortedDictionary<string, List<string>> GetAllFilesByFolder();

        List<(string Path, PackFile File)> SearchFiles(string? textFilter, IReadOnlyList<string>? extensions);
    }
}
