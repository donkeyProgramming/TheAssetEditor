using System.Reflection;
using System.Text;
using System.Windows.Forms;
using Serilog;
using Shared.Core.ErrorHandling;
using Shared.Core.Events;
using Shared.Core.Events.Global;
using Shared.Core.Misc;
using Shared.Core.PackFiles.Models;
using Shared.Core.Services;

namespace Shared.Core.PackFiles
{

    // public class PackFileContainerLoader
    // {
    //     PackFileContainer LoadGameFiles()
    //     { }
    //
    //     PackFileContainer LoadPack()
    //     { 
    //     }
    //
    //     PackFileContainer LoadFolderAsPack()
    //     {
    //     }
    //
    //
    //
    // }




    public class PackFileService : IPackFileService
    {
        private readonly ILogger _logger = Logging.Create<PackFileService>();

        private readonly List<PackFileContainer> _packFiles = [];
        private PackFileContainer? _packSelectedForEdit;

        public bool EnableFileLookUpEvents { get; internal set; } = false;

        private readonly IGlobalEventHub? _globalEventHub;
        private readonly ApplicationSettingsService _settingsService;
        private readonly GameInformationFactory _gameInformationFactory;

        public PackFileService(ApplicationSettingsService settingsService, GameInformationFactory gameInformationFactory, IGlobalEventHub? globalEventHub)
        {
            _globalEventHub = globalEventHub;
            _settingsService = settingsService;
            _gameInformationFactory = gameInformationFactory;
        }

        public List<PackFileContainer> GetAllPackfileContainers() => _packFiles.ToList(); // Return a list of the list to avoid bugs!


        public PackFileContainer? LoadSystemFolderAsPackFileContainer(string packFileSystemPath)
        {
            if (Directory.Exists(packFileSystemPath) == false)
            {
                var location = Assembly.GetEntryAssembly()!.Location;
                var loactionDir = Path.GetDirectoryName(location);
                throw new Exception($"Unable to find folder {packFileSystemPath}. Curret systempath is {loactionDir}");
            }

            var container = new PackFileContainer(packFileSystemPath);
            AddFolderContentToPackFile(container, packFileSystemPath, packFileSystemPath.ToLower() + "\\");
            AddContainer(container);
            return container;
        }

        void AddContainer(PackFileContainer container)
        {
            _packFiles.Add(container);
            _globalEventHub?.PublishGlobalEvent(new PackFileContainerAddedEvent(container));
        }

        void AddFolderContentToPackFile(PackFileContainer container, string folderPath, string rootPath)
        {
            var files = Directory.GetFiles(folderPath);
            foreach (var filePath in files)
            {
                var sanatizedFilePath = filePath.ToLower();
                var relativePath = sanatizedFilePath.Replace(rootPath, "");
                var fileName = Path.GetFileName(sanatizedFilePath);

                container.FileList[relativePath] = PackFile.CreateFromFileSystem(fileName, sanatizedFilePath);
            }

            var folders = Directory.GetDirectories(folderPath);
            foreach (var folder in folders)
                AddFolderContentToPackFile(container, folder, rootPath);
        }

        public PackFileContainer? Load(string packFileSystemPath, bool setToMainPackIfFirst = false, bool allowLoadWithoutCaPackFiles = false)
        {
            try
            {
                var caPacksLoaded = _packFiles.Count(x => x.IsCaPackFile);
                if (caPacksLoaded == 0 && allowLoadWithoutCaPackFiles != true)
                {
                    MessageBox.Show("You are trying to load a pack file before loading CA packfile. Most editors EXPECT the CA packfiles to be loaded and will cause issues if they are not.\nFile not loaded!", "Error");

                    if (System.Diagnostics.Debugger.IsAttached == false)
                        return null;
                }

                if (!File.Exists(packFileSystemPath))
                {
                    _logger.Here().Error($"Trying to load file {packFileSystemPath}, which can not be located.", "Error");
                    System.Windows.MessageBox.Show($"Unable to locate pack file \"{packFileSystemPath}\"");
                    return null;
                }

                foreach (var packFile in _packFiles)
                {
                    if (packFile.SystemFilePath == packFileSystemPath)
                    {
                        MessageBox.Show($"Pack file \"{packFileSystemPath}\" is already loaded.", "Error");
                        return null;
                    }
                }

                using var fileStream = File.OpenRead(packFileSystemPath);
                using var reader = new BinaryReader(fileStream, Encoding.ASCII);

                var container = PackFileSerializer.Load(packFileSystemPath, reader, _settingsService.CurrentSettings.LoadWemFiles, new CustomPackDuplicatePackFileResolver());
                AddContainer(container);

                var notCaPacksLoaded = _packFiles.Count(x => !x.IsCaPackFile);
                if (container.IsCaPackFile == false && setToMainPackIfFirst)
                    SetEditablePack(container);

                return container;
            }
            catch (Exception e)
            {
                MessageBox.Show($"Failed to load file {packFileSystemPath}. Error : {e.Message}", "Error");
                _logger.Here().Error($"Failed to load file {packFileSystemPath}. Error : {e}");
                return null;
            }
        }

