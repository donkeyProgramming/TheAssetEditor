using CommonControls.Events.UiCommands;
using View3D.Components.Rendering;

namespace KitbasherEditor.ViewModels.UiCommands
{
    public class ToggleLargeSceneRenderingCommand : IExecutableUiCommand
    {
        private readonly RenderEngineComponent _renderEngineComponent;

        public ToggleLargeSceneRenderingCommand(RenderEngineComponent renderEngineComponent)
        {
            _renderEngineComponent = renderEngineComponent;
        }

        public void Execute() => _renderEngineComponent.ToggleLargeSceneRendering();
    }
}
