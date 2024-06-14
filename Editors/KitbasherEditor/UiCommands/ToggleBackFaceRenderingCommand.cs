using GameWorld.Core.Components.Rendering;
using KitbasherEditor.ViewModels.MenuBarViews;
using Shared.Ui.Common.MenuSystem;

namespace KitbasherEditor.ViewModels.UiCommands
{
    public class ToggleBackFaceRenderingCommand : IKitbasherUiCommand
    {
        public string ToolTip { get; set; } = "Toggle backface rendering";
        public ActionEnabledRule EnabledRule => ActionEnabledRule.Always;
        public Hotkey HotKey { get; } = null;

        private readonly RenderEngineComponent _renderEngineComponent;

        public ToggleBackFaceRenderingCommand(RenderEngineComponent renderEngineComponent)
        {
            _renderEngineComponent = renderEngineComponent;
        }

        public void Execute() => _renderEngineComponent.ToggelBackFaceRendering();
    }
}
