using MediatR;
using Microsoft.Xna.Framework;
using MonoGame.Framework.WpfInterop;
using View3D.Components;

namespace View3D.Scene
{
    public class MainScene : WpfGame
    {
        private bool _disposed;
        WpfGraphicsDeviceService _diviceService;

        public MainScene(IMediator mediator, string contentDir = "ContentOutput") : base(mediator, contentDir)
        {

        }

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
