using CommonControls.Common.MenuSystem;
using CommonControls.Events.UiCommands;
using KitbasherEditor.ViewModels.MenuBarViews;
using System.Windows;
using View3D.Components.Component;
using View3D.SceneNodes;
using View3D.Services;
using MessageBox = System.Windows.MessageBox;

namespace KitbasherEditor.ViewModels.UiCommands
{
    public class CopyRootLodCommand : IKitbasherUiCommand
    {
        public string ToolTip { get; set; } = "Copy LOD 0 to every LOD slot";
        public ActionEnabledRule EnabledRule => ActionEnabledRule.Always;
        public Hotkey HotKey { get; } = null;

        ObjectEditor _objectEditor;
        SceneManager _sceneManager;

        public CopyRootLodCommand(ObjectEditor objectEditor, SceneManager sceneManager)
        {
            _objectEditor = objectEditor;
            _sceneManager = sceneManager;
        }

        public void Execute()
        {
            var res = MessageBox.Show("Are you sure to copy lod 0 to every lod slots? This cannot be undone!", "Attention", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (res != MessageBoxResult.Yes) return;

            var rootNode = _sceneManager.GetNodeByName<MainEditableNode>(SpecialNodes.EditableModel);
            var lodGenerationService = new LodGenerationService(_objectEditor);

            rootNode.GetLodNodes().ForEach(x =>
            {
                x.LodReductionFactor = 1;
                x.OptimizeLod_Alpha = false;
                x.OptimizeLod_Vertex = false;
            });

            lodGenerationService.CreateLodsForRootNode(rootNode);
        }
    }
}
