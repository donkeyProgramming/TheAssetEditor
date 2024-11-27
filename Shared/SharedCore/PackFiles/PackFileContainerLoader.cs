using System.Reflection;
using System.Text;
using Serilog;
using Shared.Core.ErrorHandling;
using Shared.Core.PackFiles.Models;
using Shared.Core.Services;

namespace Shared.Core.PackFiles
{
    public interface IPackFileContainerLoader
    {
        PackFileContainer? Load(string packFileSystemPath);
        PackFileContainer? LoadAllCaFiles(GameTypeEnum gameEnum);
        PackFileContainer LoadSystemFolderAsPackFileContainer(string packFileSystemPath);
    }


    public class PackFileContainerLoader : IPackFileContainerLoader
    {
        static private readonly ILogger _logger = Logging.CreateStatic(typeof(PackFileContainerLoader));
        private readonly ApplicationSettingsService _settingsService;
        private readonly GameInformationFactory _gameInformationFactory;

        public PackFileContainerLoader(ApplicationSettingsService settingsService, GameInformationFactory gameInformationFactory)
        {
            _settingsService = settingsService;
            _gameInformationFactory = gameInformationFactory;
        }

        public PackFileContainer LoadSystemFolderAsPackFileContainer(string packFileSystemPath)
        {
            if (Directory.Exists(packFileSystemPath) == false)
            {
                var location = Assembly.GetEntryAssembly()!.Location;
                var loactionDir = Path.GetDirectoryName(location);
                throw new Exception($"Unable to find folder {packFileSystemPath}. Curret systempath is {loactionDir}");
            }

            var container = new PackFileContainer(packFileSystemPath);
            AddFolderContentToPackFile(container, packFileSystemPath, packFileSystemPath.ToLower() + "\\");
            return container;
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

        public PackFileContainer? Load(string packFileSystemPath)
        {
            try
            {
                if (!File.Exists(packFileSystemPath))
                {
                    _logger.Here().Error($"Trying to load file {packFileSystemPath}, which can not be located.", "Error");
                    System.Windows.MessageBox.Show($"Unable to locate pack file \"{packFileSystemPath}\"");
                    return null;
                }

                using var fileStream = File.OpenRead(packFileSystemPath);
                using var reader = new BinaryReader(fileStream, Encoding.ASCII);

                var container = PackFileSerializer.Load(packFileSystemPath, reader, _settingsService.CurrentSettings.LoadWemFiles, new CustomPackDuplicatePackFileResolver());

                return container;
            }
            catch (Exception e)
            {
                var errorMessage = $"Failed to load file {packFileSystemPath}. Error : {e.Message}";
                _logger.Here().Error(errorMessage);
                throw new Exception(errorMessage, e);
            }
        }

        public PackFileContainer? LoadAllCaFiles(GameTypeEnum gameEnum)
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
                Parallel.ForEach(allCaPackFiles, packFilePath =>
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
                
                });

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

                return caPackFileContainer;
            }
            catch (Exception e)
            {
                _logger.Here().Error($"Trying to get all CA packs in {gameDataFolder}. Error : {e.ToString()}");
                return null;
            }
        } // 2000
    }
}
