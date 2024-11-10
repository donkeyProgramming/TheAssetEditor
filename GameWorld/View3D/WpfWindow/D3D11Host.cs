using GameWorld.Core.WpfWindow.Internals;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Color = Microsoft.Xna.Framework.Color;

namespace GameWorld.Core.WpfWindow
{
    /// <summary>
    /// Host a Direct3D 11 scene.
    /// Low level abstraction, should not be used (use <see cref="WpfGame"/> instead).
    /// </summary>
    public abstract class D3D11Host : Image, IDisposable
    {
        private static readonly object GraphicsDeviceLock = new object();

        private bool _isRendering;

        // The Direct3D 11 device (shared by all D3D11Host elements)
        // TODO: find out why the original implementation had this as static & shared
        private static GraphicsDevice _staticGraphicsDevice;
        private GraphicsDevice _graphicsDevice;
        private static bool? _isInDesignMode;
        private static int _referenceCount;

        private D3D11Image _d3D11Image;
        private bool _disposed;
        private TimeSpan _lastRenderingTime;
        private bool _loaded, GraphicsDeviceInitialized;

        /// <summary>
        /// Shared between WPF and monogame.
        /// </summary>
        private RenderTarget2D _sharedRenderTarget;
        /// <summary>
        /// Actual rendertarget that monogame will draw into.
        /// Once a draw call is finished it then copies its content to <see cref="_sharedRenderTarget"/>.
        /// This prevents flickering of the screen when WPF decides to draw the rendertarget to screen while monogame is midway populating it.
        /// </summary>
        private RenderTarget2D _cachedRenderTarget;
        private bool _resetBackBuffer, _dpiChanged;
        private bool _isActive;
        private SpriteBatch _spriteBatch;
        private double _dpiScalingFactor = 1;
        private static bool _useASingleSharedGraphicsDevice = true;
        private List<IDisposable> _toBeDisposedNextFrame = new List<IDisposable>();

        /// <summary>
        /// Initializes a new instance of the <see cref="D3D11Host"/> class.
        /// </summary>
        protected D3D11Host()
        {
            // defaulting to fill as that's what's needed in most cases
            Stretch = Stretch.Fill;

            Loaded += OnLoaded;
        }

        /// <summary>
        /// <para>
        /// Must be set before the first instance is created.
        /// </para>
        /// <para>
        /// Defaults to false, (prior 2.0 the behaviour was as if this was set to true).
        /// If set to false each WpfGame instance gets its own graphicsdevice.
        /// If set to true, A single graphics device is shared across all instances of WpfGame.
        /// </para>
        /// <para>
        /// Currently there is bug in monogame when setting this to false AND using MSAA (Disposing rendertargets will crash, so your only option is to not dispose them, causing memoryleaks).
        /// Meanwhile if this is true, you can Dispose rendertargets just fine.
        /// </para>
        /// </summary>
        public static bool UseASingleSharedGraphicsDevice
        {
            get { return _useASingleSharedGraphicsDevice; }
            set
            {
                if (_referenceCount > 0)
                {
                    throw new InvalidOperationException($"{nameof(UseASingleSharedGraphicsDevice)} must be set before the first instance is created and cannot be changed during runtime.");
                }
                _useASingleSharedGraphicsDevice = value;
            }
        }

        /// <summary>
        /// Determines whether the game runs in fixed timestep or not.
        /// The current implementation always calls Update and Draw after each other continuously.
        /// Since the rendering is based on the WPF render thread the exact times at which it will be called cannot be guaranteed.
        /// Therefore this value is always false.
        /// </summary>
        public bool IsFixedTimeStep => false;

        /// <summary>
        /// Gets or sets the target time between two updates. Defaults to 60fps.
        /// WPF is limiting its rendering to 60 FPS max, therefore setting a target value higher than 60 fps (lower than TimeSpan.FromSeconds(1 / 60.0)) will have no effect.
        /// </summary>
        public TimeSpan TargetElapsedTime { get; set; } = TimeSpan.FromTicks(160000); // 60 fps

        /// <summary>
        /// Gets a value indicating whether the controls runs in the context of a designer (e.g.
        /// Visual Studio Designer or Expression Blend).
        /// </summary>
        /// <value>
        /// <see langword="true" /> if controls run in design mode; otherwise, 
        /// <see langword="false" />.
        /// </value>
        public static bool IsInDesignMode
        {
            get
            {
                if (!_isInDesignMode.HasValue)
                    _isInDesignMode =
                        (bool)
                        DependencyPropertyDescriptor.FromProperty(DesignerProperties.IsInDesignModeProperty, typeof(FrameworkElement))
                            .Metadata.DefaultValue;

                return _isInDesignMode.Value;
            }
        }