        public bool LoadAllCaFiles(GameTypeEnum gameEnum)
        {
            var game = _gameInformationFactory.GetGameById(gameEnum);
            var gamePathInfo = _settingsService.CurrentSettings.GameDirectories.FirstOrDefault(x => x.Game == game.Type);
            var gameDataFolder = gamePathInfo!.Path;
            var gameName = game.DisplayName;

            try
            {
                _logger.Here().Information($"Loading pack files for {gameName} located in {gameDataFolder}");
                var allCaPackFiles = ManifestHelper.GetPackFilesFromManifest(gameDataFolder);

                var packList = new List<PackFileContainer>();
                foreach (var packFilePath in allCaPackFiles)
                {
                    var path = gameDataFolder + "\\" + packFilePath;
                    if (File.Exists(path))
                    {
                        using var fileStram = File.OpenRead(path);
                        using var reader = new BinaryReader(fileStram, Encoding.ASCII);

                        var pack = PackFileSerializer.Load(path, reader, _settingsService.CurrentSettings.LoadWemFiles, new CaPackDuplicatePackFileResolver());
                        packList.Add(pack);
                    }
                    else
                    {
                        _logger.Here().Warning($"{gameName} pack file '{path}' not found, loading skipped");
                    }
                }

                var caPackFileContainer = new PackFileContainer($"All Game Packs - {gameName}");
                caPackFileContainer.IsCaPackFile = true;
                caPackFileContainer.SystemFilePath = gameDataFolder;
                var packFilesOrderedByGroup = packList.GroupBy(x => x.Header.LoadOrder).OrderBy(x => x.Key);

                foreach (var group in packFilesOrderedByGroup)
                {
                    var packFilesOrderedByName = group.OrderBy(x => x.Name);
                    foreach (var packfile in packFilesOrderedByName)
                        caPackFileContainer.MergePackFileContainer(packfile);
                }

                AddContainer(caPackFileContainer);
            }
            catch (Exception e)
            {
                _logger.Here().Error($"Trying to get all CA packs in {gameDataFolder}. Error : {e.ToString()}");
                return false;
            }

            return true;
        }

        public PackFileContainer CreateNewPackFileContainer(string name, PackFileCAType type, bool setEditablePack = false)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new Exception("Name can not be empty");

            var newPackFile = new PackFileContainer(name)
            {
                Header = new PFHeader("PFH5", type),

            };
            AddContainer(newPackFile);
            if (setEditablePack)
                SetEditablePack(newPackFile);
            return newPackFile;
        }

        public void AddFilesToPack(PackFileContainer container, List<NewPackFileEntry> newFiles)
        {
            if (container.IsCaPackFile)
                throw new Exception("Can not add files to ca pack file");

            foreach (var file in newFiles)
            {
                if (string.IsNullOrWhiteSpace(file.PackFile.Name))
                    throw new Exception("PackFile name can not be empty");

                if (string.IsNullOrWhiteSpace(file.DirectoyPath))
                    throw new Exception("Directory name can not be empty");
            }

            foreach (var file in newFiles)
            {
                var path = file.DirectoyPath;
                if (!string.IsNullOrWhiteSpace(path))
                    path += "\\";
                path += file.PackFile.Name;
                container.FileList[path.ToLower()] = file.PackFile;
            }

            var files = newFiles.Select(x => x.PackFile).ToList();
            _globalEventHub?.PublishGlobalEvent(new PackFileContainerFilesAddedEvent(container, files));
        }

