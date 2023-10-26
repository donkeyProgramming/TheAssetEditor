using Microsoft.Xna.Framework;
using MonoGame.Framework.WpfInterop;
using System.Collections.Generic;

namespace View3D.Components
{
    public class ComponentInserter : IComponentInserter
    {
        private readonly WpfGame _wpfGame;
        private readonly IEnumerable<IGameComponent> _components;

        public ComponentInserter(WpfGame wpfGame, IEnumerable<IGameComponent> components)
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

    public interface IComponentInserter
    {
        void Execute();
    }
}
