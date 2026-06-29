using System.Windows.Forms;
using Shared.Core.Events;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Utility;
using Shared.Core.Services;
using Shared.Core.Settings;

namespace AssetEditor.UiCommands
{
    public class OpenProjectCommand : IAeCommand
    {
        private readonly IPackFileService _packFileService;
        private readonly ISystemFolderContainerFactory _systemFolderContainerFactory;
        private readonly IStandardDialogs _standardDialogs;
        private readonly ApplicationSettingsService _applicationSettingsService;

        public OpenProjectCommand(
            IPackFileService packFileService,
            ISystemFolderContainerFactory systemFolderContainerFactory,
            IStandardDialogs standardDialogs,
            ApplicationSettingsService applicationSettingsService)
        {
            _packFileService = packFileService;
            _systemFolderContainerFactory = systemFolderContainerFactory;
            _standardDialogs = standardDialogs;
            _applicationSettingsService = applicationSettingsService;
        }

        public void Execute()
        {
            using var dialog = new FolderBrowserDialog
            {
                Description = "Select folder to open as a pack",
                UseDescriptionForTitle = true
            };

            if (dialog.ShowDialog() != DialogResult.OK)
                return;

            var container = _systemFolderContainerFactory.Create(dialog.SelectedPath);
            if (container.PackFileSettings.GameVersion == null)
                container.PackFileSettings.GameVersion = _applicationSettingsService.CurrentSettings.CurrentGame;
            _packFileService.AddContainer(container);
            _packFileService.SetEditablePack(container);
        }
    }
}
