using Editors.KitbasherEditor.ViewModels.SaveDialog;
using GameWorld.Core.Components;
using GameWorld.Core.SceneNodes;
using GameWorld.Core.Services.SceneSaving;
using KitbasherEditor.ViewModels.MenuBarViews;
using Shared.Core.Misc;
using Shared.Ui.Common.MenuSystem;

namespace KitbasherEditor.ViewModels.UiCommands
{
    public class SaveCommandBase
    {
        private readonly SceneManager _sceneManager;
        private readonly SaveService _saveService;
        private readonly IAbstractFormFactory<SaveDialogWindow> _saveWindowFactory;
        private readonly GeometrySaveSettings _settings;

        public SaveCommandBase(GeometrySaveSettings settings, SceneManager sceneManager, SaveService saveService, IAbstractFormFactory<SaveDialogWindow> saveWindowFactory) 
        {
            _settings = settings;
            _sceneManager = sceneManager;
            _saveService = saveService;
            _saveWindowFactory = saveWindowFactory;
        }

        protected void Save(bool forceShowDialog)
        {
            if (_settings.IsUserInitialized == false || forceShowDialog)
            {
                var window = _saveWindowFactory.Create();
                window.Initialize(_settings);
                var saveScene = window.ShowDialog();
                if (saveScene != true)
                    return;
            }
  
            var mainNode = _sceneManager.GetNodeByName<MainEditableNode>(SpecialNodes.EditableModel);
            _saveService.Save(mainNode, _settings);
        }
    }

    public class SaveCommand : SaveCommandBase, IKitbasherUiCommand
    {
        public string ToolTip { get; set; } = "Save";
        public ActionEnabledRule EnabledRule => ActionEnabledRule.Always;
        public Hotkey HotKey { get; } = null;

        public SaveCommand( GeometrySaveSettings settings, SceneManager sceneManager, SaveService saveService, IAbstractFormFactory<SaveDialogWindow> saveWindowFactory)
            :base(settings, sceneManager, saveService, saveWindowFactory)
        {
        }

        public void Execute() => Save(false);
    }
}
