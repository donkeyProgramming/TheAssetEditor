using GameWorld.Core.Components.Selection;
using GameWorld.Core.Services;
using KitbasherEditor.ViewModels.MenuBarViews;
using Shared.Ui.Common.MenuSystem;

namespace KitbasherEditor.ViewModels.UiCommands
{
    public class ConvertFaceToVertexCommand : IKitbasherUiCommand
    {
        public string ToolTip { get; set; } = "Convert selected faces to vertexes";
        public ActionEnabledRule EnabledRule => ActionEnabledRule.FaceSelected;
        public Hotkey HotKey { get; } = null;

        FaceEditor _faceEditor;
        SelectionManager _selectionManager;

        public ConvertFaceToVertexCommand(FaceEditor faceEditor, SelectionManager selectionManager)
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
