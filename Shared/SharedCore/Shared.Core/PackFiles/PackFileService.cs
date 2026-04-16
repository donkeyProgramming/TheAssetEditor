using System.Windows;
using Shared.Core.ErrorHandling;
using Shared.Core.Events;
using Shared.Core.Events.Global;
using Shared.Core.Misc;
using Shared.Core.PackFiles.Models;
using Shared.Core.PackFiles.Serialization;
using Shared.Core.Settings;

namespace Shared.Core.PackFiles
{
    class PackFileService : IPackFileService
    {
        private readonly ILogger _logger = Logging.Create<PackFileService>();
        private readonly IGlobalEventHub? _globalEventHub;

        private readonly List<PackFileContainer> _packFileContainers = [];
        private PackFileContainer? _packFileContainerSelectedForEdit;

        // We use this instead of the standard dialog helper, to avaid a circular dependency
        public ISimpleMessageBox MessageBoxProvider { get; set; } = new SimpleMessageBox();
        public bool EnableFileLookUpEvents { get; set; } = false;
        public bool EnforceGameFilesMustBeLoaded { get; set; } = true;

        public PackFileService(IGlobalEventHub? globalEventHub)
        {
            _globalEventHub = globalEventHub;
        }

        public List<PackFileContainer> GetAllPackfileContainers() => _packFileContainers.ToList(); // Return a list of the list to avoid bugs!

        public PackFileContainer? AddContainer(PackFileContainer container, bool setToMainPackIfFirst = false)
        {
            if (EnforceGameFilesMustBeLoaded)
            {
                var caPacksLoaded = _packFileContainers.Count(x => x.IsCaPackFile);
                if (caPacksLoaded == 0 && container.IsCaPackFile == false)
                {
                    MessageBoxProvider.ShowDialogBox("You are trying to load a pack file before loading CA packfile. Most editors EXPECT the CA packfiles to be loaded and will cause issues if they are not.\nFile not loaded!", "Error");
                    return null;
                }
            }

            // Check if already added!
            foreach (var packFile in _packFileContainers)
            {
                if (packFile.SystemFilePath == container.SystemFilePath)
                {
                    MessageBoxProvider.ShowDialogBox($"Pack file \"{packFile.SystemFilePath}\" is already loaded.", "Error");
                    return null;
                }
            }

            AddContainerInternal(container, setToMainPackIfFirst);
            return container;
        }

        void AddContainerInternal(PackFileContainer container, bool setToMainPackIfFirst = false)
        {
            _packFileContainers.Add(container);
            _globalEventHub?.PublishGlobalEvent(new PackFileContainerAddedEvent(container));

            var notCaPacksLoaded = _packFileContainers.Count(x => !x.IsCaPackFile);
            if (container.IsCaPackFile == false && setToMainPackIfFirst)
                SetEditablePack(container);
        }

        public PackFileContainer CreateNewPackFileContainer(string name, PackFileVersion packFileVersion, PackFileCAType type, bool setEditablePack = false)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new Exception("Name can not be empty");

            var versionString = PackFileVersionConverter.ToString(packFileVersion);
            var newPackFile = new PackFileContainer(name)
            {
                Header = new PFHeader(versionString, type),
            };

            AddContainerInternal(newPackFile, setEditablePack);

            return newPackFile;
        }

        public void AddFilesToPack(PackFileContainer container, List<NewPackFileEntry> newFiles)
        {
            if (container.IsCaPackFile)
                throw new Exception("Can not add files to ca pack file");

            var addedFiles = container.AddFiles(newFiles);
            _globalEventHub?.PublishGlobalEvent(new PackFileContainerFilesAddedEvent(container, addedFiles));
        }

        public void CopyFileFromOtherPackFile(PackFileContainer source, string path, PackFileContainer target)
        {
            var lowerPath = path.Replace('/', '\\').ToLower().Trim();
            if (source.FileList.ContainsKey(lowerPath))
            {
                var file = source.FileList[lowerPath];
                var data = file.DataSource.ReadData();
                var newFile = new PackFile(file.Name, new MemorySource(data));
                target.FileList[lowerPath] = newFile;

                _globalEventHub?.PublishGlobalEvent(new PackFileContainerFilesAddedEvent(target, [newFile]));
            }
        }

        public void SetEditablePack(PackFileContainer? pf)
        {
            if (pf != null && pf.IsCaPackFile)
                throw new Exception("Trying to set CA packfile container to be editable - this is not legal!");
            _packFileContainerSelectedForEdit = pf;
            _globalEventHub?.PublishGlobalEvent(new PackFileContainerSetAsMainEditableEvent(pf));
        }

        public PackFileContainer? GetEditablePack() => _packFileContainerSelectedForEdit;