        public void CopyFileFromOtherPackFile(PackFileContainer source, string path, PackFileContainer target)
        {
            var lowerPath = path.Replace('/', '\\').ToLower().Trim();
            if (source.FileList.ContainsKey(lowerPath))
            {
                var file = source.FileList[lowerPath];
                var newFile = new PackFile(file.Name, file.DataSource);
                target.FileList[lowerPath] = newFile;

                _globalEventHub?.PublishGlobalEvent(new PackFileContainerFilesAddedEvent(target, [newFile]));
            }
        }

        public void SetEditablePack(PackFileContainer? pf)
        {
            if (pf != null && pf.IsCaPackFile)
                throw new Exception("Trying to set CA packfile container to be editable - this is not legal!");
            _packSelectedForEdit = pf;
            _globalEventHub?.PublishGlobalEvent(new PackFileContainerSetAsMainEditableEvent(pf));
        }

        public PackFileContainer? GetEditablePack() => _packSelectedForEdit;
        public bool HasEditablePackFile()
        {
            if (GetEditablePack() != null)
                return true;
            
            MessageBox.Show("Unable to complete operation, Editable packfile not set.", "Error");
            return false;
        }

        public void UnloadPackContainer(PackFileContainer pf)
        {
            var e = new BeforePackFileContainerRemovedEvent(pf);
            _globalEventHub?.PublishGlobalEvent(e);

            if (e.AllowClose == false)
                return;

            _packFiles.Remove(pf);
            if (_packSelectedForEdit == pf)
                SetEditablePack(null);

            _globalEventHub?.PublishGlobalEvent(new PackFileContainerRemovedEvent(pf));
        }

        public void DeleteFolder(PackFileContainer pf, string folder)
        {
            if (pf.IsCaPackFile)
                throw new Exception("Can not delete folder inside CA pack file");

            var folderLower = folder.ToLower();
            var itemsToDelete = pf.FileList
                .Where(x => string.Equals(Path.GetDirectoryName(x.Key), folder, StringComparison.InvariantCultureIgnoreCase))
                .ToList();

            _globalEventHub?.PublishGlobalEvent(new PackFileContainerFolderRemovedEvent(pf, folder));

            foreach (var item in itemsToDelete)
            {
                _logger.Here().Information($"Deleting file {item.Key} in directory {folder}");
                pf.FileList.Remove(item.Key);
            }
        }

        public void DeleteFile(PackFileContainer pf, PackFile file)
        {
            if (pf.IsCaPackFile)
                throw new Exception("Can not delete files inside CA pack file");

            var key = pf.FileList.FirstOrDefault(x => x.Value == file).Key;
            _logger.Here().Information($"Deleting file {key}");

            _globalEventHub?.PublishGlobalEvent(new PackFileContainerFilesRemovedEvent(pf, [file]));
            pf.FileList.Remove(key);
        }

        public void MoveFile(PackFileContainer pf, PackFile file, string newFolderPath)
        {
            if (pf.IsCaPackFile)
                throw new Exception("Can not move files inside CA pack file");

            var newFullPath = newFolderPath + "\\" + file.Name;

            var key = pf.FileList.FirstOrDefault(x => x.Value == file).Key;
            pf.FileList.Remove(key);
            pf.FileList[newFullPath] = file;

            _logger.Here().Information($"Moving file {key}");

            _globalEventHub?.PublishGlobalEvent(new PackFileContainerFilesUpdatedEvent(pf, [file]));
        }

        public void RenameDirectory(PackFileContainer pf, string currentNodeName, string newName)
        {
            if (pf.IsCaPackFile)
                throw new Exception("Can not rename in ca pack file");

            if (string.IsNullOrWhiteSpace(newName))
                throw new Exception("Name can not be empty");

            var oldNodePath = currentNodeName;
            var newNodePath = currentNodeName;

            var files = pf.FileList.Where(x => x.Key.StartsWith(oldNodePath)).ToList();
            foreach (var (path, file) in files)
            {
                pf.FileList.Remove(path);
                var newPath = newNodePath;
                if (oldNodePath.Length != 0)
                    newPath = path.Replace(oldNodePath, newNodePath);

                pf.FileList[newPath] = file;
            }

            _globalEventHub?.PublishGlobalEvent(new PackFileContainerFolderRenamedEvent(pf, newNodePath));
        }


