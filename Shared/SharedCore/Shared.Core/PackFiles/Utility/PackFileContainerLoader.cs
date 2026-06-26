using System.Collections.Concurrent;
using System.Reflection;
using System.Text;
using Shared.Core.ErrorHandling;
using Shared.Core.Events;
using Shared.Core.PackFiles.Models;
using Shared.Core.PackFiles.Models.Containers;
using Shared.Core.PackFiles.Serialization;
using Shared.Core.PackFiles.Serialization.CacheDatabase;
using Shared.Core.Services;
using Shared.Core.Settings;

namespace Shared.Core.PackFiles.Utility
{
    public interface ISystemFolderContainerFactory
    {
        IPackFileContainer Create(string folderPath);
    }

    public class SystemFolderContainerFactory : ISystemFolderContainerFactory
    {
        private readonly IFileSystemAccess _fileSystemAccess;
        private readonly IGlobalEventHub _globalEventHub;
        private readonly Func<IFileSystemWatcher> _watcherFactory;

        public SystemFolderContainerFactory(IFileSystemAccess fileSystemAccess, IGlobalEventHub globalEventHub, Func<IFileSystemWatcher> watcherFactory)
        {
            _fileSystemAccess = fileSystemAccess;
            _globalEventHub = globalEventHub;
            _watcherFactory = watcherFactory;
        }

        public IPackFileContainer Create(string folderPath)
        {
            return new SystemFolderContainer(folderPath, _fileSystemAccess, _watcherFactory(), _globalEventHub);
        }
    }


    public interface IPackFileContainerLoader
    {
        IPackFileContainer CreateFromPackFile(PackFileContainerType type, string packFilePath, bool loadAsReadOnly);
        IPackFileContainer? CreateFromGameEnum(PackFileContainerType type, GameTypeEnum game);
        IPackFileContainer CreateFromSystemFolder(string folderPath);
    }

    class PackFileContainerLoader : IPackFileContainerLoader
    {
        static private readonly ILogger _logger = Logging.CreateStatic(typeof(PackFileContainerLoader));
        private readonly ApplicationSettingsService _settingsService;
        private readonly IStandardDialogs _standardDialogs;
        private readonly LocalizationManager _localizationManager;
        private readonly IPackFileContainerCacheHelper _packFileContainerCacheHelper;
        private readonly ISystemFolderContainerFactory _systemFolderContainerFactory;

        public PackFileContainerLoader(ApplicationSettingsService settingsService, IStandardDialogs standardDialogs, LocalizationManager localizationManager, IPackFileContainerCacheHelper packFileContainerCacheHelper, ISystemFolderContainerFactory systemFolderContainerFactory)
        {
            _settingsService = settingsService;
            _standardDialogs = standardDialogs;
            _localizationManager = localizationManager;
            _packFileContainerCacheHelper = packFileContainerCacheHelper;
            _systemFolderContainerFactory = systemFolderContainerFactory;
        }

        public IPackFileContainer CreateFromSystemFolder(string packFileSystemPath)
        {
            if (Directory.Exists(packFileSystemPath) == false)
            {
                var location = Assembly.GetEntryAssembly()!.Location;
                var loactionDir = Path.GetDirectoryName(location);
                throw new Exception($"Unable to find folder {packFileSystemPath}. Curret systempath is {loactionDir}");
            }

            var container = _systemFolderContainerFactory.Create(packFileSystemPath);
            return container;
        }

        public IPackFileContainer CreateFromPackFile(PackFileContainerType type, string packFilePath, bool loadAsReadOnly)
        {
            var packfileName = Path.GetFileNameWithoutExtension(packFilePath);
            return CreateFromCollection(type, packFilePath, [packFilePath], packfileName, loadAsReadOnly, new CustomPackDuplicateFileResolver());
        }

        public IPackFileContainer? CreateFromGameEnum(PackFileContainerType type, GameTypeEnum gameEnum)
        {
            var game = GameInformationDatabase.GetGameById(gameEnum);
            var gamePathInfo = _settingsService.CurrentSettings.GameDirectories.FirstOrDefault(x => x.Game == game.Type);
            var gameName = game.DisplayName;

            if (gamePathInfo == null || string.IsNullOrWhiteSpace(gamePathInfo.Path))
            {
                var errorMessage = $"Unable to load pack files for {gameName} because no game directory is configured.";
                _logger.Here().Error(errorMessage);
                return null;
            }

            var gameDataFolder = gamePathInfo.Path;
            var fullPackFilePaths = ManifestHelper.GetPackFilesFromManifest(gameDataFolder, out var manifestFileFound);

            // When loading ca pack packs, we want to use the CA resolver as its faster. 
            // If there is no manifest file, we need to use the duplicate resolver as it loads all file in the folder.
            // There might be custom mods in there that does not follow the rules! 
            IDuplicateFileResolver packfileResolver = new CaPackDuplicateFileResolver();
            if (manifestFileFound == false)
            {
                _logger.Here().Warning($"Loading pack files for {gameName}, which does not uses manifest.txt. If there are MODs in the game folder, this might cause issues!");
                packfileResolver = new CustomPackDuplicateFileResolver();
            }

            var container = CreateFromCollection(PackFileContainerType.Database, gameDataFolder, fullPackFilePaths, $"All Game Packs - {gameName}", true, packfileResolver);
            container.IsCaPackFile = true;
            return container;
        }


