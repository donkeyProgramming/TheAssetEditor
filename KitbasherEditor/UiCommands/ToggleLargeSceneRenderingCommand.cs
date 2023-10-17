using CommonControls.Common.MenuSystem;
using KitbasherEditor.ViewModels.MenuBarViews;
using View3D.Components.Rendering;

namespace KitbasherEditor.ViewModels.UiCommands
{
    public class ToggleLargeSceneRenderingCommand : IKitbasherUiCommand
    {
        public string ToolTip { get; set; } = "Toogle rendering of large scenes";
        public ActionEnabledRule EnabledRule => ActionEnabledRule.Always;
        public Hotkey HotKey { get; } = null;

        private readonly RenderEngineComponent _renderEngineComponent;

        public ToggleLargeSceneRenderingCommand(RenderEngineComponent renderEngineComponent)
        {
            _renderEngineComponent = renderEngineComponent;
        }

        public void Execute() => _renderEngineComponent.ToggleLargeSceneRendering();
    }
}
