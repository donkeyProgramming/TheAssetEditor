using CommonControls.BaseDialogs;
using Shared.Core.Events;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Utility;
using Shared.Core.Services;
using Shared.Core.Settings;

namespace AssetEditor.UiCommands
{
    public class CreateNewProjectCommand : IAeCommand
    {
        private readonly IPackFileService _packFileService;
        private readonly IStandardDialogs _standardDialogs;
        private readonly ISystemFolderContainerFactory _systemFolderContainerFactory;
        private readonly ApplicationSettingsService _applicationSettingsService;

        public CreateNewProjectCommand(
            IPackFileService packFileService,
            IStandardDialogs standardDialogs,
            ISystemFolderContainerFactory systemFolderContainerFactory,
            ApplicationSettingsService applicationSettingsService)
        {
            _packFileService = packFileService;
            _standardDialogs = standardDialogs;
            _systemFolderContainerFactory = systemFolderContainerFactory;
            _applicationSettingsService = applicationSettingsService;
        }

        public void Execute()
        {
            var window = new NewPackFileWindow();
            if (window.ShowDialog() != true)
                return;

            if (string.IsNullOrWhiteSpace(window.SelectedFolderPath))
            {
                _standardDialogs.ShowDialogBox("No folder was selected", "Error");
                return;
            }

            var folderPack = _systemFolderContainerFactory.Create(window.SelectedFolderPath);
            if (folderPack.PackFileSettings.GameVersion == null)
                folderPack.PackFileSettings.GameVersion = _applicationSettingsService.CurrentSettings.CurrentGame;
            _packFileService.AddContainer(folderPack);
            _packFileService.SetEditablePack(folderPack);
        }
    }
}
