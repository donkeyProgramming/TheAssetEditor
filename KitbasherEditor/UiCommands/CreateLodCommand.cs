using CommonControls.Common.MenuSystem;
using KitbasherEditor.ViewModels.MenuBarViews;
using View3D.Components.Component;
using View3D.SceneNodes;
using View3D.Services;

namespace KitbasherEditor.ViewModels.UiCommands
{
    public class CreateLodCommand : IKitbasherUiCommand
    {
        public string ToolTip { get; set; } = "Auto generate lods for models";
        public ActionEnabledRule EnabledRule => ActionEnabledRule.Always;
        public Hotkey HotKey { get; } = null;

        ObjectEditor _objectEditor;
        SceneManager _sceneManager;

        public CreateLodCommand(ObjectEditor objectEditor, SceneManager sceneManager)
        {
            _objectEditor = objectEditor;
            _sceneManager = sceneManager;
        }

        public void Execute()
        {
            // remove
            //var rootNode = _sceneManager.GetNodeByName<MainEditableNode>(SpecialNodes.EditableModel);
            //var lodGenerationService = new LodGenerationService(_objectEditor);
            //lodGenerationService.CreateLodsForRootNode(rootNode);
        }
    }
}
