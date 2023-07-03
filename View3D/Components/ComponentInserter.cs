using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using View3D.Utility;

namespace View3D.Components
{
    public class ComponentInserter : IComponentInserter
    {
        private readonly ComponentManagerResolver _componentManagerResolver;
        private readonly IEnumerable<IGameComponent> _components;

        public ComponentInserter(ComponentManagerResolver componentManagerResolver, IEnumerable<IGameComponent> components)
        {
            _componentManagerResolver = componentManagerResolver;
            _components = components;
        }

        public void Execute()
        {
            foreach (var component in _components)
                _componentManagerResolver.ComponentManager.AddComponent(component);
        }
    }

    public interface IComponentInserter
    {
        void Execute();
    }
}
