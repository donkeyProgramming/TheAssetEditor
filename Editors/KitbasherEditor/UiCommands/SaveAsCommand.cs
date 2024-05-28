using KitbasherEditor.ViewModels.MenuBarViews;
using Shared.Ui.BaseDialogs.WindowHandling;
using Shared.Ui.Common.MenuSystem;
using View3D.Components.Component;
using View3D.Services.SceneSaving;

namespace KitbasherEditor.ViewModels.UiCommands
{
    public class SaveAsCommand :  SaveCommandBase, IKitbasherUiCommand
    {
        public string ToolTip { get; set; } = "SaveAs";
        public ActionEnabledRule EnabledRule => ActionEnabledRule.Always;
        public Hotkey HotKey { get; } = null;

        public SaveAsCommand(IWindowFactory windowFactory, SaveSettings settings, SceneManager sceneManager, SaveService saveService)
            : base(windowFactory, settings, sceneManager, saveService)
        {
        }

        public void Execute() => Save(true);
    }
}
