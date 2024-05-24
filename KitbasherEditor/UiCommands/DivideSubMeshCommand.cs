using KitbasherEditor.ViewModels.MenuBarViews;
using Shared.Ui.Common.MenuSystem;
using System.Windows.Input;
using View3D.Components.Component.Selection;
using View3D.Services;

namespace KitbasherEditor.ViewModels.UiCommands
{
    public class DivideSubMeshCommand : IKitbasherUiCommand
    {
        public string ToolTip { get; set; } = "Split mesh into logical parts. Hold leftAlt to not combine logical sub-parts";
        public ActionEnabledRule EnabledRule => ActionEnabledRule.OneObjectSelected;
        public Hotkey HotKey { get; } = new Hotkey(Key.Add, ModifierKeys.None);

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
