using System;
using GameWorld.Core.Components.Rendering;
using GameWorld.Core.Rendering.RenderItems;
using Microsoft.Xna.Framework;

namespace GameWorld.Core.Components
{
    public class FpsComponent : BaseComponent
    {
        private int _frames;
        private int _liveFrames;
        private TimeSpan _timeElapsed;
        private readonly RenderEngineComponent _renderEngineComponent;


        public FpsComponent(RenderEngineComponent renderEngineComponent)
        {
            _renderEngineComponent = renderEngineComponent;
        }

        public override void Update(GameTime gameTime)
        {
            _timeElapsed += gameTime.ElapsedGameTime;
            if (_timeElapsed >= TimeSpan.FromSeconds(1))
            {
                _timeElapsed -= TimeSpan.FromSeconds(1);
                _frames = _liveFrames;
                _liveFrames = 0;
            }
        }

        public override void Draw(GameTime gameTime)
        {
            _liveFrames++;
            var text = $"FPS: {_frames}";

            var renderItem = new FontRenderItem(_renderEngineComponent, text, new Vector2(5), Color.White);
            _renderEngineComponent.AddRenderItem(RenderBuckedId.Font, renderItem);
        }
    }
}
