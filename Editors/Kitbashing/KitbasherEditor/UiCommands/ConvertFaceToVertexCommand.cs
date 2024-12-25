using Editors.KitbasherEditor.Core.MenuBarViews;
using GameWorld.Core.Components.Selection;
using GameWorld.Core.Services;
using Shared.Ui.Common.MenuSystem;

namespace Editors.KitbasherEditor.UiCommands
{
    public class ConvertFaceToVertexCommand : ITransientKitbasherUiCommand
    {
        public string ToolTip { get; set; } = "Convert selected faces to vertexes";
        public ActionEnabledRule EnabledRule => ActionEnabledRule.FaceSelected;
        public Hotkey? HotKey { get; } = null;

        private readonly FaceEditor _faceEditor;
        private readonly SelectionManager _selectionManager;

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
