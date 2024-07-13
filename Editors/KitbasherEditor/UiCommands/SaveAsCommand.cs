using Editors.KitbasherEditor.ViewModels.SaveDialog;
using GameWorld.Core.Components;
using GameWorld.Core.Services.SceneSaving;
using KitbasherEditor.ViewModels.MenuBarViews;
using Shared.Core.Misc;
using Shared.Ui.Common.MenuSystem;

namespace KitbasherEditor.ViewModels.UiCommands
{
    public class SaveAsCommand :  SaveCommandBase, IKitbasherUiCommand
    {
        public string ToolTip { get; set; } = "SaveAs";
        public ActionEnabledRule EnabledRule => ActionEnabledRule.Always;
        public Hotkey HotKey { get; } = null;

        public SaveAsCommand(GeometrySaveSettings settings, SceneManager sceneManager, SaveService saveService, IAbstractFormFactory<SaveDialogWindow> saveWindowFactory)
            : base(settings, sceneManager, saveService, saveWindowFactory)
        {
        }

        public void Execute() => Save(true);
    }
}
