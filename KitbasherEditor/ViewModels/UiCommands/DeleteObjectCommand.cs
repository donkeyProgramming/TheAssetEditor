using CommonControls.Events.UiCommands;
using View3D.Components.Component.Selection;
using View3D.Services;

namespace KitbasherEditor.ViewModels.UiCommands
{
    public class DeleteObjectCommand : IExecutableUiCommand
    {
        SelectionManager _selectionManager;
        ObjectEditor _objectEditor;
        FaceEditor _faceEditor;

        public DeleteObjectCommand(SelectionManager selectionManager, ObjectEditor objectEditor, FaceEditor faceEditor)
        {
            _selectionManager = selectionManager;
            _objectEditor = objectEditor;
            _faceEditor = faceEditor;
        }

        public void Execute()
        {
            if (_selectionManager.GetState() is ObjectSelectionState objectSelectionState)
                _objectEditor.DeleteObject(objectSelectionState);
            else if (_selectionManager.GetState() is FaceSelectionState faceSelection)
                _faceEditor.DeleteFaces(faceSelection);
        }
    }
}
