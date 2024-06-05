using Microsoft.Xna.Framework;
using MonoGame.Framework.WpfInterop;
using Shared.Core.Events;

namespace View3D.Services
{
    public class GameWorld : WpfGame
    {
        private bool _disposed;
        WpfGraphicsDeviceService _deviceServiceHandle;

        public GameWorld(IResourceLibrary resourceLibrary, EventHub eventHub, string contentDir = "BuiltContent") : base(resourceLibrary, eventHub, contentDir)
        {

        }

        protected override void Initialize()
        {
            _disposed = false;
            _deviceServiceHandle = new WpfGraphicsDeviceService(this);

            base.Initialize();
        }


        protected override void Dispose(bool disposing)
        {
            if (_disposed)
                return;
            _disposed = true;

            _deviceServiceHandle = null;
            base.Dispose(disposing);
            Components.Clear();
        }
    }
}
