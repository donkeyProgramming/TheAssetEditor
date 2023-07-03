using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using View3D.Utility;

namespace View3D.Components.Component
{
    public class FpsComponent : BaseComponent, IDisposable
    {
        private SpriteBatch _spriteBatch;
        private SpriteFont _font;
        private int _frames;
        private int _liveFrames;
        private TimeSpan _timeElapsed;
        private readonly ResourceLibary _resourceLibary;
        private readonly DeviceResolverComponent _deviceResolverComponent;

        public FpsComponent(ResourceLibary resourceLibary, DeviceResolverComponent deviceResolverComponent)
        {
            _resourceLibary = resourceLibary;
            _deviceResolverComponent = deviceResolverComponent;
        }

        protected override void LoadContent()
        {
            _font = _resourceLibary.DefaultFont;
            _spriteBatch = new SpriteBatch(_deviceResolverComponent.Device);
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
            _spriteBatch.Begin();
            _spriteBatch.DrawString(_font, $"FPS: {_frames}", new Vector2(5), Color.White);
            _spriteBatch.End();
        }

        public void Dispose()
        {
            _spriteBatch.Dispose();
        }
    }
}