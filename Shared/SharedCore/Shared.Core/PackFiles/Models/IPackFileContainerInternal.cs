using Shared.Core.Settings;

namespace Shared.Core.PackFiles.Models
{
    internal interface IPackFileContainerInternal : IPackFileContainer
    {
        List<PackFile> AddFiles(List<NewPackFileEntry> newFiles);
        PackFile? DeleteFile(PackFile file);
        void DeleteFolder(string folder);
        PackFile? FindFile(string path);
        string? GetFullPath(PackFile file);
        void MoveFile(PackFile file, string newFolderPath);
        string RenameDirectory(string currentNodeName, string newName);
        void RenameFile(PackFile file, string newName);
        void SaveFileData(PackFile file, byte[] data);
        void SaveToDisk(string path, bool createBackup, GameInformation gameInformation);
    }
}