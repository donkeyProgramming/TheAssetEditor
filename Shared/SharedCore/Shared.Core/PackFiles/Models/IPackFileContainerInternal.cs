using Shared.Core.Settings;

namespace Shared.Core.PackFiles.Models
{
    internal interface IPackFileContainerInternal : IPackFileContainer
    {
        void AddOrUpdateFile(string path, PackFile file);
        List<PackFile> AddFiles(List<NewPackFileEntry> newFiles);
        PackFile? DeleteFile(PackFile file);
        void DeleteFolder(string folder);
        void MoveFile(PackFile file, string newFolderPath);
        string RenameDirectory(string currentNodeName, string newName);
        void RenameFile(PackFile file, string newName);
        void SaveFileData(PackFile file, byte[] data);
        // createBackup keeps a copy of the file being replaced: a .pack edited directly keeps it as
        // <name>.pack1 on its first save after opening or reloading; a project keeps numbered copies in Backup\
        void SaveToDisk(string path, bool createBackup, GameInformation gameInformation);

        List<(string FileName, PackFile Pack)> FindAllWithExtention(string extention);
        List<(string Path, PackFile File)> GetDirectoryContent(string directoryPath);

        // Like GetFullPath, but without the fallback to matching by file name
        string? GetExactFullPath(PackFile file);

    }
}
