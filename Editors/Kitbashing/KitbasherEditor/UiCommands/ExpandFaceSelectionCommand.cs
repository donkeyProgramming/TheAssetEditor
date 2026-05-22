using System.Windows.Input;
using Editors.KitbasherEditor.Core.MenuBarViews;
using GameWorld.Core.Components.Selection;
using GameWorld.Core.Services;
using Shared.Ui.Common.MenuSystem;

namespace Editors.KitbasherEditor.UiCommands
{
    public class ExpandFaceSelectionCommand : ITransientKitbasherUiCommand
    {
        public string ToolTip { get; set; } = "Grow selection";
        public ActionEnabledRule EnabledRule => ActionEnabledRule.FaceSelected;
        public Hotkey? HotKey { get; } = null;

        private readonly FaceEditor _faceEditor;
        private readonly SelectionManager _selectionManager;
        private readonly IWindowsKeyboard _keyboard;

        public ExpandFaceSelectionCommand(FaceEditor faceEditor, SelectionManager selectionManager, IWindowsKeyboard keyboard)
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
