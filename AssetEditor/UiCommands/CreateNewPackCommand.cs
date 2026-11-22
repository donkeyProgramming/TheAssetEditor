using System;
using Shared.Core.Events;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.Services;
using Shared.Core.Settings;

namespace AssetEditor.UiCommands
{
    // Direct pack editing: a new, empty pack held in memory until its first save asks where to put it,
    // as AE did before folder projects (CreateGamePack in a0d05135)
    public class CreateNewPackCommand : IAeCommand
    {
        private readonly IPackFileService _packFileService;
        private readonly IStandardDialogs _standardDialogs;
        private readonly ApplicationSettingsService _settingsService;
        private readonly LocalizationManager _localizationManager;

        public CreateNewPackCommand(
            IPackFileService packFileService,
            IStandardDialogs standardDialogs,
            ApplicationSettingsService settingsService,
            LocalizationManager localizationManager)
        {
            _packFileService = packFileService;
            _standardDialogs = standardDialogs;
            _settingsService = settingsService;
            _localizationManager = localizationManager;
        }

        public void Execute()
        {
            if (_settingsService.CurrentSettings.EnableDirectPackEditing == false)
                return;

            var input = _standardDialogs.ShowTextInputDialog(_localizationManager.Get("CreateNewPackCommand.Title"));
            if (input.Result == false)
                return;

            var name = input.Text?.Trim() ?? string.Empty;
            if (name.EndsWith(".pack", StringComparison.OrdinalIgnoreCase))
                name = name[..^".pack".Length].Trim();

            if (string.IsNullOrWhiteSpace(name))
            {
                _standardDialogs.ShowDialogBox(string.Format(_localizationManager.Get("CreateNewPackCommand.InvalidName"), input.Text), _localizationManager.Get("CreateNewPackCommand.Title"));
                return;
            }

            var game = GameInformationDatabase.GetGameById(_settingsService.CurrentSettings.CurrentGame);
            _packFileService.CreateNewPackFileContainer(name, game.PackFileVersion, PackFileCAType.MOD, true);
        }
    }
}
