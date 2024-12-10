using System.Windows.Input;
using Editors.KitbasherEditor.Core.MenuBarViews;
using GameWorld.Core.Components.Selection;
using GameWorld.Core.Services;
using Shared.Ui.Common.MenuSystem;

namespace Editors.KitbasherEditor.UiCommands
{
    public class DivideSubMeshCommand : ITransientKitbasherUiCommand
    {
        public string ToolTip { get; set; } = "Split mesh into logical parts. Hold left Alt to not combine logical sub-parts";
        public ActionEnabledRule EnabledRule => ActionEnabledRule.OneObjectSelected;
        public Hotkey? HotKey { get; } = new Hotkey(Key.Add, ModifierKeys.None);

        private readonly SelectionManager _selectionManager;
        private readonly ObjectEditor _objectEditor;
        private readonly FaceEditor _faceEditor;
        private readonly WindowKeyboard _keyboard;

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
