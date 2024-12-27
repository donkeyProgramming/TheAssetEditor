using Editors.KitbasherEditor.Core.MenuBarViews;
using GameWorld.Core.Components.Selection;
using GameWorld.Core.Services;
using Shared.Ui.Common.MenuSystem;
using System.Windows.Input;

namespace Editors.KitbasherEditor.UiCommands
{
    public class DeleteObjectCommand : ITransientKitbasherUiCommand
    {
        public string ToolTip { get; set; } = "Delete";
        public ActionEnabledRule EnabledRule => ActionEnabledRule.ObjectOrFaceSelected;
        public Hotkey? HotKey { get; } = new Hotkey(Key.Delete, ModifierKeys.None);


        private readonly SelectionManager _selectionManager;
        private readonly ObjectEditor _objectEditor;
        private readonly FaceEditor _faceEditor;

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
