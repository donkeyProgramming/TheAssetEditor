using Shared.Core.PackFiles.Models;
using Shared.Core.Services;

namespace Shared.Core.PackFiles
{
    public interface IPackFileService
    {
        bool EnableFileLookUpEvents { get; }

        void AddFilesToPack(PackFileContainer container, List<PackFileService.NewFileEntry> newFiles);
        void CopyFileFromOtherPackFile(PackFileContainer source, string path, PackFileContainer target);
        PackFileContainer CreateNewPackFileContainer(string name, PackFileCAType type, bool setEditablePack = false);
        void DeleteFile(PackFileContainer pf, PackFile file);
        void DeleteFolder(PackFileContainer pf, string folder);
        PackFile? FindFile(string path, PackFileContainer? container = null);
        List<PackFileContainer> GetAllPackfileContainers();
        PackFileContainer? GetEditablePack();
        string GetFullPath(PackFile file, PackFileContainer? container = null);
        PackFileContainer? GetPackFileContainer(PackFile file);
        bool HasEditablePackFile();
        PackFileContainer? Load(string packFileSystemPath, bool setToMainPackIfFirst = false, bool allowLoadWithoutCaPackFiles = false);
        bool LoadAllCaFiles(GameTypeEnum gameEnum);
        PackFileContainer? LoadFolderContainer(string packFileSystemPath);
        void MoveFile(PackFileContainer pf, PackFile file, string newFolderPath);
        void RenameDirectory(PackFileContainer pf, string currentNodeName, string newName);
        void RenameFile(PackFileContainer pf, PackFile file, string newName);
        void Save(PackFileContainer pf, string path, bool createBackup);
        void SaveFile(PackFile file, byte[] data);
        void SetEditablePack(PackFileContainer? pf);
        void UnloadPackContainer(PackFileContainer pf);
    }
}