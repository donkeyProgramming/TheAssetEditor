using System.IO;
using System.Windows.Forms;
using Shared.Core.Events;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.PackFiles.Utility;
using Shared.Core.Services;
using Shared.Core.Settings;

namespace AssetEditor.UiCommands
{
    public class ImportPackAsAsProjectCommand : IAeCommand
    {
        private readonly IPackFileService _packFileService;
        private readonly IPackFileContainerLoader _packFileContainerLoader;
        private readonly ISystemFolderContainerFactory _systemFolderContainerFactory;
        private readonly IStandardDialogs _standardDialogs;
        private readonly ApplicationSettingsService _applicationSettingsService;

        public ImportPackAsAsProjectCommand(
            IPackFileService packFileService,
            IPackFileContainerLoader packFileContainerLoader,
            ISystemFolderContainerFactory systemFolderContainerFactory,
            IStandardDialogs standardDialogs,
            ApplicationSettingsService applicationSettingsService)
        {
            _packFileService = packFileService;
            _packFileContainerLoader = packFileContainerLoader;
            _systemFolderContainerFactory = systemFolderContainerFactory;
            _standardDialogs = standardDialogs;
            _applicationSettingsService = applicationSettingsService;
        }

        public void Execute()
        {
            // Step 1: Select the .pack file
            using var packDialog = new OpenFileDialog
            {
                Filter = "Pack files (*.pack)|*.pack|All files (*.*)|*.*",
                Title = "Select pack file to convert"
            };

            if (packDialog.ShowDialog() != DialogResult.OK)
                return;

            // Step 2: Select destination folder
            using var folderDialog = new FolderBrowserDialog
            {
                Description = "Select destination folder for extracted files",
                UseDescriptionForTitle = true
            };

            if (folderDialog.ShowDialog() != DialogResult.OK)
                return;

            var destinationFolder = folderDialog.SelectedPath;

            // Step 3: Load the pack and extract all files to the destination
            var packContainer = _packFileContainerLoader.CreateFromPackFile(PackFileContainerType.Normal, packDialog.FileName, false);
            var allFiles = packContainer.GetAllFiles();

            foreach (var (relativePath, packFile) in allFiles)
            {
                var absolutePath = Path.Combine(destinationFolder, relativePath);
                var directory = Path.GetDirectoryName(absolutePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                var data = packFile.DataSource.ReadData();
                File.WriteAllBytes(absolutePath, data);
            }

            // Step 4: Open the extracted folder as a SystemFolderContainer
            var systemContainer = _systemFolderContainerFactory.Create(destinationFolder);
            if (systemContainer.PackFileSettings.GameVersion == null)
                systemContainer.PackFileSettings.GameVersion = _applicationSettingsService.CurrentSettings.CurrentGame;
            _packFileService.AddContainer(systemContainer);
            _packFileService.SetEditablePack(systemContainer);

        }
    }
}