        public IPackFileContainer CreateFromCollection(PackFileContainerType type, string packFileSystemPath, List<string> fullPackFilePaths, string createdPackFileName, bool loadAsReadOnly, IDuplicateFileResolver duplicateFileResolver)
        {
            if(type == PackFileContainerType.Database && loadAsReadOnly == false)
                throw new InvalidOperationException($"Cannot load as writable if loading from cache. Caching is only supported for read-only containers. PackFile {createdPackFileName}");
       
            var fingerprint = string.Empty;
            var cacheFilePath = string.Empty;
            if (type == PackFileContainerType.Database)
            {
                fingerprint = _packFileContainerCacheHelper.ComputeFingerprint(fullPackFilePaths);
                var cachePrefix = createdPackFileName;
                cacheFilePath = _packFileContainerCacheHelper.GetCacheFilePath(cachePrefix, fingerprint);

                var cached = _packFileContainerCacheHelper.TryLoadFromCache(cacheFilePath, fingerprint);
                if (cached != null)
                    return cached;

                //var cacheInvalidReason = GetCacheInvalidReason(cacheFilePath, fingerprint);
                //_logger.Here().Information($"Cache invalid reason for {gameName}: {cacheInvalidReason}");
                //var reasonMessage = string.Format(_localizationManager.Get("PackFileCache.InvalidReason." + cacheInvalidReason), gameName);
                //var buildingMessage = string.Format(_localizationManager.Get("PackFileCache.BuildingCache"), gameName);
                //var cacheDescription = _localizationManager.Get("PackFileCache.Description");
                _standardDialogs.ShowDialogBox("Failed to load from cache - Generating new cache");
            }

            using (_standardDialogs.ShowWaitCursor())
            {
                var container = LoadPackFilesFromDisk(createdPackFileName, fullPackFilePaths, duplicateFileResolver);
                container.Name = createdPackFileName;
                container.IsReadOnly = loadAsReadOnly;
                container.SystemFilePath = packFileSystemPath;

                if (type == PackFileContainerType.Database)
                {
                    return _packFileContainerCacheHelper.SaveAndLoadCache(fingerprint, container, cacheFilePath);
                }

                return container;
            }
        }








        private static PackFileContainer LoadPackFilesFromDisk(string createdPackFileName, List<string> fullPackFilePaths, IDuplicateFileResolver packfileResolver)
        {
            var packList = new ConcurrentBag<PackFileContainer>();
            var packsCompressionStats = new ConcurrentDictionary<CompressionFormat, CompressionInformation>();

            Parallel.ForEach(fullPackFilePaths, packFilePath =>
            {
                var path = packFilePath;
                if (File.Exists(path))
                {
                    using var fileStream = File.OpenRead(path);
                    using var reader = new BinaryReader(fileStream, Encoding.ASCII);

                    var packFileSize = new FileInfo(path).Length;
                    var pack = PackFileSerializerLoader.Load(path, packFileSize, reader, packfileResolver);
                    packList.Add(pack);

                    PackFileLog.LogPackCompression(pack);
                    var packCompressionStats = PackFileLog.GetCompressionInformation(pack);
                    foreach (var kvp in packCompressionStats)
                    {
                        packsCompressionStats.AddOrUpdate(
                            kvp.Key,
                            _ => new CompressionInformation(kvp.Value.DiskSize, kvp.Value.UncompressedSize),
                            (_, existingStats) => new CompressionInformation(
                                existingStats.DiskSize + kvp.Value.DiskSize,
                                existingStats.UncompressedSize + kvp.Value.UncompressedSize));
                    }
                }
                else
                    _logger.Here().Warning($"{createdPackFileName} pack file '{path}' not found, loading skipped");
            }
            );

            PackFileLog.LogPacksCompression(packsCompressionStats);

            // If there is only one packfile - we dont need to sort. Just return it. 
            // Be aware, that when we create a new PackFileContainer in the case of multiple packfiles, we will lose the original header information of the first packfile.
            // This is because we need to create a new header for the new container. This should not be an issue, but its something to be aware of.
            if (packList.Count == 1)
                return packList.First();

            var mergedPackFile = PackFileContainer.CreatePackFile(createdPackFileName);
            var packFilesOrderedByGroup = packList.GroupBy(x => x.Header.LoadOrder).OrderBy(x => x.Key);

            foreach (var group in packFilesOrderedByGroup)
            {
                var packFilesOrderedByName = group.OrderBy(x => x.Name);
                foreach (var packfile in packFilesOrderedByName)
                {
                    if (string.IsNullOrWhiteSpace(packfile.SystemFilePath) == false)
                        mergedPackFile.SourcePackFilePaths.Add(packfile.SystemFilePath);
                    mergedPackFile.MergePackFileContainer(packfile);
                }
            }

            return mergedPackFile;
        }
    }
}
