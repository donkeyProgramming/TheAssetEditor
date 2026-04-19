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

        private static PackFileContainer CastContainer(IPackFileContainer container) => (PackFileContainer)container;

        public List<IPackFileContainer> GetAllPackfileContainers() => _packFileContainers.Cast<IPackFileContainer>().ToList();

        public IPackFileContainer? AddContainer(IPackFileContainer container, bool setToMainPackIfFirst = false)
        {
            var pf = CastContainer(container);
            if (EnforceGameFilesMustBeLoaded)
            {
                var caPacksLoaded = _packFileContainers.Count(x => x.IsCaPackFile);
                if (caPacksLoaded == 0 && pf.IsCaPackFile == false)
                {
                    MessageBoxProvider.ShowDialogBox("You are trying to load a pack file before loading CA packfile. Most editors EXPECT the CA packfiles to be loaded and will cause issues if they are not.\nFile not loaded!", "Error");
                    return null;
                }
            }

            // Check if already added!
            foreach (var packFile in _packFileContainers)
            {
                if (packFile.SystemFilePath != null && packFile.SystemFilePath == pf.SystemFilePath)
                {
                    MessageBoxProvider.ShowDialogBox($"Pack file \"{packFile.SystemFilePath}\" is already loaded.", "Error");
                    return null;
                }
            }

            AddContainerInternal(pf, setToMainPackIfFirst);
            return pf;
        }

        void AddContainerInternal(PackFileContainer container, bool setToMainPackIfFirst = false)
        {
            _packFileContainers.Add(container);
            _globalEventHub?.PublishGlobalEvent(new PackFileContainerAddedEvent(container));

            var notCaPacksLoaded = _packFileContainers.Count(x => !x.IsCaPackFile);
            if (container.IsCaPackFile == false && setToMainPackIfFirst)
                SetEditablePack(container);
        }

        public IPackFileContainer CreateNewPackFileContainer(string name, PackFileVersion packFileVersion, PackFileCAType type, bool setEditablePack = false)
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

        public void AddFilesToPack(IPackFileContainer container, List<NewPackFileEntry> newFiles)
        {
            var pf = CastContainer(container);
            if (pf.IsCaPackFile)
                throw new Exception("Can not add files to ca pack file");

            var addedFiles = pf.AddFiles(newFiles);
            _globalEventHub?.PublishGlobalEvent(new PackFileContainerFilesAddedEvent(pf, addedFiles));
        }

        public void CopyFileFromOtherPackFile(IPackFileContainer source, string path, IPackFileContainer target)
        {
            var sourceContainer = CastContainer(source);
            var targetContainer = CastContainer(target);
            var lowerPath = path.Replace('/', '\\').ToLower().Trim();
            if (sourceContainer.FileList.ContainsKey(lowerPath))
            {
                var file = sourceContainer.FileList[lowerPath];
                var data = file.DataSource.ReadData();
                var newFile = new PackFile(file.Name, new MemorySource(data));
                targetContainer.FileList[lowerPath] = newFile;

                _globalEventHub?.PublishGlobalEvent(new PackFileContainerFilesAddedEvent(targetContainer, [newFile]));
            }
        }

        public void SetEditablePack(IPackFileContainer? pf)
        {
            if (pf != null && pf.IsCaPackFile)
                throw new Exception("Trying to set CA packfile container to be editable - this is not legal!");
            _packFileContainerSelectedForEdit = pf != null ? CastContainer(pf) : null;
            _globalEventHub?.PublishGlobalEvent(new PackFileContainerSetAsMainEditableEvent(pf));
        }

        public IPackFileContainer? GetEditablePack() => _packFileContainerSelectedForEdit;

        public void UnloadPackContainer(IPackFileContainer pf)
        {
            var container = CastContainer(pf);
            var e = new BeforePackFileContainerRemovedEvent(container);
            _globalEventHub?.PublishGlobalEvent(e);

            if (e.AllowClose == false)
                return;

            _packFileContainers.Remove(container);
            if (_packFileContainerSelectedForEdit == container)
                SetEditablePack(null);

            _globalEventHub?.PublishGlobalEvent(new PackFileContainerRemovedEvent(container));
        }

        public void DeleteFolder(IPackFileContainer pf, string folder)
        {
            var container = CastContainer(pf);
            if (container.IsCaPackFile)
                throw new Exception("Can not delete folder inside CA pack file");

            _globalEventHub?.PublishGlobalEvent(new PackFileContainerFolderRemovedEvent(container, folder));
            container.DeleteFolder(folder);
        }

        public void DeleteFile(IPackFileContainer pf, PackFile file)
        {
            var container = CastContainer(pf);
            if (container.IsCaPackFile)
                throw new Exception("Can not delete files inside CA pack file");

            _logger.Here().Information($"Deleting file {container.GetFullPath(file)}");
            _globalEventHub?.PublishGlobalEvent(new PackFileContainerFilesRemovedEvent(container, [file]));
            container.DeleteFile(file);
        }

        public void MoveFile(IPackFileContainer pf, PackFile file, string newFolderPath)
        {
            var container = CastContainer(pf);
            if (container.IsCaPackFile)
                throw new Exception("Can not move files inside CA pack file");

            var key = container.GetFullPath(file);
            _globalEventHub?.PublishGlobalEvent(new PackFileContainerFilesRemovedEvent(container, [file]));
            container.MoveFile(file, newFolderPath);
            _logger.Here().Information($"Moving file {key}");
            _globalEventHub?.PublishGlobalEvent(new PackFileContainerFilesAddedEvent(container, [file]));
        }

        public void RenameDirectory(IPackFileContainer pf, string currentNodeName, string newName)
        {
            var container = CastContainer(pf);
            if (container.IsCaPackFile)
                throw new Exception("Can not rename in ca pack file");

            if (string.IsNullOrWhiteSpace(newName))
                throw new Exception("Name can not be empty");

            var newNodePath = container.RenameDirectory(currentNodeName, newName);
            _globalEventHub?.PublishGlobalEvent(new PackFileContainerFolderRenamedEvent(container, newNodePath));
        }

        public void RenameFile(IPackFileContainer pf, PackFile file, string newName)
        {
            var container = CastContainer(pf);
            if (container.IsCaPackFile)
                throw new Exception("Can not rename file in ca pack file");

            if (string.IsNullOrWhiteSpace(newName))
                throw new Exception("Name can not be empty");

            container.RenameFile(file, newName);
            _globalEventHub?.PublishGlobalEvent(new PackFileContainerFilesUpdatedEvent(container, [file]));
        }

        public void SaveFile(PackFile file, byte[] data)
        {
            var pf = _packFileContainerSelectedForEdit;
            if (pf == null)
                throw new Exception("No editable pack file is set");
            if (pf.IsCaPackFile)
                throw new Exception("Can not save ca pack file");

            pf.SaveFileData(file, data);
            _globalEventHub?.PublishGlobalEvent(new PackFileContainerFilesUpdatedEvent(pf, [file]));
            _globalEventHub?.PublishGlobalEvent(new PackFileSavedEvent(file));
        }

        public void SavePackContainer(IPackFileContainer pf, string path, bool createBackup, GameInformation gameInformation)
        {
            var container = CastContainer(pf);
            if (container.IsCaPackFile)
                throw new Exception("Can not save ca pack file");

            container.SaveToDisk(path, createBackup, gameInformation);
            _globalEventHub?.PublishGlobalEvent(new PackFileContainerSavedEvent(container));
        }

        public IPackFileContainer? GetPackFileContainer(PackFile file)
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

        public PackFile? FindFile(string path, IPackFileContainer? container = null)
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
                var concreteContainer = CastContainer(container);
                var result = concreteContainer.FindFile(path);
                if (result != null)
                {
                    if (EnableFileLookUpEvents)
                        _globalEventHub?.PublishGlobalEvent(new PackFileLookUpEvent(path, concreteContainer, true));
                    return result;
                }
            }

            if (EnableFileLookUpEvents)
                _globalEventHub?.PublishGlobalEvent(new PackFileLookUpEvent(path, null, false));
            return null;
        }

        public string GetFullPath(PackFile file, IPackFileContainer? container = null)
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
                var concreteContainer = CastContainer(container);
                var res = concreteContainer.GetFullPath(file);
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
