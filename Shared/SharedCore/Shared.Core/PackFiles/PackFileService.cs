using System.Windows;
using Shared.Core.ErrorHandling;
using Shared.Core.Events;
using Shared.Core.Events.Global;
using Shared.Core.PackFiles.Models;
using Shared.Core.PackFiles.Models.Containers;
using Shared.Core.PackFiles.Models.FileSources;
using Shared.Core.PackFiles.Serialization;
using Shared.Core.PackFiles.Utility;
using Shared.Core.Settings;

namespace Shared.Core.PackFiles
{
    class PackFileService : IPackFileService
    {
        private readonly ILogger _logger = Logging.Create<PackFileService>();
        private readonly IGlobalEventHub? _globalEventHub;

        private readonly List<IPackFileContainerInternal> _packFileContainers = [];
        private IPackFileContainerInternal? _packFileContainerSelectedForEdit;

        // We use this instead of the standard dialog helper, to avaid a circular dependency
        public ISimpleMessageBox MessageBoxProvider { get; set; } = new SimpleMessageBox();
        public bool EnableFileLookUpEvents { get; set; } = false;
        public bool EnforceGameFilesMustBeLoaded { get; set; } = true;

        public PackFileService(IGlobalEventHub? globalEventHub)
        {
            _globalEventHub = globalEventHub;
        }

        internal static IPackFileContainerInternal CastContainer(IPackFileContainer container) => (IPackFileContainerInternal)container;

        public List<IPackFileContainer> GetAllPackfileContainers() => _packFileContainers.Cast<IPackFileContainer>().ToList();

        public IPackFileContainer? AddContainer(IPackFileContainer container, bool setToMainPackIfFirst = false)
        {
            var pf = CastContainer(container);
            if (EnforceGameFilesMustBeLoaded)
            {
                var caPacksLoaded = _packFileContainers.Count(x => x.IsCaPackFile);
                if (caPacksLoaded == 0 && pf.IsCaPackFile == false)
                {
                    _logger.Here().Warning($"Rejected loading pack file '{DescribeContainer(pf)}' because no CA pack files are loaded yet");
                    MessageBoxProvider.ShowDialogBox("You are trying to load a pack file before loading CA packfile. Most editors EXPECT the CA packfiles to be loaded and will cause issues if they are not.\nFile not loaded!", "Error");
                    return null;
                }
            }

            // Check if already added!
            foreach (var packFile in _packFileContainers)
            {
                if (packFile.SystemFilePath != null && packFile.SystemFilePath == pf.SystemFilePath)
                {
                    _logger.Here().Warning($"Rejected loading duplicate pack file '{packFile.SystemFilePath}'");
                    MessageBoxProvider.ShowDialogBox($"Pack file \"{packFile.SystemFilePath}\" is already loaded.", "Error");
                    return null;
                }
            }

            AddContainerInternal(pf, setToMainPackIfFirst);
            return pf;
        }

        void AddContainerInternal(IPackFileContainerInternal container, bool setToMainPackIfFirst = false)
        {
            _packFileContainers.Add(container);
            var notCaPacksLoaded = _packFileContainers.Count(x => !x.IsCaPackFile);
            _logger.Here().Information($"Added pack file container '{DescribeContainer(container)}' (CA:{container.IsCaPackFile}). Loaded containers: {_packFileContainers.Count}, editable containers: {notCaPacksLoaded}");
            _globalEventHub?.PublishGlobalEvent(new PackFileContainerAddedEvent(container));

            if (container.IsCaPackFile == false && setToMainPackIfFirst)
            {
                _logger.Here().Information($"Setting '{DescribeContainer(container)}' as editable pack after load");
                SetEditablePack(container);
            }
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

            _logger.Here().Information($"Creating new pack file container '{name}' with version '{versionString}' and type '{type}'");
            AddContainerInternal(newPackFile, setEditablePack);

            return newPackFile;
        }