        public void RenameFile(PackFileContainer pf, PackFile file, string newName)
        {
            if (pf.IsCaPackFile)
                throw new Exception("Can not rename file in ca pack file");

            if (string.IsNullOrWhiteSpace(newName))
                throw new Exception("Name can not be empty");

            var key = pf.FileList.FirstOrDefault(x => x.Value == file).Key;
            pf.FileList.Remove(key);

            var dir = Path.GetDirectoryName(key);
            file.Name = newName;
            pf.FileList[dir + "\\" + file.Name] = file;

            _globalEventHub?.PublishGlobalEvent(new PackFileContainerFilesUpdatedEvent(pf, [file]));
        }

        public void SaveFile(PackFile file, byte[] data)
        {
            var pf = GetEditablePack();
            if (pf.IsCaPackFile)
                throw new Exception("Can not save ca pack file");
            file.DataSource = new MemorySource(data);

            _globalEventHub?.PublishGlobalEvent(new PackFileContainerFilesUpdatedEvent(pf, [file]));
            _globalEventHub?.PublishGlobalEvent(new PackFileSavedEvent(file));
        }

        public void SavePackContainer(PackFileContainer pf, string path, bool createBackup)
        {
            if (File.Exists(path) && DirectoryHelper.IsFileLocked(path))
            {
                throw new IOException($"Cannot access {path} because another process has locked it, most likely the game.");
            }

            if (pf.IsCaPackFile)
                throw new Exception("Can not save ca pack file");
            if (createBackup)
                SaveUtility.CreateFileBackup(path);

            // Check if file has changed in size
            if (pf.OriginalLoadByteSize != -1)
            {
                var fileInfo = new FileInfo(pf.SystemFilePath);
                var byteSize = fileInfo.Length;
                if (byteSize != pf.OriginalLoadByteSize)
                    throw new Exception("File has been changed outside of AssetEditor. Can not save the file as it will cause corruptions");
            }

            pf.SystemFilePath = path;
            using (var memoryStream = new FileStream(path + "_temp", FileMode.OpenOrCreate))
            {
                using var writer = new BinaryWriter(memoryStream);
                pf.SaveToByteArray(writer);
            }

            File.Delete(path);
            File.Move(path + "_temp", path);

            pf.OriginalLoadByteSize = new FileInfo(path).Length;
        }

        public PackFileContainer? GetPackFileContainer(PackFile file)
        {
            foreach (var pf in _packFiles)
            {
                var res = pf.FileList.FirstOrDefault(x => x.Value == file).Value;
                if (res != null)
                    return pf;
            }
            _logger.Here().Information($"Unknown packfile container for {file.Name}");
            return null;
        }

        public PackFile? FindFile(string path, PackFileContainer? container = null)
        {
            var lowerPath = path.Replace('/', '\\').ToLower().Trim();

            if (container == null)
            {
                for (var i = _packFiles.Count - 1; i >= 0; i--)
                {
                    if (_packFiles[i].FileList.ContainsKey(lowerPath))
                    {
                        if (EnableFileLookUpEvents)
                            _globalEventHub?.PublishGlobalEvent(new PackFileLookUpEvent(path, _packFiles[i], true));
                        return _packFiles[i].FileList[lowerPath];
                    }
                }
            }
            else
            {
                if (container.FileList.ContainsKey(lowerPath))
                {
                    if (EnableFileLookUpEvents)
                        _globalEventHub?.PublishGlobalEvent(new PackFileLookUpEvent(path, container, true));
                    return container.FileList[lowerPath];
                }
            }

            if (EnableFileLookUpEvents)
                _globalEventHub?.PublishGlobalEvent(new PackFileLookUpEvent(path, null, false));
            return null;
        }

        public string GetFullPath(PackFile file, PackFileContainer? container = null)
        {
            if (container == null)
            {
                foreach (var pf in _packFiles)
                {
                    var res = pf.FileList.FirstOrDefault(x => x.Value == file).Key;
                    if (string.IsNullOrWhiteSpace(res) == false)
                        return res;
                }
            }
            else
            {
                var res = container.FileList.FirstOrDefault(x => x.Value == file).Key;
                if (string.IsNullOrWhiteSpace(res) == false)
                    return res;
            }
            throw new Exception("Unknown path for " + file.Name);
        }
    }

    public record NewPackFileEntry(string DirectoyPath, PackFile PackFile);
}
