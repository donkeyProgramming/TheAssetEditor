using GameWorld.WpfWindow;
using Microsoft.Xna.Framework.Graphics;

namespace GameWorld.Core.Utility
{
    public interface IDeviceResolver
    {
        public GraphicsDevice Device { get; }
    }

    public class DeviceResolver : IDeviceResolver
    {
        private readonly WpfGame _scene;
        public DeviceResolver(WpfGame game)
        {
            _scene = game;
        }

        public GraphicsDevice Device { get => _scene.GraphicsDevice; }
    }
}
