﻿using System.Linq;
using System.Windows.Forms;
using Shared.Core.Events;
using Shared.Core.PackFiles;
using Shared.Core.Services;
using Shared.Ui.Common;
namespace AssetEditor.UiCommands
{
    internal class OpenGamePackCommand : IUiCommand
    {
        private readonly PackFileService _packFileService;
        private readonly ApplicationSettingsService _applicationSettingsService;
        private readonly GameInformationFactory _gameInformationFactory;

        public OpenGamePackCommand(PackFileService packFileService, ApplicationSettingsService applicationSettingsService, GameInformationFactory gameInformationFactory)
        {
            _packFileService = packFileService;
            _applicationSettingsService = applicationSettingsService;
            _gameInformationFactory = gameInformationFactory;
        }

        public void Execute(GameTypeEnum game)
        {
            var settingsService = _applicationSettingsService;
            var settings = settingsService.CurrentSettings;
            var gamePath = settings.GameDirectories.FirstOrDefault(x => x.Game == game);

            if (gamePath == null || string.IsNullOrWhiteSpace(gamePath.Path))
            {
                System.Windows.MessageBox.Show("No path provided for game");
                return;
            }

            foreach (var packFile in _packFileService.Database.PackFiles)
            {
                if (packFile.SystemFilePath == gamePath.Path)
                {
                    MessageBox.Show($"Pack files for \"{_gameInformationFactory.GetGameById(game).DisplayName}\" are already loaded.", "Error");
                    return;
                }
            }

            using (new WaitCursor())
            {
                _packFileService.LoadAllCaFiles(gamePath.Path, _gameInformationFactory.GetGameById(game).DisplayName);
            }
        }
    }
}
