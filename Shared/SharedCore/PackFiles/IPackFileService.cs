using Shared.Core.PackFiles.Models;
using Shared.Core.Services;
using static Shared.Core.PackFiles.PackFileService;

namespace Shared.Core.PackFiles
{
    public interface IPackFileService
    {
        PackFileDataBase Database { get; }
        bool TriggerFileUpdates { get; set; }


        event PackFileService.FileLookUpHander? FileLookUpEvent;

        // Create
        void AddFilesToPack(PackFileContainer container, List<NewFileEntry> newFiles);
        PackFileContainer CreateNewPackFileContainer(string name, PackFileCAType type, bool setEditablePack = false);

        // Load
        bool LoadAllCaFiles(GameTypeEnum gameEnum);
        PackFileContainer? Load(string packFileSystemPath, bool setToMainPackIfFirst = false, bool allowLoadWithoutCaPackFiles = false);
        PackFileContainer? LoadFolderContainer(string packFileSystemPath);

        // Save 
        void Save(PackFileContainer pf, string path, bool createBackup);
        void SaveFile(PackFile file, byte[] data);

        // Edit
        void RenameDirectory(PackFileContainer pf, string currentNodeName, string newName);
        void RenameFile(PackFileContainer pf, PackFile file, string newName);

        // Find - Return FileLoopupResult(Packfile file, string fullPath, PackFileContainer owner)
        List<string> DeepSearch(string searchStr, bool caseSensetive);
        List<string> SearchForFile(string partOfFileName); 
        List<PackFile> FindAllFilesInDirectory(string dir, bool includeSubFolders = true);
        List<PackFile> FindAllWithExtention(string extention, PackFileContainer packFileContainer = null);
        List<(string FileName, PackFile Pack)> FindAllWithExtentionIncludePaths(string extention, PackFileContainer packFileContainer = null);
        PackFile? FindFile(string path, PackFileContainer? container = null);
        string GetFullPath(PackFile file, PackFileContainer? container = null);
        PackFileContainer? GetPackFileContainer(PackFile file);


        List<PackFileContainer> GetAllPackfileContainers();
        PackFileContainer? GetEditablePack();
        List<PackFile> GetAllAnimPacks();


        // Delete
        void DeleteFile(PackFileContainer pf, PackFile file);
        void DeleteFolder(PackFileContainer pf, string folder);

      



        // Misc
        bool HasEditablePackFile();
        void MoveFile(PackFileContainer pf, PackFile file, string newFolderPath);
        void CopyFileFromOtherPackFile(PackFileContainer source, string path, PackFileContainer target);
        void SetEditablePack(PackFileContainer pf);
        void UnloadPackContainer(PackFileContainer pf);
    }
}
