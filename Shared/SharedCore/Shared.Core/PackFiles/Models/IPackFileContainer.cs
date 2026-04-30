using System.Collections.Generic;

namespace Shared.Core.PackFiles.Models
{
    public readonly struct DirectoryContent
    {
        public IReadOnlyList<string> SubFolders { get; init; }
        public IReadOnlyList<(string FileName, PackFile File)> Files { get; init; }

        public static DirectoryContent Empty => new()
        {
            SubFolders = [],
            Files = []
        };
    }

    public interface IPackFileContainer
    {
        string Name { get; }
        bool IsCaPackFile { get; set; }
        string SystemFilePath { get; }


        int GetFileCount();
        PackFile? FindFile(string path);
        bool ContainsFile(string path);
        string? GetFullPath(PackFile file);
        void AddOrUpdateFile(string path, PackFile file);
        List<(string FileName, PackFile Pack)> FindAllWithExtention(string extention);
        Dictionary<string, PackFile> GetAllFiles();
        DirectoryContent GetDirectoryContent(string directoryPath);
    }
}
