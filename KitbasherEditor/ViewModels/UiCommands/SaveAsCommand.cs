using CommonControls.Common.MenuSystem;
using CommonControls.Events.UiCommands;
using KitbasherEditor.ViewModels.MenuBarViews;
using View3D.Services;

namespace KitbasherEditor.ViewModels.UiCommands
{
    public class SaveAsCommand : IKitbasherUiCommand
    {
        public string ToolTip { get; set; } = "SaveAs";
        public ActionEnabledRule EnabledRule => ActionEnabledRule.Always;
        public Hotkey HotKey { get; } = null;

        private readonly SceneSaverService _sceneSaverService;

        public SaveAsCommand(SceneSaverService sceneSaverService)
        {
            _sceneSaverService = sceneSaverService;
        }

        public void Execute() => _sceneSaverService.SaveAs();
    }
}
