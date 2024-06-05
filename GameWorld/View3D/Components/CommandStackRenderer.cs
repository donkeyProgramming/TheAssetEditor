using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Shared.Core.Events;
using View3D.Utility;

namespace View3D.Components.Component
{
    public class CommandStackRenderer : BaseComponent
    {
        string _animationText;
        GameTime _animationStart;
        bool _startAnimation;
        private readonly ResourceLibrary _resourceLibrary;
        private readonly EventHub _eventHub;

        public CommandStackRenderer(ResourceLibrary resourceLibrary, EventHub eventHub)
        {
            _resourceLibrary = resourceLibrary;
            _eventHub = eventHub;

            _eventHub.Register<CommandStackUndoEvent>(Handle);
            _eventHub.Register<CommandStackChangedEvent>(Handle);
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

                _resourceLibrary.CommonSpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
                _resourceLibrary.CommonSpriteBatch.DrawString(_resourceLibrary.DefaultFont, _animationText, new Vector2(5, 20), new Color(0, 0, 0, alphaValue));
                _resourceLibrary.CommonSpriteBatch.End();
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

