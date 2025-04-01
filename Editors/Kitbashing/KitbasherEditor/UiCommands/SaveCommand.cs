using System.Runtime;
using Editors.KitbasherEditor.Core.MenuBarViews;
using Editors.KitbasherEditor.ViewModels.SaveDialog;
using GameWorld.Core.Components;
using GameWorld.Core.SceneNodes;
using GameWorld.Core.Services.SceneSaving;
using Shared.Core.Misc;
using Shared.Ui.Common.MenuSystem;

namespace Editors.KitbasherEditor.UiCommands
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

        protected SaveResult? Save(bool forceShowDialog)
        {
            if (_settings.IsUserInitialized == false || forceShowDialog)
            {
                var window = _saveWindowFactory.Create();
                window.Initialize(_settings);
                var saveScene = window.ShowDialog();
                if (saveScene != true)
                    return null;
            }


            var mainNode = _sceneManager.GetNodeByName<MainEditableNode>(SpecialNodes.EditableModel);
            _settings.AttachmentPoints = mainNode.AttachmentPoints; // Bit of a hack, clean up at some point
            var res = _saveService.Save(mainNode, _settings);
            return res;
        }
    }

    public class SaveCommand : SaveCommandBase, ITransientKitbasherUiCommand
    {
        public string ToolTip { get; set; } = "Save";
        public ActionEnabledRule EnabledRule => ActionEnabledRule.Always;
        public Hotkey? HotKey { get; } = null;

        public SaveCommand(GeometrySaveSettings settings, SceneManager sceneManager, SaveService saveService, IAbstractFormFactory<SaveDialogWindow> saveWindowFactory)
            : base(settings, sceneManager, saveService, saveWindowFactory)
        {
        }

        public void Execute() => Save(false);
        public SaveResult? ExecuteWithResult() => Save(false);
    }

    public class SaveAsCommand : SaveCommandBase, ITransientKitbasherUiCommand
    {
        public string ToolTip { get; set; } = "SaveAs";
        public ActionEnabledRule EnabledRule => ActionEnabledRule.Always;
        public Hotkey? HotKey { get; } = null;

        public SaveAsCommand(GeometrySaveSettings settings, SceneManager sceneManager, SaveService saveService, IAbstractFormFactory<SaveDialogWindow> saveWindowFactory)
            : base(settings, sceneManager, saveService, saveWindowFactory)
        {
        }

        public void Execute() => Save(true);
    }
}
