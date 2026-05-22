using System.Linq;
using System.Windows.Forms;
using Shared.Core.Events;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Utility;
using Shared.Core.Settings;
using Shared.Ui.Common;
namespace AssetEditor.UiCommands
{
    internal class OpenGamePackCommand : IAeCommand
    {
        private readonly IPackFileService _packFileService;
        private readonly IPackFileContainerLoader _packFileContainerLoader;
        private readonly ApplicationSettingsService _applicationSettingsService;
        private GameTypeEnum _game;

        public OpenGamePackCommand(IPackFileService packFileService, IPackFileContainerLoader packFileContainerLoader, ApplicationSettingsService applicationSettingsService)
        {
            _packFileService = packFileService;
            _packFileContainerLoader = packFileContainerLoader;
            _applicationSettingsService = applicationSettingsService;
        }

        public void Configure(GameTypeEnum game)
        {
            _game = game;
        }

        public void Execute()
        {
            var settingsService = _applicationSettingsService;
            var settings = settingsService.CurrentSettings;
            var gamePath = settings.GameDirectories.FirstOrDefault(x => x.Game == _game);

            if (gamePath == null || string.IsNullOrWhiteSpace(gamePath.Path))
            {
                System.Windows.MessageBox.Show("No path provided for game");
                return;
            }

            var packFileContainer = _packFileService.GetAllPackfileContainers();
            foreach (var packFile in packFileContainer)
            {
                if (packFile.SystemFilePath == gamePath.Path)
                {
                    MessageBox.Show($"Pack files for \"{GameInformationDatabase.GetGameById(_game).DisplayName}\" are already loaded.", "Error");
                    return;
                }
            }

            using (new WaitCursor())
            {
                var res = _packFileContainerLoader.LoadAllCaFiles(_game);
                _packFileService.AddContainer(res);
            }
        }
    }
}
