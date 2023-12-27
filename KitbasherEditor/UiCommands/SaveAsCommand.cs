using CommonControls.Common.MenuSystem;
using KitbasherEditor.ViewModels.MenuBarViews;
using View3D.Services.SceneSaving.Geometry;

namespace KitbasherEditor.ViewModels.UiCommands
{
    public class SaveAsCommand : IKitbasherUiCommand
    {
        public string ToolTip { get; set; } = "SaveAs";
        public ActionEnabledRule EnabledRule => ActionEnabledRule.Always;
        public Hotkey HotKey { get; } = null;

        private readonly SceneSaverService _sceneSaverService;
        private readonly KitbasherRootScene _kitbasherRootScene;

        public SaveAsCommand(SceneSaverService sceneSaverService, KitbasherRootScene kitbasherRootScene)
        {
            _sceneSaverService = sceneSaverService;
            _kitbasherRootScene = kitbasherRootScene;
        }

        public void Execute()
        {
            throw new System.Exception();
            //_sceneSaverService.SaveAs(_kitbasherRootScene.SelectedOutputFormat);
        }
    }
}
