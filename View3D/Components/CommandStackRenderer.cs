using Common;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using View3D.Utility;

namespace View3D.Components.Component
{
    public class CommandStackRenderer : BaseComponent
    {
        SpriteBatch _spriteBatch;
        string _animationText;
        GameTime _animationStart;
        bool _startAnimation;
        private readonly ResourceLibary _resourceLibary;
        private readonly EventHub _eventHub;

        public CommandStackRenderer(ResourceLibary resourceLibary, EventHub eventHub)
        {
            _resourceLibary = resourceLibary;
            _eventHub = eventHub;

            _eventHub.Register<CommandStackUndoEvent>(Handle);
            _eventHub.Register<CommandStackChangedEvent>(Handle);
        }

        public override void Initialize()
        {
            _spriteBatch = _resourceLibary.CreateSpriteBatch();
        }

        public void Dispose()
        {
            _spriteBatch.Dispose();
            _spriteBatch = null;
        }

        public override void Draw(GameTime gameTime)
        {
            if (_animationStart != null)
            {
                var timeDiff = (gameTime.TotalGameTime - _animationStart.TotalGameTime).TotalMilliseconds;
                float lerpValue = (float)timeDiff / 2000.0f;
                var alphaValue = MathHelper.Lerp(1, 0, lerpValue);
                if (lerpValue >= 1)
                    _animationStart = null;

                _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
                _spriteBatch.DrawString(_resourceLibary.DefaultFont, _animationText, new Vector2(5, 20), new Color(0, 0, 0, alphaValue));
                _spriteBatch.End();
            }

            base.Draw(gameTime);
        }

        void CreateAnimation(string text)
        {
            _animationText = text;
            _startAnimation = true;
        }



        public override void Update(GameTime gameTime)
        {
            if (_startAnimation == true)
            {
                _animationStart = gameTime;
            }
            _startAnimation = false;

            base.Update(gameTime);
        }

        void Handle(CommandStackChangedEvent notification) => CreateAnimation($"Command added: {notification.HintText}");
        void Handle(CommandStackUndoEvent notification) => CreateAnimation($"Command Undone: {notification.HintText}");
    }
}

