using CommonControls.Events.UiCommands;
using View3D.Components.Rendering;

namespace KitbasherEditor.ViewModels.UiCommands
{
    public class ToggleBackFaceRenderingCommand : IExecutableUiCommand
    {
        private readonly RenderEngineComponent _renderEngineComponent;

        public ToggleBackFaceRenderingCommand(RenderEngineComponent renderEngineComponent)
        {
            _renderEngineComponent = renderEngineComponent;
        }

        public void Execute() => _renderEngineComponent.ToggelBackFaceRendering()
    }
}
