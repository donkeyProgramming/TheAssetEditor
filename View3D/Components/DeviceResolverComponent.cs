using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Framework.WpfInterop;
using System;
using System.Collections.Generic;
using System.Text;

namespace View3D.Components
{
    public class DeviceResolverComponent : BaseComponent
    {
        WpfGame _game;
        public DeviceResolverComponent(WpfGame game) : base(game)
        {
            _game = game;
        }

        public GraphicsDevice Device { get => _game.GraphicsDevice; }
    }
}
