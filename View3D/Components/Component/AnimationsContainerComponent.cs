using Common;
using Microsoft.Xna.Framework;
using MonoGame.Framework.WpfInterop;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
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
            UpdateOrder= (int)ComponentUpdateOrderEnum.Animation;
        }

        public AnimationPlayer RegisterAnimationPlayer(AnimationPlayer player, string name)
        {
            _playerMap.Add(name, player);
            return player;
        }

        public void Remove(AnimationPlayer player)
        {
            var item = _playerMap.Where(x=>x.Value == player);
            if (item != null)
                _playerMap.Remove(item.First().Key);
        }

        public AnimationPlayer Get(string name)
        {
            var hasKey = _playerMap.ContainsKey(name);
            if (!hasKey)
                return null;
            return _playerMap[name];
        }

        public override void Update(GameTime gameTime)
        {
            var itemsToRemove = _playerMap.Where(x => x.Value.MarkedForRemoval).ToList();
            foreach (var item in itemsToRemove)
                _playerMap.Remove(item.Key);

            foreach (var item in _playerMap)
                item.Value.Update(gameTime);
            base.Update(gameTime);
        }


    }
}