        /// <summary>
        /// Gets whether the current control is active.
        /// This property will be true, when the control is active (parent window has focus).
        /// Note that if the game is inside a tab, then this property will only be true if the window has focus and the tab is active.
        /// If either the window loses focus or the tab is switched, this property will be false.
        /// </summary>
        public bool IsActive
        {
            get { return _isActive; }
            private set
            {
                if (_isActive == value)
                    return;
                _isActive = value;
                if (!_disposed)
                {
                    if (IsActive)
                        Activated?.Invoke(this, EventArgs.Empty);
                    else
                        Deactivated?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        /// <summary>
        /// Gets the scaling factor that is applied to the attached gamecontrol.
        /// For legacy compatibility this always defaults to a factor of 1.
        /// If your monitor is scaled at 200%, then this will cause the game to render at only half the size.
        /// In order to render at full native resolution, set this value to the correct <see cref="SystemDpiScalingFactor"/>.
        /// </summary>
        public double DpiScalingFactor
        {
            get => _dpiScalingFactor;
            set
            {
                if (value <= 0)
                    throw new ArgumentOutOfRangeException(nameof(DpiScalingFactor), "value must be positive");
                if (_dpiScalingFactor == value)
                    return;

                _dpiScalingFactor = value;
                _dpiChanged = true;
            }
        }

        /// <summary>
        /// Gets the graphics device.
        /// </summary>
        /// <value>The graphics device.</value>
        public GraphicsDevice GraphicsDevice => UseASingleSharedGraphicsDevice ? _staticGraphicsDevice : _graphicsDevice;

        /// <summary>
        /// Default services collection.
        /// </summary>
        public GameServiceContainer Services { get; } = new GameServiceContainer();

        /// <summary>
        /// Event is invoked when the game is activated (window receives focus).
        /// Note for game instances inside a tab control this will fire only when the tab is activated (or the tab was active and the window is focused).
        /// </summary>
        public event EventHandler<EventArgs> Activated;

        /// <summary>
        /// Event is invoked when the game is deactivated (window loses focus).
        /// Note for game instances inside a tab control this will fire only when the tab is deactivated (or the tab was active and the window loses focus).
        /// </summary>
        public event EventHandler<EventArgs> Deactivated;

        public void Dispose()
        {
            // each of those has its own check for disposed
            StopRendering();
            UnitializeImageSource();
            DisposeRenderTargetsFromPreviousFrames();
            if (GraphicsDeviceInitialized)
            {
                UninitializeGraphicsDevice();
                GraphicsDeviceInitialized = false;
            }

            if (_disposed)
                return;

            _disposed = true;
            Activated = null;
            Deactivated = null;
            if (_spriteBatch != null)
            {
                _spriteBatch.Dispose();
                _spriteBatch = null;
            }
            Dispose(true);
        }

        protected abstract void Dispose(bool disposing);

        protected virtual void Initialize()
        {
            _disposed = false;

            var graphicsDeviceManager = (IGraphicsDeviceManager)Services.GetService(typeof(IGraphicsDeviceManager));
            if (graphicsDeviceManager == null)
                throw new NotSupportedException($"Services must contain a {nameof(IGraphicsDeviceManager)} instance");

            graphicsDeviceManager.CreateDevice();
        }

        /// <summary>
        /// Raises the <see cref="FrameworkElement.SizeChanged" /> event, using the specified 
        /// information as part of the eventual event data.
        /// </summary>
        /// <param name="sizeInfo">Details of the old and new size involved in the change.</param>
        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            _resetBackBuffer = true;
            base.OnRenderSizeChanged(sizeInfo);
        }

        protected virtual void Render(GameTime time)
        {
        }

        private void InitializeGraphicsDevice()
        {
            lock (GraphicsDeviceLock)
            {
                _referenceCount++;
                if (_referenceCount == 1 || !UseASingleSharedGraphicsDevice)
                {
                    // Create Direct3D 11 device.
                    var presentationParameters = new PresentationParameters
                    {
                        // Do not associate graphics device with window.
                        DeviceWindowHandle = nint.Zero,
                        DepthStencilFormat = DepthFormat.Depth24,
                    };
                    var gd = CreateSharedGraphicsDevice(presentationParameters);
                    if (UseASingleSharedGraphicsDevice)
                        _staticGraphicsDevice = gd;
                    else
                        _graphicsDevice = gd;
                }
            }
        }

        private static GraphicsDevice CreateSharedGraphicsDevice(PresentationParameters presentationParameters)
        {
            // TODO: could allow user to chose which adapter to use
            return new GraphicsDevice(GraphicsAdapter.DefaultAdapter, GraphicsProfile.HiDef, presentationParameters);
        }

        /// <summary>
        /// Helper that allows applying new presentation parameters.
        /// </summary>
        /// <param name="pp"></param>
        public void RecreateGraphicsDevice(PresentationParameters pp)
        {
            lock (GraphicsDeviceLock)
            {
                if (GraphicsDevice == null)
                    throw new NotSupportedException("Can only recreate graphicsdevice when one already exists. Initalize one first!");

                CreateGraphicsDeviceDependentResources(pp);
            }
        }

        protected virtual void OnGraphicDeviceDisposed()
        {
        }

        private void UninitializeGraphicsDevice()
        {
            // We pretende this is a singelton...
            //return;
            //lock (GraphicsDeviceLock)
            //{
            //    _referenceCount--;
            //    if (_referenceCount == 0 || !UseASingleSharedGraphicsDevice)
            //    {
            //        // dirty workaround. when just calling .Dispose monogame crashes because MonoGame internal _swapChain is null and
            //        // they just blindly trust it isn't null and call .Dipose on it
            //
            //        // as a workaround we will now call the bare minimum setup methods to create a (temporary) swapchain in the last second and then call Dispose
            //        // that way Dispose doesn't crash
            //
            //        // literally anything but 0 will do, our WPF window is already closed at this point (returns 0) so just use a random pointer
            //        // technically a swapChain will be attached to this device handle for a few ms before we dispose it again
            //        // let's hope attaching it to "1" doesn't break anything
            //        GraphicsDevice.PresentationParameters.DeviceWindowHandle = new IntPtr(1);
            //
            //        // call below will now trigger some events on the GraphicsDevice and we have no way of suppressing them
            //        // but we need to call reset to actually create a swapChain, otherwise Dispose will crash
            //        GraphicsDevice.Reset();
            //
            //        // finally we can safely call dispose without receiving NullReferenceException
            //        GraphicsDevice.Dispose();
            //        if (UseASingleSharedGraphicsDevice)
            //            _staticGraphicsDevice = null;
            //        else
            //            _graphicsDevice = null;
            //
            //        OnGraphicDeviceDisposed();
            //    }
            //}
        }

        private void CreateBackBuffer()
        {
            _d3D11Image.SetBackBuffer(null);
            if (_sharedRenderTarget != null)
            {
                // can't directly dispose rendertargets now since Monogame needs it for one more frame
                // (when a rendertarget is swapped, monogame still uses the old one for one more frame)
                // so queue dispose for later
                _toBeDisposedNextFrame.Add(_sharedRenderTarget);
            }

            var sampleCount = 0;
            if (_cachedRenderTarget != null)
            {
                // if there was a previous rendertarget, reuse its sample count
                sampleCount = _cachedRenderTarget.MultiSampleCount;
                // can't directly dispose rendertargets now since Monogame needs it for one more frame
                // (when a rendertarget is swapped, monogame still uses the old one for one more frame)
                // so queue dispose for later
                _toBeDisposedNextFrame.Add(_cachedRenderTarget);
            }

            var width = (int)(Math.Max(ActualWidth, 1) * DpiScalingFactor);
            var height = (int)(Math.Max(ActualHeight, 1) * DpiScalingFactor);

            CreateGraphicsDeviceDependentResources(new PresentationParameters
            {
                BackBufferWidth = width,
                BackBufferHeight = height,
                MultiSampleCount = sampleCount
            });
        }

        private void CreateGraphicsDeviceDependentResources(PresentationParameters pp)
        {
            var width = pp.BackBufferWidth;
            var height = pp.BackBufferHeight;
            var ms = pp.MultiSampleCount;

            _sharedRenderTarget = new RenderTarget2D(GraphicsDevice, width, height, false, SurfaceFormat.Bgr32,
                    DepthFormat.Depth24Stencil8, 0, RenderTargetUsage.DiscardContents, true);
            _sharedRenderTarget.Name = "sharedRenderTarget";
            _d3D11Image.SetBackBuffer(_sharedRenderTarget);

            // internal rendertarget; all user draws render into this before we draw it to the actual back buffer
            // that way flickering of screen will be prevented when under heavy system load (such as when using rendertargets on intel graphics: https://gitlab.com/MarcStan/MonoGame.Framework.WpfInterop/issues/12)
            // -> always preserve its contents so worst case user gets to see the old screen again
            _cachedRenderTarget = new RenderTarget2D(GraphicsDevice, width, height, false, SurfaceFormat.Bgr32,
                DepthFormat.Depth24Stencil8, ms, RenderTargetUsage.PreserveContents, false);
            _cachedRenderTarget.Name = "cachedRenderTarget";
        }

        private void InitializeImageSource()
        {
            _d3D11Image = new D3D11Image();
            _d3D11Image.IsFrontBufferAvailableChanged += OnIsFrontBufferAvailableChanged;
            CreateBackBuffer();
            Source = _d3D11Image;
            _spriteBatch = new SpriteBatch(GraphicsDevice);
        }

        private void OnIsFrontBufferAvailableChanged(object sender, DependencyPropertyChangedEventArgs eventArgs)
        {
            if (_d3D11Image.IsFrontBufferAvailable)
            {
                StartRendering();
                _resetBackBuffer = true;
            }
            else
            {
                StopRendering();
            }
        }

        public void ForceEnsureCreated()
        {
            OnLoaded(null, null);
        }

        private void OnLoaded(object sender, RoutedEventArgs eventArgs)
        {
            if (IsInDesignMode || _loaded)
                return;

            _loaded = true;

            // get the current window and register Activate/Deactivate
            //var window = LogicalTreeHelperEx.FindParent<Window>(this);
            //if (window == null)
            //    throw new NotSupportedException("The game control does not have a parent window, this is currently not supported");

            //window.Activated += OnWindowActivated;
            //window.Deactivated += OnWindowDeactivated;
            //window.Closed += OnWindowClosed;
            var tabControl = LogicalTreeHelperEx.FindParent<TabControl>(this);
            // if there is any parent that is a tabitem, we must be inside a TabControl

            var windowIsInTab = tabControl != null;
            if (windowIsInTab)
            {
                // also hook up to tab events
                tabControl.SelectionChanged += TabChanged;
            }
            // check whether current window is indeed the active one (usually it will be)
            // but there are some edge cases (programmatically show a tab with this game control after a timer) -> window doesn't have to be active
            //var active = Application.Current.Windows.OfType<Window>().SingleOrDefault(x => x.IsActive);
            //if (active == window && IsVisible)
            {
                IsActive = true;
            }

            if (!GraphicsDeviceInitialized)
            {
                InitializeGraphicsDevice();
                GraphicsDeviceInitialized = true;
            }
            InitializeImageSource();

            // workaround for exceptions in Onloaded being swallowed by default on x64
            // https://stackoverflow.com/questions/4807122/wpf-showdialog-swallowing-exceptions-during-window-load
            try
            {
                // initialize runs in userland
                // user can fuck up anything an have an exception thrown
                // if it throws, we don't want rendering to start
                Initialize();
                StartRendering();
            }
            catch (Exception ex)
            {
                if (Environment.Is64BitOperatingSystem || Environment.Is64BitProcess)
                {
                    // catch and rethrow because WPF just swallows it silently on x64..
                    var deCancerifyWpf = new BackgroundWorker();
                    deCancerifyWpf.DoWork += (e, arg) => { arg.Result = arg.Argument; };
                    deCancerifyWpf.RunWorkerCompleted += (e, arg) =>
                    {
                        // who needs a proper stacktrace anyway
                        // at least we get to see the exception..
                        throw new Exception("Initialize failed, see inner exception for details.", (Exception)arg.Result);
                    };
                    deCancerifyWpf.RunWorkerAsync(ex);
                }
            }
        }

        private void TabChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.Source is TabControl)
            {
                // are we being added or removed?
                var gameParent = LogicalTreeHelperEx.FindParent<TabItem>(this);
                if (e.AddedItems.Contains(gameParent))
                {
                    // added
                    IsActive = true;
                }
                else if (e.RemovedItems.Contains(gameParent))
                {
                    // removed
                    IsActive = false;
                }
            }
        }

