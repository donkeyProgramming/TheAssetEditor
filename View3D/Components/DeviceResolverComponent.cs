using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Framework.WpfInterop;
using System;
using System.Collections.Generic;
using System.Text;

namespace View3D.Components
{

    public interface IDeviceResolver
    {
        public GraphicsDevice Device { get; }
    }

    public class DeviceResolverComponent : BaseComponent, IDeviceResolver
    {
        WpfGame _game;
        public DeviceResolverComponent(WpfGame game) : base(game)
        {
            _game = game;
        }

        public GraphicsDevice Device { get => _game.GraphicsDevice; }
    }

    public class ManualDeviceResolver : IDeviceResolver
    {
        private readonly GraphicsDevice _device;

        public ManualDeviceResolver(GraphicsDevice device)
        {
            _device = device;
        }

        public GraphicsDevice Device { get => _device; }
    }
}
