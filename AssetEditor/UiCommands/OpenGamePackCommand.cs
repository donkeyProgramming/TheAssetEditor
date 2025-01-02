using System.Linq;
using System.Windows.Forms;
using Shared.Core.Events;
using Shared.Core.PackFiles;
using Shared.Core.Settings;
using Shared.Ui.Common;
namespace AssetEditor.UiCommands
{
    internal class OpenGamePackCommand : IUiCommand
    {
        private readonly IPackFileService _packFileService;
        private readonly IPackFileContainerLoader _packFileContainerLoader;
        private readonly ApplicationSettingsService _applicationSettingsService;

        public OpenGamePackCommand(IPackFileService packFileService, IPackFileContainerLoader packFileContainerLoader, ApplicationSettingsService applicationSettingsService)
        {
            _packFileService = packFileService;
            _packFileContainerLoader = packFileContainerLoader;
            _applicationSettingsService = applicationSettingsService;
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

            var packFileContainer = _packFileService.GetAllPackfileContainers();
            foreach (var packFile in packFileContainer)
            {
                if (packFile.SystemFilePath == gamePath.Path)
                {
                    MessageBox.Show($"Pack files for \"{GameInformationDatabase.GetGameById(game).DisplayName}\" are already loaded.", "Error");
                    return;
                }
            }

            using (new WaitCursor())
            {
                var res = _packFileContainerLoader.LoadAllCaFiles(game);
                _packFileService.AddContainer(res);
            }
        }
    }
}
