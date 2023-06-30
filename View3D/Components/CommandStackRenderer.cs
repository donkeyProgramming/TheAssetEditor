using MediatR;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Threading;
using System.Threading.Tasks;
using View3D.Utility;

namespace View3D.Components.Component
{
    public class CommandStackRenderer : BaseComponent, 
        INotificationHandler<CommandStackChangedEvent>,
        INotificationHandler<CommandStackUndoEvent>
    {
        SpriteBatch _spriteBatch;
        string _animationText;
        GameTime _animationStart;
        bool _startAnimation;
        private readonly ResourceLibary _resourceLibary;
        private readonly DeviceResolverComponent _deviceResolverComponent;

        public CommandStackRenderer(ResourceLibary resourceLibary, DeviceResolverComponent deviceResolverComponent)
        {
            _resourceLibary = resourceLibary;
            _deviceResolverComponent = deviceResolverComponent;
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

        public override void Initialize()
        {
            _spriteBatch = new SpriteBatch(_deviceResolverComponent.Device);
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

        public Task Handle(CommandStackChangedEvent notification, CancellationToken cancellationToken)
        {
            CreateAnimation($"Command added: {notification.HintText}");
            return Task.CompletedTask;
        }

        public Task Handle(CommandStackUndoEvent notification, CancellationToken cancellationToken)
        {
            CreateAnimation($"Command Undone: {notification.HintText}");
            return Task.CompletedTask;
        }
    }
}

