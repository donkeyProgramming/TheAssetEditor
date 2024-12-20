using System;
using System.Collections.Generic;
using System.Windows;
using GameWorld.Core.Services;
using GameWorld.Core.WpfWindow.Events;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Shared.Core.Events;
using Shared.Core.Services;

namespace GameWorld.Core.WpfWindow
{

    /// <summary>
    /// The replacement for <see cref="Game"/>. Unlike <see cref="Game"/> the <see cref="WpfGame"/> is a WPF control and can be hosted inside WPF windows.
    /// </summary>
    public class WpfGame : D3D11Host, IWpfGame
    {
        WpfGraphicsDeviceService _deviceServiceHandle;
        private readonly ResourceLibrary _resourceLibrary;
        private readonly IStandardDialogs _exceptionService;
        private readonly IEventHub _eventHub;
        private readonly string _contentDir;

        private ContentManager _content;
        private readonly List<IUpdateable> _sortedUpdateable;
        private readonly List<IDrawable> _sortedDrawable;


        public FrameworkElement GetFocusElement() { return this; }

        /// <summary>
        /// Creates a new instance of a game host panel.
        /// </summary>
        public WpfGame(ResourceLibrary resourceLibrary, IStandardDialogs exceptionService, IEventHub eventHub, string contentDir = "Content")
        {
            if (string.IsNullOrEmpty(contentDir))
                throw new ArgumentNullException(nameof(contentDir));
            _resourceLibrary = resourceLibrary;
            _exceptionService = exceptionService;
            _eventHub = eventHub;
            _contentDir = contentDir;

            Focusable = true;
            Components = new GameComponentCollection();
            _sortedDrawable = new List<IDrawable>();
            _sortedUpdateable = new List<IUpdateable>();
        }

        /// <summary>
        /// Gets or sets whether this instance takes focus instantly on mouse over.
        /// If set to false, the user must click into the game panel to gain focus.
        /// This applies to both <see cref="Input.WpfMouse"/> and <see cref="Input.WpfKeyboard"/> behaviour.
        /// Defaults to true.
        /// </summary>
        public bool FocusOnMouseOver { get; set; } = false;

        /// <summary>
        /// Mirrors the game component collection behaviour of monogame.
        /// </summary>
        public GameComponentCollection Components { get; }

        /// <summary>
        /// The content manager for this game.
        /// </summary>
        public ContentManager Content
        {
            get { return _content; }
            set
            {
                _content = value;
            }
        }

        /// <summary>
        /// Dispose is called to dispose of resources.
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            _deviceServiceHandle = null;

            _sortedUpdateable.Clear();
            _sortedDrawable.Clear();

            // Should we ever need to dispose of the components?
            // They should always be created though DI, so DI should dispose them
            foreach (var c in Components)
            {
                var disposable = c as IDisposable;
                disposable?.Dispose();
            }
            Components.ComponentAdded -= ComponentAdded;
            Components.ComponentRemoved -= ComponentRemoved;
            Components.Clear();

            Services.RemoveService(typeof(IGraphicsDeviceService));
            Services.RemoveService(typeof(IGraphicsDeviceManager));
        }

        /// <summary>
        /// The draw method that is called to render your scene.
        /// </summary>
        /// <param name="gameTime"></param>
        protected virtual void Draw(GameTime gameTime)
        {
            for (var i = 0; i < _sortedDrawable.Count; i++)
            {
                if (_sortedDrawable[i].Visible)
                    _sortedDrawable[i].Draw(gameTime);
            }
        }

        protected override void OnGraphicDeviceDisposed()
        {
            _eventHub.PublishGlobalEvent(new GraphicDeviceDisposedEvent());
            _resourceLibrary.Reset();
        }

        /// <summary>
        /// Initialize is called once when the control is created.
        /// </summary>
        protected override void Initialize()
        {
            _deviceServiceHandle = new WpfGraphicsDeviceService(this);
            base.Initialize();

            Content = new ContentManager(Services, _contentDir);
            _resourceLibrary.Initialize(GraphicsDevice, Content);

            foreach (var c in Components)
                ComponentAdded(this, new GameComponentCollectionEventArgs(c));

            Components.ComponentAdded += ComponentAdded;
            Components.ComponentRemoved += ComponentRemoved;

            _eventHub.Publish(new SceneInitializedEvent());
        }

        /// <summary>
        /// Internal method used to integrate <see cref="Update"/> and <see cref="Draw"/> with the WPF control.
        /// </summary>
        /// <param name="time"></param>
        protected sealed override void Render(GameTime time)
        {
            try
            {
                // just run as fast as possible, WPF itself is limited to 60 FPS so that's the max we will get
                Update(time);
                Draw(time);
            }

            catch(Exception ex) 
            {
                StopRendering();
                _exceptionService.ShowExceptionWindow(ex);
                StartRendering();
            }
        }


        /// <summary>
        /// The update method that is called to update your game logic.
        /// </summary>
        /// <param name="gameTime"></param>
        protected virtual void Update(GameTime gameTime)
        {
            for (var i = 0; i < _sortedUpdateable.Count; i++)
            {
                if (_sortedUpdateable[i].Enabled)
                    _sortedUpdateable[i].Update(gameTime);
            }
        }

        private void ComponentRemoved(object sender, GameComponentCollectionEventArgs args)
        {
            var update = args.GameComponent as IUpdateable;
            if (update != null)
            {
                update.UpdateOrderChanged -= UpdateOrderChanged;
                _sortedUpdateable.Remove(update);
            }
            var draw = args.GameComponent as IDrawable;
            if (draw != null)
            {
                draw.DrawOrderChanged -= DrawOrderChanged;
                _sortedDrawable.Remove(draw);
            }
        }

        private void ComponentAdded(object sender, GameComponentCollectionEventArgs args)
        {
            // monogame also calls initialize
            // I would have assumed that there'd be some property IsInitialized to prevent multiple calls to Initialize, but there isn't
            args.GameComponent.Initialize();
            var update = args.GameComponent as IUpdateable;
            if (update != null)
            {
                _sortedUpdateable.Add(update);
                update.UpdateOrderChanged += UpdateOrderChanged;
                SortUpdatables();
            }
            var draw = args.GameComponent as IDrawable;
            if (draw != null)
            {
                _sortedDrawable.Add(draw);
                draw.DrawOrderChanged += DrawOrderChanged;
                SortDrawables();
            }
        }
        public T GetComponent<T>() where T : IGameComponent
        {
            var type = typeof(T);
            foreach (var comp in Components)
            {
                if (comp.GetType() == type)
                    return (T)comp;
                if (type.IsAssignableFrom(comp.GetType()))
                    return (T)comp;
            }

            return default;
        }

        public T AddComponent<T>(T comp) where T : IGameComponent
        {
            Components.Add(comp);
            return comp;
        }

        public void RemoveComponent<T>(T comp) where T : IGameComponent
        {
            Components.Remove(comp);
        }

        private void SortDrawables()
        {
            _sortedDrawable.Sort((a, b) => a.DrawOrder.CompareTo(b.DrawOrder));
        }

        private void DrawOrderChanged(object sender, EventArgs e)
        {
            SortDrawables();
        }

        private void UpdateOrderChanged(object sender, EventArgs eventArgs)
        {
            SortUpdatables();
        }

        private void SortUpdatables()
        {
            _sortedUpdateable.Sort((a, b) => a.UpdateOrder.CompareTo(b.UpdateOrder));
        }
    }
}
