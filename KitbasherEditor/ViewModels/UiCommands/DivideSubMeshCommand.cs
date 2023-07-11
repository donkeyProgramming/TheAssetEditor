using CommonControls.Common.MenuSystem;
using CommonControls.Events.UiCommands;
using System.Windows.Input;
using View3D.Components.Component.Selection;
using View3D.Services;

namespace KitbasherEditor.ViewModels.UiCommands
{
    public class DivideSubMeshCommand : IExecutableUiCommand
    {
        SelectionManager _selectionManager;
        ObjectEditor _objectEditor;
        FaceEditor _faceEditor;
        WindowKeyboard _keyboard;

        public DivideSubMeshCommand(SelectionManager selectionManager, ObjectEditor objectEditor, FaceEditor faceEditor, WindowKeyboard keyboard)
        {
            _selectionManager = selectionManager;
            _objectEditor = objectEditor;
            _faceEditor = faceEditor;
            _keyboard = keyboard;
        }

        public void Execute()
        {
            if (_selectionManager.GetState() is ObjectSelectionState objectSelectionState)
                _objectEditor.DivideIntoSubmeshes(objectSelectionState, !_keyboard.IsKeyDown(Key.LeftAlt));
            if (_selectionManager.GetState() is FaceSelectionState faceSelectionState)
                _faceEditor.DuplicatedSelectedFacesToNewMesh(faceSelectionState, true);
        }
    }
}
