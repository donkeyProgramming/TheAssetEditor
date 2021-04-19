using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Framework.WpfInterop;

namespace View3D.Components.Component
{
    public class FpsComponent : BaseComponent, IDisposable
    {
        #region Fields
        private SpriteBatch _spriteBatch;
        private SpriteFont _font;
        private int _frames;
        private int _liveFrames;
        private TimeSpan _timeElapsed;
        #endregion

        #region Constructors

        public FpsComponent(WpfGame game) : base(game)
        {
        }

        #endregion

        #region Methods

        protected override void LoadContent()
        {
            _font = Game.Content.Load<SpriteFont>("Fonts//DefaultFont");

            _spriteBatch = new SpriteBatch(GraphicsDevice);
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

        #endregion
    }
}