        private void OnWindowClosed(object sender, EventArgs e)
        {
            Dispose();
        }

        private void OnWindowActivated(object sender, EventArgs e)
        {
            var tabControl = LogicalTreeHelperEx.FindParent<TabControl>(this);
            // if there is any parent that is a tabitem, we must be inside a TabControl
            // 
            var windowIsInTab = tabControl != null;
            if (windowIsInTab)
            {
                // inside a tab, check whether the tab is active
                var tabItem = LogicalTreeHelperEx.FindParent<TabItem>(this);
                // only set to true for the currently active tab
                IsActive = tabControl.SelectedItem == tabItem;
            }
            else
            {
                // regular control on the window
                IsActive = true;
            }
        }

        private void OnWindowDeactivated(object sender, EventArgs e)
        {
            IsActive = false;
        }

        private void OnRendering(object sender, EventArgs eventArgs)
        {
            if (!_isRendering)
                return;

            if (_toBeDisposedNextFrame.Count > 0)
            {
                DisposeRenderTargetsFromPreviousFrames();
            }

            // Recreate back buffer if necessary.
            if (_resetBackBuffer || _dpiChanged)
            {
                CreateBackBuffer();
                _dpiChanged = false;
            }

            // CompositionTarget.Rendering event may be raised multiple times per frame
            // (e.g. during window resizing).
            // this will be apparent when the last rendering time equals the new argument
            var renderingEventArgs = (RenderingEventArgs)eventArgs;
            if (_lastRenderingTime != renderingEventArgs.RenderingTime)
            {
                // get time since last actual rendering

                var deltaTicks = renderingEventArgs.RenderingTime.Ticks - _lastRenderingTime.Ticks;
                // accumulate until time is greater than target time between frames
                if (deltaTicks >= TargetElapsedTime.Ticks)
                {
                    // enough time has passed to draw a single frame
                    // draw into cache
                    GraphicsDevice.SetRenderTarget(_cachedRenderTarget);
                    Render(new GameTime(renderingEventArgs.RenderingTime, TimeSpan.FromTicks(deltaTicks)));
                    GraphicsDevice.Flush();

                    _lastRenderingTime = renderingEventArgs.RenderingTime;
                }
            }
            else if (_resetBackBuffer)
            {
                // always force render when backbuffer is reset (happens during resize due to size change)
                // if we don't always render it will remain black until next frame is drawn
                // draw into cache
                GraphicsDevice.SetRenderTarget(_cachedRenderTarget);
                Render(new GameTime(renderingEventArgs.RenderingTime, TimeSpan.Zero));
                GraphicsDevice.Flush();
            }

            _d3D11Image.Lock();
            // poor man's swap chain implementation
            // now draw from cache to backbuffer
            GraphicsDevice.SetRenderTarget(_sharedRenderTarget);
            _spriteBatch.Begin();
            _spriteBatch.Draw(_cachedRenderTarget, GraphicsDevice.Viewport.Bounds, Color.White);
            _spriteBatch.End();
            GraphicsDevice.Flush();

            _d3D11Image.Unlock();
            GraphicsDevice.SetRenderTarget(_cachedRenderTarget);
            _d3D11Image.Invalidate(); // Always invalidate D3DImage to reduce flickering
                                      // during window resizing.

            _resetBackBuffer = false;
        }

