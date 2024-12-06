using Editors.KitbasherEditor.Core.MenuBarViews;
using GameWorld.Core.Components;
using GameWorld.Core.SceneNodes;
using GameWorld.Core.Services;
using Shared.Ui.Common.MenuSystem;

namespace Editors.KitbasherEditor.UiCommands
{
    public class SortMeshesCommand : ITransientKitbasherUiCommand
    {
        public string ToolTip { get; set; } = "Sort models by name";
        public ActionEnabledRule EnabledRule => ActionEnabledRule.Always;
        public Hotkey? HotKey { get; } = null;

        private readonly SceneManager _sceneManager;
        private readonly ObjectEditor _objectEditor;

        public SortMeshesCommand(SceneManager sceneManager, ObjectEditor objectEditor)
        {
            _sceneManager = sceneManager;
            _objectEditor = objectEditor;
        }

        public void Execute()
        {
            var rootNode = _sceneManager.GetNodeByName<MainEditableNode>(SpecialNodes.EditableModel);
            var lod0 = rootNode.GetLodNodes().FirstOrDefault();
            if (lod0 != null)
                _objectEditor.SortMeshes(lod0);
        }
    }
}
