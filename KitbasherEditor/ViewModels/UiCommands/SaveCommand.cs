using CommonControls.Events.UiCommands;
using View3D.Services;

namespace KitbasherEditor.ViewModels.UiCommands
{
    public class SaveCommand : IExecutableUiCommand
    {
        private readonly SceneSaverService _sceneSaverService;

        public SaveCommand(SceneSaverService sceneSaverService)
        {
            _sceneSaverService = sceneSaverService;
        }

        public void Execute() => _sceneSaverService.Save();
    }
}