        private void DisposeRenderTargetsFromPreviousFrames()
        {
            for (var i = 0; i < _toBeDisposedNextFrame.Count; i++)
            {
                _toBeDisposedNextFrame[i]?.Dispose();
                _toBeDisposedNextFrame.RemoveAt(i--);
            }
        }

        protected void StartRendering()
        {
            if (_isRendering)
                return;

            CompositionTarget.Rendering += OnRendering;
            _isRendering = true;
        }

        protected void StopRendering()
        {
            if (!_isRendering)
                return;

            CompositionTarget.Rendering -= OnRendering;
            _isRendering = false;
        }

        private void UnitializeImageSource()
        {
            Source = null;

            if (_d3D11Image != null)
            {
                _d3D11Image.IsFrontBufferAvailableChanged -= OnIsFrontBufferAvailableChanged;
                _d3D11Image.Dispose();
                _d3D11Image = null;
            }
            if (_sharedRenderTarget != null)
            {
                _sharedRenderTarget.Dispose();
                _sharedRenderTarget = null;
            }
            if (_cachedRenderTarget != null)
            {
                if (_cachedRenderTarget.MultiSampleCount > 0 && UseASingleSharedGraphicsDevice)
                {
                    // TODO: this is a memoryleak, the code is intentional because Dispose actually crashes Monogame when using a shared graphicsdevice and disposing MSAA enabled rendertargets
                    // at the very latest this will happen on window close, for SPA this is fine as the process will shut down
                    // but if your editor has multiple windows (or tabs) that can be created/closed multiple times this will slowly increase memory usage..
                }
                else
                {
                    _cachedRenderTarget.Dispose();
                }
                _cachedRenderTarget = null;
            }
        }
    }
}
