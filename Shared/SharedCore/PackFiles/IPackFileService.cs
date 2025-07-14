using Shared.Core.PackFiles.Models;
using Shared.Core.Settings;

namespace Shared.Core.PackFiles
{
    public interface IPackFileService
    {
        bool EnableFileLookUpEvents { get; set; }
        bool EnforceGameFilesMustBeLoaded { get; set; }

        PackFileContainer? AddContainer(PackFileContainer container, bool setToMainPackIfFirst = false);
        void AddFilesToPack(PackFileContainer container, List<NewPackFileEntry> newFiles);
        void CopyFileFromOtherPackFile(PackFileContainer source, string path, PackFileContainer target);
        PackFileContainer CreateNewPackFileContainer(string name, PackFileCAType type, bool setEditablePack = false);
        void DeleteFile(PackFileContainer pf, PackFile file);
        void DeleteFolder(PackFileContainer pf, string folder);
        PackFile? FindFile(string path, PackFileContainer? container = null);
        List<PackFileContainer> GetAllPackfileContainers();
        PackFileContainer? GetEditablePack();
        string GetFullPath(PackFile file, PackFileContainer? container = null);
        PackFileContainer? GetPackFileContainer(PackFile file);
        void MoveFile(PackFileContainer pf, PackFile file, string newFolderPath);
        void RenameDirectory(PackFileContainer pf, string currentNodeName, string newName);
        void RenameFile(PackFileContainer pf, PackFile file, string newName);
        void SaveFile(PackFile file, byte[] data);
        void SavePackContainer(PackFileContainer pf, string path, bool createBackup, GameInformation gameInformation);
        void SetEditablePack(PackFileContainer? pf);
        void UnloadPackContainer(PackFileContainer pf);
    }
}
