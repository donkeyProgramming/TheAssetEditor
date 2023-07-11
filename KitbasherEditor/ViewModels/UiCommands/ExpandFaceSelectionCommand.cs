using CommonControls.Common.MenuSystem;
using CommonControls.Events.UiCommands;
using System.Windows.Input;
using View3D.Components.Component.Selection;
using View3D.Services;

namespace KitbasherEditor.ViewModels.UiCommands
{
    public class ExpandFaceSelectionCommand : IExecutableUiCommand
    {
        FaceEditor _faceEditor;
        SelectionManager _selectionManager;
        WindowKeyboard _keyboard;

        public ExpandFaceSelectionCommand(FaceEditor faceEditor, SelectionManager selectionManager, WindowKeyboard keyboard)
        {
            _faceEditor = faceEditor;
            _selectionManager = selectionManager;
            _keyboard = keyboard;
        }

        public void Execute()
        {
            _faceEditor.GrowSelection(_selectionManager.GetState() as FaceSelectionState, !_keyboard.IsKeyDown(Key.LeftAlt));
        }
    }
}
