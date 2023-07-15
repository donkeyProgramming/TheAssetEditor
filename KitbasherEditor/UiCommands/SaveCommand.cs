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
        private readonly KitbasherRootScene _kitbasherRootScene;

        public SaveCommand(SceneSaverService sceneSaverService, KitbasherRootScene kitbasherRootScene)
        {
            _sceneSaverService = sceneSaverService;
            _kitbasherRootScene = kitbasherRootScene;
        }

        public void Execute() => _sceneSaverService.Save(_kitbasherRootScene.SelectedOutputFormat);
    }
}
