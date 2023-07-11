using CommonControls.Events.UiCommands;
using View3D.Components.Component.Selection;
using View3D.Services;

namespace KitbasherEditor.ViewModels.UiCommands
{
    public class ConvertFaceToObjectCommand : IExecutableUiCommand
    {
        FaceEditor _faceEditor;
        SelectionManager _selectionManager;

        public ConvertFaceToObjectCommand(FaceEditor faceEditor, SelectionManager selectionManager)
        {
            _faceEditor = faceEditor;
            _selectionManager = selectionManager;
        }

        public void Execute()
        {
            _faceEditor.ConvertSelectionToVertex(_selectionManager.GetState() as FaceSelectionState);
        }
    }
}
