using CommonControls.Events.UiCommands;
using View3D.Services;

namespace KitbasherEditor.ViewModels.UiCommands
{
    public class SaveAsCommand : IExecutableUiCommand
    {
        private readonly SceneSaverService _sceneSaverService;

        public SaveAsCommand(SceneSaverService sceneSaverService)
        {
            _sceneSaverService = sceneSaverService;
        }

        public void Execute() => _sceneSaverService.SaveAs();
    }
}
