using Editors.KitbasherEditor.Core.MenuBarViews;
using GameWorld.Core.Components.Selection;
using GameWorld.Core.Services;
using Shared.Ui.Common.MenuSystem;
using System.Windows.Input;

namespace Editors.KitbasherEditor.UiCommands
{
    public class DuplicateObjectCommand : ITransientKitbasherUiCommand
    {
        public string ToolTip { get; set; } = "Duplicate selection";
        public ActionEnabledRule EnabledRule => ActionEnabledRule.ObjectOrFaceSelected;
        public Hotkey? HotKey { get; } = new Hotkey(Key.D, ModifierKeys.Control);

        private readonly SelectionManager _selectionManager;
        private readonly ObjectEditor _objectEditor;
        private readonly FaceEditor _faceEditor;

        public DuplicateObjectCommand(SelectionManager selectionManager, ObjectEditor objectEditor, FaceEditor faceEditor)
        {
            _selectionManager = selectionManager;
            _objectEditor = objectEditor;
            _faceEditor = faceEditor;
        }

        public void Execute()
        {
            if (_selectionManager.GetState() is ObjectSelectionState objectSelectionState)
                _objectEditor.DuplicateObject(objectSelectionState);
            if (_selectionManager.GetState() is FaceSelectionState faceSelectionState)
                _faceEditor.DuplicatedSelectedFacesToNewMesh(faceSelectionState, false);
        }
    }
}
