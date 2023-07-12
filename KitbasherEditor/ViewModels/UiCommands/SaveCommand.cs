using CommonControls.Common.MenuSystem;
using KitbasherEditor.ViewModels.MenuBarViews;
using View3D.Services;

namespace KitbasherEditor.ViewModels.UiCommands
{
    public class SaveCommand : IKitbasherUiCommand
    {
        public string ToolTip { get; set; } = "Save";
        public ActionEnabledRule EnabledRule => ActionEnabledRule.Always;
        public Hotkey HotKey { get; } = null;

        private readonly SceneSaverService _sceneSaverService;

        public SaveCommand(SceneSaverService sceneSaverService)
        {
            _sceneSaverService = sceneSaverService;
        }

        public void Execute() => _sceneSaverService.Save();
    }
}
