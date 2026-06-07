using CommonControls.BaseDialogs;
using Shared.Core.Events;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.PackFiles.Models.Containers;
using Shared.Core.Services;
using Shared.Core.Settings;

namespace AssetEditor.UiCommands
{
    public class CreateNewPackFileCommand : IAeCommand
    {
        private readonly IPackFileService _packFileService;
        private readonly ApplicationSettingsService _settingsService;
        private readonly IStandardDialogs _standardDialogs;
        private readonly ISystemFolderContainerFactory _systemFolderContainerFactory;

        public CreateNewPackFileCommand(
            IPackFileService packFileService,
            ApplicationSettingsService settingsService,
            IStandardDialogs standardDialogs,
            ISystemFolderContainerFactory systemFolderContainerFactory)
        {
            _packFileService = packFileService;
            _settingsService = settingsService;
            _standardDialogs = standardDialogs;
            _systemFolderContainerFactory = systemFolderContainerFactory;
        }

        public void Execute()
        {
            var window = new NewPackFileWindow();
            if (window.ShowDialog() != true)
                return;

            if (window.SelectedType == NewPackFileType.GamePack)
                CreateGamePack(window.PackName);
            else
                CreateFolderPack(window.SelectedFolderPath);
        }

        private void CreateGamePack(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                _standardDialogs.ShowDialogBox($"'{name}' is not a valid pack name", "Error");
                return;
            }

            var currentGame = _settingsService.CurrentSettings.CurrentGame;
            var pfsVersion = GameInformationDatabase.Games[currentGame].PackFileVersion;

            var newPackFile = _packFileService.CreateNewPackFileContainer(name.Trim(), pfsVersion, PackFileCAType.MOD);
            _packFileService.SetEditablePack(newPackFile);
        }

        private void CreateFolderPack(string? folderPath)
        {
            if (string.IsNullOrWhiteSpace(folderPath))
            {
                _standardDialogs.ShowDialogBox("No folder was selected", "Error");
                return;
            }

            var folderPack = _systemFolderContainerFactory.Create(folderPath);
            _packFileService.AddContainer(folderPack);
            _packFileService.SetEditablePack(folderPack);
        }
    }
}
