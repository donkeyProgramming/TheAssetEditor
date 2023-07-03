using MonoGame.Framework.WpfInterop;
using System;
using System.Collections.Generic;
using System.Text;

namespace View3D.Utility
{
    public class ComponentManagerResolver
    {
        private readonly WpfGame _game;

        public IComponentManager ComponentManager { get => _game; }
        public ComponentManagerResolver(WpfGame game)
        {
            _game = game;
        }
    }
}
