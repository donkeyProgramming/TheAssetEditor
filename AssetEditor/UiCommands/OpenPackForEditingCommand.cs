using System;
using System.IO;
using Shared.Core.Events;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.PackFiles.Utility;
using Shared.Core.Services;
using Shared.Core.Settings;

namespace AssetEditor.UiCommands
{
    // Direct pack editing: opens a .pack to be edited in place, as AE did before folder projects
    // (OpenPackFile in 32055ecf^). The pack is backed up once it has loaded, unless backups are off.
    public class OpenPackForEditingCommand : IAeCommand
    {
        private readonly IPackFileService _packFileService;
        private readonly IPackFileContainerLoader _packFileContainerLoader;
        private readonly PackBackupService _packBackupService;
        private readonly IStandardDialogs _standardDialogs;
        private readonly ApplicationSettingsService _settingsService;
        private readonly LocalizationManager _localizationManager;

        public OpenPackForEditingCommand(
            IPackFileService packFileService,
            IPackFileContainerLoader packFileContainerLoader,
            PackBackupService packBackupService,
            IStandardDialogs standardDialogs,
            ApplicationSettingsService settingsService,
            LocalizationManager localizationManager)
        {
            _packFileService = packFileService;
            _packFileContainerLoader = packFileContainerLoader;
            _packBackupService = packBackupService;
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

            // Checked before loading so reopening an open pack can't replace its backup with what AE has saved since
            if (_packFileService.IsPackFileLoaded(packPath))
            {
                _standardDialogs.ShowDialogBox(string.Format(_localizationManager.Get("OpenPackForEditingCommand.AlreadyOpen"), packPath), _localizationManager.Get("OpenPackForEditingCommand.Title"));
                return;
            }

            var container = _packFileContainerLoader.CreateFromPackFile(PackFileContainerType.Normal, packPath, false);

            // The loader opened it read-only and said why (AE can't write it back safely); it can still be browsed
            if (container.IsReadOnly)
            {
                _packFileService.AddContainer(container, false);
                return;
            }

            if (TryBackup(packPath) == false)
                return;

            _packFileService.AddContainer(container, true);
        }

        private bool TryBackup(string packPath)
        {
            try
            {
                _packBackupService.BackupOnOpen(packPath);
                return true;
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                var message = string.Format(_localizationManager.Get("OpenPackForEditingCommand.BackupFailed"), Path.GetFileName(packPath), e.Message);
                return _standardDialogs.ShowYesNoBox(message, _localizationManager.Get("OpenPackForEditingCommand.BackupFailedTitle")) == ShowMessageBoxResult.OK;
            }
        }
    }
}
