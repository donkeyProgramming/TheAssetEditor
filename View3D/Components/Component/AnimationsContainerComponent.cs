using Common;
using Microsoft.Xna.Framework;
using MonoGame.Framework.WpfInterop;
using Serilog;
using System;
using System.Collections.Generic;
using System.Text;
using View3D.Animation;

namespace View3D.Components.Component
{
    public class AnimationsContainerComponent : BaseComponent
    {
        ILogger _logger = Logging.Create<AnimationsContainerComponent>();

        Dictionary<string, AnimationPlayer> _playerMap = new Dictionary<string, AnimationPlayer>();

        public AnimationsContainerComponent(WpfGame game) : base(game)
        {
        }

        public AnimationPlayer RegisterAnimationPlayer(AnimationPlayer player, string name)
        {
            _playerMap.Add(name, player);
            return player;
        }

        public override void Update(GameTime gameTime)
        {
            foreach (var item in _playerMap)
                item.Value.Update(gameTime);
            base.Update(gameTime);
        }

    }
}
