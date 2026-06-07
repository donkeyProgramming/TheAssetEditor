using System.IO;
using System.Windows.Forms;
using CommonControls.BaseDialogs;
using Shared.Core.Events;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models.Containers;
using Shared.Core.PackFiles.Utility;
using Shared.Core.Services;

namespace AssetEditor.UiCommands
{
    public class OpenPackFileCommand : IAeCommand
    {
        private readonly IPackFileService _packFileService;
        private readonly IPackFileContainerLoader _packFileContainerLoader;
        private readonly ISystemFolderContainerFactory _systemFolderContainerFactory;
        private readonly IStandardDialogs _standardDialogs;

        public OpenPackFileCommand(
            IPackFileService packFileService,
            IPackFileContainerLoader packFileContainerLoader,
            ISystemFolderContainerFactory systemFolderContainerFactory,
            IStandardDialogs standardDialogs)
        {
            _packFileService = packFileService;
            _packFileContainerLoader = packFileContainerLoader;
            _systemFolderContainerFactory = systemFolderContainerFactory;
            _standardDialogs = standardDialogs;
        }

        public void Execute()
        {
            var optionsWindow = new OpenPackFileOptionsWindow();
            if (optionsWindow.ShowDialog() != true)
                return;

            switch (optionsWindow.SelectedOption)
            {
                case OpenPackFileOption.OpenPackFile:
                    OpenPackFile();
                    break;
                case OpenPackFileOption.OpenSystemFolder:
                    OpenSystemFolder();
                    break;
                case OpenPackFileOption.ConvertToSystemFolder:
                    ConvertPackToSystemFolder();
                    break;
            }
        }

        private void OpenPackFile()
        {
            using var dialog = new OpenFileDialog
            {
                Filter = "Pack files (*.pack)|*.pack|All files (*.*)|*.*"
            };

            if (dialog.ShowDialog() != DialogResult.OK)
                return;

            var container = _packFileContainerLoader.CreateFromPackFile(PackFileContainerType.Normal, dialog.FileName, false);
            _packFileService.AddContainer(container, true);
        }

        private void OpenSystemFolder()
        {
            using var dialog = new FolderBrowserDialog
            {
                Description = "Select folder to open as a pack",
                UseDescriptionForTitle = true
            };

            if (dialog.ShowDialog() != DialogResult.OK)
                return;

            var container = _systemFolderContainerFactory.Create(dialog.SelectedPath);
            _packFileService.AddContainer(container);
            _packFileService.SetEditablePack(container);
        }

        private void ConvertPackToSystemFolder()
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
            _packFileService.AddContainer(systemContainer);
            _packFileService.SetEditablePack(systemContainer);
        }
    }
}
