using CommonControls.Events.UiCommands;
using System.Linq;
using View3D.Components.Component.Selection;
using View3D.SceneNodes;
using View3D.Services;

namespace KitbasherEditor.ViewModels.UiCommands
{
    public class ReduceMeshCommand : IExecutableUiCommand
    {
        SelectionManager _selectionManager;
        ObjectEditor _objectEditor;

        public ReduceMeshCommand(SelectionManager selectionManager, ObjectEditor objectEditor)
        {
            _selectionManager = selectionManager;
            _objectEditor = objectEditor;
        }

        public void Execute()
        {
            var selectedObjects = _selectionManager.GetState() as ObjectSelectionState;
            if (selectedObjects == null || selectedObjects.SelectionCount() == 0)
                return;

            var meshNodes = selectedObjects.SelectedObjects()
                .Where(x => x is Rmv2MeshNode)
                .Select(x => x as Rmv2MeshNode)
                .ToList();

            _objectEditor.ReduceMesh(meshNodes, 0.9f, true);
        }
    }
}
