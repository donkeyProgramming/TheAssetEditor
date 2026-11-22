using Shared.Core.Events;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.PackFiles.Utility;
using Shared.Core.Services;
using Shared.Core.Settings;

namespace AssetEditor.UiCommands
{
    // Direct pack editing: opens a .pack to be edited in place, as AE did before folder projects
    // (OpenPackFile in 32055ecf^). Its backup is kept by the first save, not here.
    public class OpenPackForEditingCommand : IAeCommand
    {
        private readonly IPackFileService _packFileService;
        private readonly IPackFileContainerLoader _packFileContainerLoader;
        private readonly IStandardDialogs _standardDialogs;
        private readonly ApplicationSettingsService _settingsService;
        private readonly LocalizationManager _localizationManager;

        public OpenPackForEditingCommand(
            IPackFileService packFileService,
            IPackFileContainerLoader packFileContainerLoader,
            IStandardDialogs standardDialogs,
            ApplicationSettingsService settingsService,
            LocalizationManager localizationManager)
        {
            _packFileService = packFileService;
            _packFileContainerLoader = packFileContainerLoader;
            _standardDialogs = standardDialogs;
            _settingsService = settingsService;
            _localizationManager = localizationManager;
        }

        public void Execute()
        {
            var dialog = _standardDialogs.ShowSystemOpenFileDialog(filter: "Pack files (*.pack)|*.pack|All files (*.*)|*.*");
            if (dialog.Result == false || dialog.FilePaths.Count == 0)
                return;

            Open(dialog.FilePaths[0]);
        }

        public void Open(string packPath)
        {
            if (_settingsService.CurrentSettings.EnableDirectPackEditing == false)
                return;

            // Checked before loading, so an open pack isn't read a second time just to be refused
            if (_packFileService.IsPackFileLoaded(packPath))
            {
                _standardDialogs.ShowDialogBox(string.Format(_localizationManager.Get("OpenPackForEditingCommand.AlreadyOpen"), packPath), _localizationManager.Get("OpenPackForEditingCommand.Title"));
                return;
            }

            var container = _packFileContainerLoader.CreateFromPackFile(PackFileContainerType.Normal, packPath, false);

            // If the loader opened it read-only, it said why (AE can't write it back safely); it can still be browsed
            _packFileService.AddContainer(container, container.IsReadOnly == false);
        }
    }
}