        public void UnloadPackContainer(PackFileContainer pf)
        {
            var e = new BeforePackFileContainerRemovedEvent(pf);
            _globalEventHub?.PublishGlobalEvent(e);

            if (e.AllowClose == false)
                return;

            _packFileContainers.Remove(pf);
            if (_packFileContainerSelectedForEdit == pf)
                SetEditablePack(null);

            _globalEventHub?.PublishGlobalEvent(new PackFileContainerRemovedEvent(pf));
        }

        public void DeleteFolder(PackFileContainer pf, string folder)
        {
            if (pf.IsCaPackFile)
                throw new Exception("Can not delete folder inside CA pack file");

            _globalEventHub?.PublishGlobalEvent(new PackFileContainerFolderRemovedEvent(pf, folder));
            pf.DeleteFolder(folder);
        }

        public void DeleteFile(PackFileContainer pf, PackFile file)
        {
            if (pf.IsCaPackFile)
                throw new Exception("Can not delete files inside CA pack file");

            _logger.Here().Information($"Deleting file {pf.GetFullPath(file)}");
            _globalEventHub?.PublishGlobalEvent(new PackFileContainerFilesRemovedEvent(pf, [file]));
            pf.DeleteFile(file);
        }

        public void MoveFile(PackFileContainer pf, PackFile file, string newFolderPath)
        {
            if (pf.IsCaPackFile)
                throw new Exception("Can not move files inside CA pack file");

            var key = pf.GetFullPath(file);
            _globalEventHub?.PublishGlobalEvent(new PackFileContainerFilesRemovedEvent(pf, [file]));
            pf.MoveFile(file, newFolderPath);
            _logger.Here().Information($"Moving file {key}");
            _globalEventHub?.PublishGlobalEvent(new PackFileContainerFilesAddedEvent(pf, [file]));
        }

        public void RenameDirectory(PackFileContainer pf, string currentNodeName, string newName)
        {
            if (pf.IsCaPackFile)
                throw new Exception("Can not rename in ca pack file");

            if (string.IsNullOrWhiteSpace(newName))
                throw new Exception("Name can not be empty");

            var newNodePath = pf.RenameDirectory(currentNodeName, newName);
            _globalEventHub?.PublishGlobalEvent(new PackFileContainerFolderRenamedEvent(pf, newNodePath));
        }

        public void RenameFile(PackFileContainer pf, PackFile file, string newName)
        {
            if (pf.IsCaPackFile)
                throw new Exception("Can not rename file in ca pack file");

            if (string.IsNullOrWhiteSpace(newName))
                throw new Exception("Name can not be empty");

            pf.RenameFile(file, newName);
            _globalEventHub?.PublishGlobalEvent(new PackFileContainerFilesUpdatedEvent(pf, [file]));
        }

        public void SaveFile(PackFile file, byte[] data)
        {
            var pf = GetEditablePack();
            if (pf.IsCaPackFile)
                throw new Exception("Can not save ca pack file");

            pf.SaveFileData(file, data);
            _globalEventHub?.PublishGlobalEvent(new PackFileContainerFilesUpdatedEvent(pf, [file]));
            _globalEventHub?.PublishGlobalEvent(new PackFileSavedEvent(file));
        }

        public void SavePackContainer(PackFileContainer pf, string path, bool createBackup, GameInformation gameInformation)
        {
            if (pf.IsCaPackFile)
                throw new Exception("Can not save ca pack file");

            pf.SaveToDisk(path, createBackup, gameInformation);
            _globalEventHub?.PublishGlobalEvent(new PackFileContainerSavedEvent(pf));
        }

        public PackFileContainer? GetPackFileContainer(PackFile file)
        {
            foreach (var pf in _packFileContainers)
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
            if (container == null)
            {
                for (var i = _packFileContainers.Count - 1; i >= 0; i--)
                {
                    var result = _packFileContainers[i].FindFile(path);
                    if (result != null)
                    {
                        if (EnableFileLookUpEvents)
                            _globalEventHub?.PublishGlobalEvent(new PackFileLookUpEvent(path, _packFileContainers[i], true));
                        return result;
                    }
                }
            }
            else
            {
                var result = container.FindFile(path);
                if (result != null)
                {
                    if (EnableFileLookUpEvents)
                        _globalEventHub?.PublishGlobalEvent(new PackFileLookUpEvent(path, container, true));
                    return result;
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
                foreach (var pf in _packFileContainers)
                {
                    var res = pf.GetFullPath(file);
                    if (res != null)
                        return res;
                }
            }
            else
            {
                var res = container.GetFullPath(file);
                if (res != null)
                    return res;
            }

            throw new Exception("Unknown path for " + file.Name);
        }
    }

    public record NewPackFileEntry(string DirectoyPath, PackFile PackFile);

    public interface ISimpleMessageBox
    {
        void ShowDialogBox(string message, string title);
    }

    public class SimpleMessageBox : ISimpleMessageBox
    {
        public void ShowDialogBox(string message, string title) => MessageBox.Show(message, title);
    }
}
