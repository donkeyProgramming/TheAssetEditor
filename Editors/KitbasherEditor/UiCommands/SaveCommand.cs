using GameWorld.Core.Components;
using GameWorld.Core.SceneNodes;
using GameWorld.Core.Services.SceneSaving;
using KitbasherEditor.ViewModels.MenuBarViews;
using KitbasherEditor.ViewModels.SaveDialog;
using KitbasherEditor.Views.EditorViews;
using Shared.Ui.BaseDialogs.WindowHandling;
using Shared.Ui.Common.MenuSystem;

namespace KitbasherEditor.ViewModels.UiCommands
{
    public class SaveCommandBase
    {
        private readonly IWindowFactory _windowFactory;
        private readonly SceneManager _sceneManager;
        private readonly SaveService _saveService;
        private SaveSettings _settings;

        public SaveCommandBase(IWindowFactory windowFactory, SaveSettings settings, SceneManager sceneManager, SaveService saveService)
        {
            _windowFactory = windowFactory;
            _settings = settings;
            _sceneManager = sceneManager;
            _saveService = saveService;
        }

        protected void Save(bool forceShowDialog)
        {
            var saveScene = true;
            if (_settings.IsUserInitialized == false || forceShowDialog)
            {
                var window = _windowFactory.Create<SaveDialogViewModel, SaveDialogView>("Save", 630, 350);
                window.TypedContext.Initialize(_settings);
                saveScene = window.ShowWindow(true) == true;
                if (saveScene)
                    window.TypedContext.UpdateSettings(ref _settings);
            }

            if (saveScene)
            {
                var mainNode = _sceneManager.GetNodeByName<MainEditableNode>(SpecialNodes.EditableModel);
                _saveService.Save(mainNode, _settings);
            }
        }
    }

    public class SaveCommand : SaveCommandBase, IKitbasherUiCommand
    {
        public string ToolTip { get; set; } = "Save";
        public ActionEnabledRule EnabledRule => ActionEnabledRule.Always;
        public Hotkey HotKey { get; } = null;

        public SaveCommand(IWindowFactory windowFactory, SaveSettings settings, SceneManager sceneManager, SaveService saveService)
            :base(windowFactory, settings, sceneManager, saveService)
        {
        }

        public void Execute() => Save(false);
    }
}