        public void AddFilesToPack(IPackFileContainer container, List<NewPackFileEntry> newFiles)
        {
            var pf = CastContainer(container);
            if (pf.IsCaPackFile)
                throw new Exception("Can not add files to ca pack file");

            _logger.Here().Information($"Adding {newFiles.Count} file(s) to '{DescribeContainer(pf)}'");
            var addedFiles = pf.AddFiles(newFiles);
            _logger.Here().Information($"Added {addedFiles.Count} file(s) to '{DescribeContainer(pf)}'");
            _globalEventHub?.PublishGlobalEvent(new PackFileContainerFilesAddedEvent(pf, addedFiles));
        }

        public void CopyFileFromOtherPackFile(IPackFileContainer source, string path, IPackFileContainer target)
        {
            var pf = CastContainer(target);
            if (pf.IsCaPackFile)
                throw new Exception("Can not add files to ca pack file");

            var sourceContainer = CastContainer(source);
            var targetContainer = CastContainer(target);
            var lowerPath = PathNormalization.NormalizeFileName(path);
            var file = sourceContainer.FindFile(lowerPath);
            if (file != null)
            {
                var data = file.DataSource.ReadData();
                var newFile = new PackFile(file.Name, new MemorySource(data));
                targetContainer.AddOrUpdateFile(lowerPath, newFile);

                _logger.Here().Information($"Copied '{lowerPath}' from '{DescribeContainer(sourceContainer)}' to '{DescribeContainer(targetContainer)}'");

                _globalEventHub?.PublishGlobalEvent(new PackFileContainerFilesAddedEvent(targetContainer, [newFile]));
            }
            else
            {
                _logger.Here().Warning($"Unable to copy '{lowerPath}' from '{DescribeContainer(sourceContainer)}' because the file was not found");
            }
        }

        public void SetEditablePack(IPackFileContainer? pf)
        {
            if (pf != null && pf.IsCaPackFile)
                throw new Exception("Trying to set CA packfile container to be editable - this is not legal!");
            _packFileContainerSelectedForEdit = pf != null ? CastContainer(pf) : null;
            _logger.Here().Information($"Editable pack set to '{DescribeContainer(_packFileContainerSelectedForEdit)}'");
            _globalEventHub?.PublishGlobalEvent(new PackFileContainerSetAsMainEditableEvent(pf));
        }

        public IPackFileContainer? GetEditablePack() => _packFileContainerSelectedForEdit;

        public void UnloadPackContainer(IPackFileContainer pf)
        {
            var container = CastContainer(pf);
            _logger.Here().Information($"Unload requested for pack file container '{DescribeContainer(container)}'");
            var e = new BeforePackFileContainerRemovedEvent(container);
            _globalEventHub?.PublishGlobalEvent(e);

            if (e.AllowClose == false)
            {
                _logger.Here().Information($"Unload cancelled for pack file container '{DescribeContainer(container)}'");
                return;
            }

            _packFileContainers.Remove(container);
            if (_packFileContainerSelectedForEdit == container)
                SetEditablePack(null);

            _logger.Here().Information($"Unloaded pack file container '{DescribeContainer(container)}'. Remaining containers: {_packFileContainers.Count}");
            _globalEventHub?.PublishGlobalEvent(new PackFileContainerRemovedEvent(container));
        }

        public List<(string FileName, PackFile Pack)> FindAllWithExtention(string extention, IPackFileContainer? pf = null)
        {
            if (pf != null)
            {
                var container = CastContainer(pf);
                return container.FindAllWithExtention(extention);
            }

            var output = new List<(string, PackFile)>();
            foreach (var instance in _packFileContainers)
                output.AddRange(instance.FindAllWithExtention(extention));
            return output;
        }

        public void DeleteFolder(IPackFileContainer pf, string folder)
        {
            var container = CastContainer(pf);
            if (container.IsCaPackFile)
                throw new Exception("Can not delete folder inside CA pack file");

            _logger.Here().Information($"Deleting folder '{folder}' from '{DescribeContainer(container)}'");
            _globalEventHub?.PublishGlobalEvent(new PackFileContainerFolderRemovedEvent(container, folder));
            container.DeleteFolder(folder);
        }

