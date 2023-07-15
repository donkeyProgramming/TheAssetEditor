using Microsoft.Xna.Framework.Graphics;
using View3D.Services;

namespace View3D.Components
{

    public interface IDeviceResolver
    {
        public GraphicsDevice Device { get; }
    }

    public class DeviceResolverComponent : BaseComponent, IDeviceResolver
    {
        GameWorld _scene;
        public DeviceResolverComponent(GameWorld game)
        {
            _scene = game;
        }

        public GraphicsDevice Device { get => _scene.GraphicsDevice; }
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
