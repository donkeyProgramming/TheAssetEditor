using GameWorld.Core.Components.Rendering;
using KitbasherEditor.ViewModels.MenuBarViews;
using Shared.Ui.Common.MenuSystem;

namespace Editors.KitbasherEditor.UiCommands
{
    public class ToggleLargeSceneRenderingCommand : IKitbasherUiCommand
    {
        public string ToolTip { get; set; } = "Toggle rendering of large scenes";
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
