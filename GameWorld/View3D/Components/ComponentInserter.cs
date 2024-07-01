using System.Collections.Generic;
using GameWorld.WpfWindow;
using Microsoft.Xna.Framework;

namespace GameWorld.Core.Components
{
    public interface IComponentInserter
    {
        void Execute();
    }

    public class ComponentInserter : IComponentInserter
    {
        private readonly IWpfGame _wpfGame;
        private readonly IEnumerable<IGameComponent> _components;

        public ComponentInserter(IWpfGame wpfGame, IEnumerable<IGameComponent> components)
        {
            _wpfGame = wpfGame;
            _components = components;
        }

        public void Execute()
        {
            foreach (var component in _components)
                _wpfGame.AddComponent(component);
        }
    }
}
