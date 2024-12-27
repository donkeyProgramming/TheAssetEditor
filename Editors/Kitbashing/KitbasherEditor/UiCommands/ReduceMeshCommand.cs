using Editors.KitbasherEditor.Core.MenuBarViews;
using GameWorld.Core.Components.Selection;
using GameWorld.Core.SceneNodes;
using GameWorld.Core.Services;
using Shared.Ui.Common.MenuSystem;

namespace Editors.KitbasherEditor.UiCommands
{
    public class ReduceMeshCommand : ITransientKitbasherUiCommand
    {
        public string ToolTip { get; set; } = "Reduce the mesh polygon count by 10%";
        public ActionEnabledRule EnabledRule => ActionEnabledRule.AtleastOneObjectSelected;
        public Hotkey? HotKey { get; } = null;

        private readonly SelectionManager _selectionManager;
        private readonly ObjectEditor _objectEditor;

        public ReduceMeshCommand(SelectionManager selectionManager, ObjectEditor objectEditor)
        {
            _selectionManager = selectionManager;
            _objectEditor = objectEditor;
        }

        public void Execute()
        {
            var selectedObjects = _selectionManager.GetState() as ObjectSelectionState;
            if (selectedObjects == null || selectedObjects.SelectionCount() == 0)
                return;

            var meshNodes = selectedObjects.SelectedObjects()
                .Where(x => x is Rmv2MeshNode)
                .Select(x => x as Rmv2MeshNode)
                .ToList();

            _objectEditor.ReduceMesh(meshNodes, 0.9f, true);
        }
    }
}
