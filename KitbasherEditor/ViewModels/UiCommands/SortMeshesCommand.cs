using CommonControls.Events.UiCommands;
using System.Linq;
using View3D.Components.Component;
using View3D.SceneNodes;
using View3D.Services;

namespace KitbasherEditor.ViewModels.MenuBarViews
{
    public class SortMeshesCommand : IExecutableUiCommand
    {
        SceneManager _sceneManager;
        private readonly ObjectEditor _objectEditor;

        public SortMeshesCommand(SceneManager sceneManager, ObjectEditor objectEditor)
        {
            _sceneManager = sceneManager;
            _objectEditor = objectEditor;
        }

        public void Execute()
        {
            var rootNode = _sceneManager.GetNodeByName<MainEditableNode>(SpecialNodes.EditableModel);
            var lod0 = rootNode.GetLodNodes().FirstOrDefault();
            if (lod0 != null)
                _objectEditor.SortMeshes(lod0);
        }
    }
}
