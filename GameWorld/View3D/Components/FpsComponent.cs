using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using View3D.Utility;

namespace View3D.Components.Component
{
    public class FpsComponent : BaseComponent
    {
        private SpriteFont _font;
        private int _frames;
        private int _liveFrames;
        private TimeSpan _timeElapsed;
        private readonly ResourceLibrary _resourceLibrary;

        public FpsComponent(ResourceLibrary resourceLibrary)
        {
            _resourceLibrary = resourceLibrary;
        }

        protected override void LoadContent()
        {
            _font = _resourceLibrary.DefaultFont;
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
            _resourceLibrary.CommonSpriteBatch.Begin();
            _resourceLibrary.CommonSpriteBatch.DrawString(_font, $"FPS: {_frames}", new Vector2(5), Color.White);
            _resourceLibrary.CommonSpriteBatch.End();
        }
    }
}
