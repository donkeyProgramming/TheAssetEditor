using GameWorld.Core.Components.Rendering;
using GameWorld.Core.Rendering;
using Microsoft.Xna.Framework;

namespace GameWorld.Core.Components
{
    public class GridComponent : BaseComponent
    {
        private readonly RenderEngineComponent _renderEngineComponent;

        public GridComponent(RenderEngineComponent renderEngineComponent)
        {
            _renderEngineComponent = renderEngineComponent;
        }

        public override void Draw(GameTime gameTime)
        {
            _renderEngineComponent.AddRenderLines(LineHelper.CreateGrid());
        }
    }
}
