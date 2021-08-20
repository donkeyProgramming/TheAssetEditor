using Microsoft.Xna.Framework;
using MonoGame.Framework.WpfInterop;

namespace View3D.Scene
{
    public class SceneContainer : WpfGame
    {
        private bool _disposed;
        WpfGraphicsDeviceService _diviceService;

        protected override void Initialize()
        {
            _disposed = false;
            _diviceService = new WpfGraphicsDeviceService(this);

            base.Initialize();
        }

        protected override void LoadContent()
        {
            base.LoadContent();
        }

        protected override void Draw(GameTime time)
        {
            base.Draw(time);
        }

        protected override void Dispose(bool disposing)
        {
            if (_disposed)
                return;
            _disposed = true;

            _diviceService = null;
            base.Dispose(disposing);
            Components.Clear();
        }
    }
}