        public void DeleteFile(IPackFileContainer pf, PackFile file)
        {
            var container = CastContainer(pf);
            if (container.IsCaPackFile)
                throw new Exception("Can not delete files inside CA pack file");

            _logger.Here().Information($"Deleting file '{DescribeFile(container, file)}' from '{DescribeContainer(container)}'");
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
            _logger.Here().Information($"Moved file '{key ?? file.Name}' to '{newFolderPath}' in '{DescribeContainer(container)}'");
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
            _logger.Here().Information($"Renamed directory '{currentNodeName}' to '{newNodePath}' in '{DescribeContainer(container)}'");
            _globalEventHub?.PublishGlobalEvent(new PackFileContainerFolderRenamedEvent(container, currentNodeName, newNodePath));
        }

        public void RenameFile(IPackFileContainer pf, PackFile file, string newName)
        {
            var container = CastContainer(pf);
            if (container.IsCaPackFile)
                throw new Exception("Can not rename file in ca pack file");

            if (string.IsNullOrWhiteSpace(newName))
                throw new Exception("Name can not be empty");

            var previousPath = container.GetFullPath(file) ?? file.Name;
            container.RenameFile(file, newName);
            _logger.Here().Information($"Renamed file '{previousPath}' to '{DescribeFile(container, file)}' in '{DescribeContainer(container)}'");
            _globalEventHub?.PublishGlobalEvent(new PackFileContainerFilesUpdatedEvent(container, [file]));
        }

        public void SaveFile(PackFile file, byte[] data)
        {
            var pf = _packFileContainerSelectedForEdit;
            if (pf == null)
                throw new Exception("No editable pack file is set");
            if (pf.IsCaPackFile)
                throw new Exception("Can not save ca pack file");

            _logger.Here().Information($"Saving file '{DescribeFile(pf, file)}' to '{DescribeContainer(pf)}' ({data.Length} bytes)");
            pf.SaveFileData(file, data);
            _globalEventHub?.PublishGlobalEvent(new PackFileContainerFilesUpdatedEvent(pf, [file]));
            _globalEventHub?.PublishGlobalEvent(new PackFileSavedEvent(file));
        }

        public void SavePackContainer(IPackFileContainer pf, string path, bool createBackup, GameInformation gameInformation)
        {
            var container = CastContainer(pf);
            if (container.IsCaPackFile)
                throw new Exception("Can not save ca pack file");

            _logger.Here().Information($"Saving pack file container '{DescribeContainer(container)}' to '{path}' (CreateBackup:{createBackup}, Game:{gameInformation.DisplayName})");
            container.SaveToDisk(path, createBackup, gameInformation);
            _logger.Here().Information($"Saved pack file container '{DescribeContainer(container)}' to '{path}'");
            _globalEventHub?.PublishGlobalEvent(new PackFileContainerSavedEvent(container));
        }

        public IPackFileContainer? GetPackFileContainer(PackFile file)
        {
            foreach (var pf in _packFileContainers)
            {
                var path = pf.GetFullPath(file);
                if (path != null)
                    return pf;
            }
            _logger.Here().Warning($"Unknown packfile container for file '{file.Name}'");
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

            _logger.Here().Warning($"Unable to resolve full path for file '{file.Name}'");
            throw new Exception("Unknown path for " + file.Name);
        }

        private static string DescribeContainer(IPackFileContainer? container)
        {
            if (container == null)
                return "<none>";

            var concreteContainer = CastContainer(container);
            return concreteContainer.SystemFilePath ?? concreteContainer.Name;
        }

        private static string DescribeFile(IPackFileContainer container, PackFile file)
        {
            var concreteContainer = CastContainer(container);
            return concreteContainer.GetFullPath(file) ?? file.Name;
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
