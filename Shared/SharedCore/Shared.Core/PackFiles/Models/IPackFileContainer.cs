namespace Shared.Core.PackFiles.Models
{
    public interface IPackFileContainer
    {
        string Name { get; }
        bool IsCaPackFile { get; set; }
        string SystemFilePath { get; }
        Dictionary<string, PackFile> FileList { get; }
        HashSet<string> SourcePackFilePaths { get; }
        int GetFileCount();
        PackFile? FindFile(string path);
        bool ContainsFile(string path);
        string? GetFullPath(PackFile file);
        void AddOrUpdateFile(string path, PackFile file);
        List<(string FileName, PackFile Pack)> FindAllWithExtention(string extention);
    }
}
