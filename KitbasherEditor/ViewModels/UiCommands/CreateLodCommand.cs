using CommonControls.Events.UiCommands;
using View3D.Components.Component;
using View3D.SceneNodes;
using View3D.Services;

namespace KitbasherEditor.ViewModels.UiCommands
{
    public class CreateLodCommand : IExecutableUiCommand
    {
        ObjectEditor _objectEditor;
        SceneManager _sceneManager;

        public CreateLodCommand(ObjectEditor objectEditor, SceneManager sceneManager)
        {
            _objectEditor = objectEditor;
            _sceneManager = sceneManager;
        }

        public void Execute()
        {
            var rootNode = _sceneManager.GetNodeByName<MainEditableNode>(SpecialNodes.EditableModel);
            var lodGenerationService = new LodGenerationService(_objectEditor);
            lodGenerationService.CreateLodsForRootNode(rootNode);
        }
    }
}
