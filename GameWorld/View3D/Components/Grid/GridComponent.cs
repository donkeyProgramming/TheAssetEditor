using GameWorld.Core.Components.Rendering;
using GameWorld.Core.Services;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GameWorld.Core.Components.Grid
{

    /// <summary>
    /// Infinite grid component using a procedural screen-space shader.
    /// Renders a camera-following ground plane quad with analytically anti-aliased
    /// grid lines computed in the fragment shader via frac() + fwidth() + smoothstep().
    /// </summary>
    public class GridComponent : BaseComponent, IDisposable
    {
        private readonly ArcBallCamera _camera;
        private readonly IScopedResourceLibrary _resourceLibrary;
        private readonly RenderEngineComponent _renderEngineComponent;
        private Effect? _shaderEffect;
        private GridRenderItem? _renderItem;


        public bool ShowGrid { get; set; } = true;
        public Vector3 GridColur { get; set; } = new Vector3(0f, 0f, 0f);

        public GridComponent(ArcBallCamera camera, IScopedResourceLibrary resourceLibrary, RenderEngineComponent renderEngineComponent)
        {
            _camera = camera;
            _resourceLibrary = resourceLibrary;
            _renderEngineComponent = renderEngineComponent;
        }

        public override void Initialize()
        {
            _shaderEffect = _resourceLibrary.GetStaticEffect(ShaderTypes.Grid);
            _renderItem = new GridRenderItem(_shaderEffect);
            base.Initialize();
        }

        public override void Draw(GameTime gameTime)
        {
            if (!ShowGrid || _shaderEffect == null || _renderItem == null)
                return;

            _renderItem.Update(_camera, GridColur);
            _renderEngineComponent.AddRenderItem(RenderBuckedId.Normal, _renderItem);
        }

        public void Dispose()
        {
            _shaderEffect = null;
        }
    }
}
