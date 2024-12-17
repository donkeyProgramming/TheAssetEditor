using System;
using GameWorld.Core.Components.Rendering;
using GameWorld.Core.Rendering.RenderItems;
using GameWorld.Core.Services;
using Microsoft.Xna.Framework;
using Shared.Core.Events;

namespace GameWorld.Core.Components
{
    public class CommandStackRenderer : BaseComponent, IDisposable
    {
        string _animationText;
        GameTime? _animationStart;
        bool _startAnimation;
        private readonly RenderEngineComponent _resourceLibrary;
        private readonly IEventHub _eventHub;
        private readonly RenderEngineComponent _renderEngineComponent;

        public CommandStackRenderer(RenderEngineComponent resourceLibrary, IEventHub eventHub, RenderEngineComponent renderEngineComponent)
        {
            _resourceLibrary = resourceLibrary;
            _eventHub = eventHub;
            _renderEngineComponent = renderEngineComponent;

            _eventHub.Register<CommandStackUndoEvent>(this, Handle);
            _eventHub.Register<CommandStackChangedEvent>(this, Handle);
        }

        public override void Draw(GameTime gameTime)
        {
            if (_animationStart != null)
            {
                var timeDiff = (gameTime.TotalGameTime - _animationStart.TotalGameTime).TotalMilliseconds;
                var lerpValue = (float)timeDiff / 2000.0f;
                var alphaValue = MathHelper.Lerp(1, 0, lerpValue);
                if (lerpValue >= 1)
                    _animationStart = null;

                var renderItem = new FontRenderItem(_resourceLibrary,_animationText, new Vector2(5, 20), new Color(0, 0, 0, alphaValue));
                _renderEngineComponent.AddRenderItem(RenderBuckedId.Font, renderItem);
            }
        }

        void CreateAnimation(string text)
        {
            _animationText = text;
            _startAnimation = true;
        }

        public override void Update(GameTime gameTime)
        {
            if (_startAnimation == true)
                _animationStart = gameTime;
            _startAnimation = false;
        }

        void Handle(CommandStackChangedEvent notification) => CreateAnimation($"Command added: {notification.HintText}");
        void Handle(CommandStackUndoEvent notification) => CreateAnimation($"Command Undone: {notification.HintText}");

        public void Dispose()
        {
            _eventHub.UnRegister(this);
        }
    }
}

