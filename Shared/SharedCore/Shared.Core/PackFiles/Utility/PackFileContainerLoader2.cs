using System.Collections.Concurrent;
using System.Reflection;
using System.Text;
using Microsoft.Xna.Framework;
using Shared.Core.ErrorHandling;
using Shared.Core.PackFiles.Models;
using Shared.Core.PackFiles.Models.Containers;
using Shared.Core.PackFiles.Serialization;
using Shared.Core.PackFiles.Serialization.CacheDatabase;
using Shared.Core.Services;
using Shared.Core.Settings;

namespace Shared.Core.PackFiles.Utility
{

    public enum PackFileContainerType
    {
        Cached,
        Normal,
    }

    public interface IPackFileContainerLoader
    {
        IPackFileContainer CreateFromPackFile(PackFileContainerType type, string packFilePath, bool loadAsReadOnly);
      //  IPackFileContainer CreateFromCollection(PackFileContainerType type, string packFileSystemPath, List<string> fullPackFilePaths, string createdPackFileName, bool loadAsReadOnly, IDuplicateFileResolver duplicateFileResolver);
        IPackFileContainer? CreateFromGameEnum(PackFileContainerType type, GameTypeEnum game);

        IPackFileContainer CreateFromSystemFolder(string folderPath);
    }

    public class PackFileContainerLoader : IPackFileContainerLoader
    {
        static private readonly ILogger _logger = Logging.CreateStatic(typeof(PackFileContainerLoader));
        private readonly ApplicationSettingsService _settingsService;
        private readonly IStandardDialogs _standardDialogs;
        private readonly LocalizationManager _localizationManager;

        public PackFileContainerLoader(ApplicationSettingsService settingsService, IStandardDialogs standardDialogs, LocalizationManager localizationManager)
        {
            // TODO : Handle context for logger

            _settingsService = settingsService;
            _standardDialogs = standardDialogs;
            _localizationManager = localizationManager;
        }

        public IPackFileContainer CreateFromSystemFolder(string packFileSystemPath)
        {
            if (Directory.Exists(packFileSystemPath) == false)
            {
                var location = Assembly.GetEntryAssembly()!.Location;
                var loactionDir = Path.GetDirectoryName(location);
                throw new Exception($"Unable to find folder {packFileSystemPath}. Curret systempath is {loactionDir}");
            }

            var containerName = Path.GetFileName(packFileSystemPath);
            var container = new PackFileContainer(containerName)
            {
                SystemFilePath = packFileSystemPath,
            };
            AddFolderContentToPackFile(container, packFileSystemPath, packFileSystemPath.ToLower() + "\\");
            return container;
        }

        private static void AddFolderContentToPackFile(PackFileContainer container, string folderPath, string rootPath)
        {
            var files = Directory.GetFiles(folderPath);
            foreach (var filePath in files)
            {
                var sanatizedFilePath = filePath.ToLower();
                var relativePath = sanatizedFilePath.Replace(rootPath, "");
                var fileName = Path.GetFileName(sanatizedFilePath);

                container.AddOrUpdateFile(relativePath, PackFile.CreateFromFileSystem(fileName, sanatizedFilePath));
            }

            var folders = Directory.GetDirectories(folderPath);
            foreach (var folder in folders)
                AddFolderContentToPackFile(container, folder, rootPath);
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

            return CreateFromCollection(PackFileContainerType.Cached, gameDataFolder, fullPackFilePaths, $"All Game Packs - {gameName}", true, packfileResolver);
        }


        public IPackFileContainer CreateFromCollection(PackFileContainerType type, string packFileSystemPath, List<string> fullPackFilePaths, string createdPackFileName, bool loadAsReadOnly, IDuplicateFileResolver duplicateFileResolver)
        {
            if(type == PackFileContainerType.Cached && loadAsReadOnly == false)
                throw new InvalidOperationException($"Cannot load as writable if loading from cache. Caching is only supported for read-only containers. PackFile {createdPackFileName}");


            var fingerprint = PackFileContainerCacheHelper.ComputeFingerprint(fullPackFilePaths); 
            var cachePrefix = createdPackFileName;
            var cacheFilePath = PackFileContainerCacheHelper.GetCacheFilePath(cachePrefix, fingerprint);

            if (type == PackFileContainerType.Cached)
            {
                var cached = PackFileContainerCacheHelper.TryLoadFromCache(cacheFilePath, fingerprint);
                if (cached != null)
                    return cached;

                //var cacheInvalidReason = GetCacheInvalidReason(cacheFilePath, fingerprint);
                //_logger.Here().Information($"Cache invalid reason for {gameName}: {cacheInvalidReason}");
                //var reasonMessage = string.Format(_localizationManager.Get("PackFileCache.InvalidReason." + cacheInvalidReason), gameName);
                //var buildingMessage = string.Format(_localizationManager.Get("PackFileCache.BuildingCache"), gameName);
                //var cacheDescription = _localizationManager.Get("PackFileCache.Description");
                _standardDialogs.ShowDialogBox("Failed to load from cache - make better error later");

            }

            using (_standardDialogs.ShowWaitCursor())
            {

                var container = LoadPackFilesFromDisk(createdPackFileName, fullPackFilePaths, duplicateFileResolver);
                container.Name = createdPackFileName;
                container.IsCaPackFile = loadAsReadOnly;
                container.SystemFilePath = packFileSystemPath;

                if (type == PackFileContainerType.Cached)
                {
                    var dbOptions = PackFileContainerCacheHelper.CreateDbOptions(cacheFilePath);
                    PackFileContainerCacheHelper.SaveCache(fingerprint, container, dbOptions);
                    // Do we want to set it to the cached version? It should be the same data, just in a different format.
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

            var mergedPackFile = new PackFileContainer(createdPackFileName);
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
