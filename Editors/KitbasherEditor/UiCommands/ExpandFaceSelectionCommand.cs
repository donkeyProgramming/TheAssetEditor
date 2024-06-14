using System.Windows.Input;
using GameWorld.Core.Components.Selection;
using GameWorld.Core.Services;
using KitbasherEditor.ViewModels.MenuBarViews;
using Shared.Ui.Common.MenuSystem;

namespace KitbasherEditor.ViewModels.UiCommands
{
    public class ExpandFaceSelectionCommand : IKitbasherUiCommand
    {
        public string ToolTip { get; set; } = "Grow selection";
        public ActionEnabledRule EnabledRule => ActionEnabledRule.FaceSelected;
        public Hotkey HotKey { get; } = null;

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
