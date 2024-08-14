using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Windows;

namespace GameWorld.Core.WpfWindow
{
    /// <summary>
    /// The <see cref="Microsoft.Xna.Framework.Content.ContentManager"/> needs a <see cref="IGraphicsDeviceService"/> to be in the <see cref="System.ComponentModel.Design.IServiceContainer"/>. This class fulfills this purpose.
    /// </summary>
    public class WpfGraphicsDeviceService : IGraphicsDeviceService, IGraphicsDeviceManager, IDisposable
    {
        internal const int MsaaSampleLimit = 32;
        readonly WpfGame _host;

        /// <summary>
        /// Create a new instance of the dummy. The constructor will autom. add the instance itself to the <see cref="D3D11Host.Services"/> container of <see cref="host"/>.
        /// </summary>
        /// <param name="host"></param>
        public WpfGraphicsDeviceService(WpfGame host)
        {
            _host = host ?? throw new ArgumentNullException(nameof(host));

            if (host.Services.GetService(typeof(IGraphicsDeviceService)) != null)
                throw new NotSupportedException("A graphics device service is already registered.");

            if (host.GraphicsDevice == null)
                throw new ArgumentException("Provided host graphics device is null.");

            GraphicsDevice = host.GraphicsDevice;
            _host.GraphicsDevice.DeviceReset += GraphicsDevice_DeviceReset;
            _host.GraphicsDevice.DeviceResetting += GraphicsDevice_DeviceResetting;
            _host.GraphicsDevice.Disposing += GraphicsDevice_Disposing;

            host.Services.AddService(typeof(IGraphicsDeviceService), this);
            host.Services.AddService(typeof(IGraphicsDeviceManager), this);
        }

        private void GraphicsDevice_DeviceResetting(object sender, EventArgs e)
        {
            DeviceResetting?.Invoke(this, e);
        }

        private void GraphicsDevice_DeviceReset(object sender, EventArgs e)
        {
            DeviceReset?.Invoke(this, e);
        }

        private void GraphicsDevice_Disposing(object sender, EventArgs e)
        {
            _host.GraphicsDevice.DeviceReset -= GraphicsDevice_DeviceReset;
            _host.GraphicsDevice.DeviceResetting -= GraphicsDevice_DeviceResetting;
            _host.GraphicsDevice.Disposing -= GraphicsDevice_Disposing;
        }


        /// <inheritdoc />
        public event EventHandler<EventArgs> DeviceCreated;

        /// <inheritdoc />
        public event EventHandler<EventArgs> DeviceDisposing;

        /// <inheritdoc />
        public event EventHandler<EventArgs> DeviceReset;

        /// <inheritdoc />
        public event EventHandler<EventArgs> DeviceResetting;

        public GraphicsDevice GraphicsDevice { get; }

        public bool PreferMultiSampling { get; set; } = true;

        /// <summary>
        /// Gets the scaling factor that is applied to the attached gamecontrol.
        /// For legacy compatibility this always defaults to a factor of 1.
        /// If your monitor is scaled at 200%, then this will cause the game to render at only half the size.
        /// In order to render at full native resolution, set this value to the correct <see cref="SystemDpiScalingFactor"/>.
        /// </summary>
        public double DpiScalingFactor
        {
            get => _host.DpiScalingFactor;
            set
            {
                if (value <= 0)
                    throw new ArgumentOutOfRangeException(nameof(DpiScalingFactor), "value must be positive");
                _host.DpiScalingFactor = value;
            }
        }

        /// <summary>
        /// When called returns the system Dpi scaling factor.
        /// The scaling factor may be different between different monitors.
        /// This value will always return the value based on the monitor where the attached gamecontrol is positioned.
        /// </summary>
        public double SystemDpiScalingFactor => PresentationSource.FromVisual(_host).CompositionTarget.TransformToDevice.M11;

        public int PreferredBackBufferWidth => (int)_host.ActualWidth;

        public int PreferredBackBufferHeight => (int)_host.ActualHeight;


        public bool BeginDraw()
        {
            return true;
        }

        public void CreateDevice()
        {
            ApplyChanges();
            DeviceCreated?.Invoke(this, EventArgs.Empty);
        }

        public void EndDraw()
        {

        }

        public void ApplyChanges()
        {
            var w = Math.Max((int)_host.ActualWidth, 1);
            var h = Math.Max((int)_host.ActualHeight, 1);
            var pp = new PresentationParameters
            {
                // set to windows limit, if gpu doesn't support it, monogame will autom. scale it down to the next supported level
                MultiSampleCount = PreferMultiSampling ? MsaaSampleLimit : 0,
                BackBufferWidth = w,
                BackBufferHeight = h,
                DeviceWindowHandle = nint.Zero
            };
            // would be so easy to just call reset. but for some reason monogame doesn't want the WindowHandle to be null on reset (but it's totally fine to be null on create)
            // GraphicsDevice.Reset(pp);
            DeviceDisposing?.Invoke(this, EventArgs.Empty);
            // manually work around it by telling our base implementation to handle the changes
            _host.RecreateGraphicsDevice(pp);
        }

        public void Dispose()
        {
            //_host.GraphicsDevice.DeviceReset -= GraphicsDevice_DeviceReset;
            //_host.GraphicsDevice.DeviceResetting -= GraphicsDevice_DeviceResetting;
        }
    }
}
