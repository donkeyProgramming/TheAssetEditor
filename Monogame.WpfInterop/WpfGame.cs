using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using SharedCore.Events;
using System;
using System.Collections.Generic;


namespace MonoGame.Framework.WpfInterop
{
    public interface IComponentManager
    {
        public T GetComponent<T>() where T : IGameComponent;
        public T AddComponent<T>(T comp) where T : IGameComponent;
        public void RemoveComponent<T>(T comp) where T : IGameComponent;
    }

    public class SceneInitializedEvent
    {
    }

    /// <summary>
    /// The replacement for <see cref="Game"/>. Unlike <see cref="Game"/> the <see cref="WpfGame"/> is a WPF control and can be hosted inside WPF windows.
    /// </summary>
    public abstract class WpfGame : D3D11Host, IComponentManager
    {
        private readonly EventHub _eventHub;
        private readonly string _contentDir;

        #region Fields

        private ContentManager _content;
        private readonly List<IUpdateable> _sortedUpdateables;
        private readonly List<IDrawable> _sortedDrawables;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new instance of a game host panel.
        /// </summary>
        protected WpfGame(EventHub eventHub, string contentDir)
        {
            if (string.IsNullOrEmpty(contentDir))
                throw new ArgumentNullException(nameof(contentDir));
            _eventHub = eventHub;
            _contentDir = contentDir;

            Focusable = true;
            Components = new GameComponentCollection();
            _sortedDrawables = new List<IDrawable>();
            _sortedUpdateables = new List<IUpdateable>();
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets whether this instance takes focus instantly on mouse over.
        /// If set to false, the user must click into the game panel to gain focus.
        /// This applies to both <see cref="MonoGame.Framework.WpfInterop.Input.WpfMouse"/> and <see cref="MonoGame.Framework.WpfInterop.Input.WpfKeyboard"/> behaviour.
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

        #endregion

        #region Methods

        /// <summary>
        /// Dispose is called to dispose of resources.
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            foreach (var c in Components)
            {
                var disp = c as IDisposable;
                disp?.Dispose();
            }
            Components.ComponentAdded -= ComponentAdded;
            Components.ComponentRemoved -= ComponentRemoved;
            Components.Clear();

            Services.RemoveService(typeof(IGraphicsDeviceService));
            Services.RemoveService(typeof(IGraphicsDeviceManager));

            UnloadContent();
            _sortedUpdateables.Clear();
            _sortedDrawables.Clear();

            Content.Unload();
            Content?.Dispose();
            Content = null;
        }

        /// <summary>
        /// The draw method that is called to render your scene.
        /// </summary>
        /// <param name="gameTime"></param>
        protected virtual void Draw(GameTime gameTime)
        {
            for (int i = 0; i < _sortedDrawables.Count; i++)
            {
                if (_sortedDrawables[i].Visible)
                    _sortedDrawables[i].Draw(gameTime);
            }
        }

        /// <summary>
        /// Initialize is called once when the control is created.
        /// </summary>
        protected override void Initialize()
        {
            base.Initialize();
            Content = new ContentManager(Services, _contentDir);

            // hook events now (graphics, etc. is now loaded)
            // any components added prior we insert manually
            foreach (var c in Components)
            {
                ComponentAdded(this, new GameComponentCollectionEventArgs(c));
            }
            Components.ComponentAdded += ComponentAdded;
            Components.ComponentRemoved += ComponentRemoved;

            LoadContent();
            _eventHub.Publish(new SceneInitializedEvent());
        }

        /// <summary>
        /// Load content is called once by <see cref="Initialize()"/>.
        /// </summary>
        protected virtual void LoadContent()
        {

        }

        /// <summary>
        /// Internal method used to integrate <see cref="Update"/> and <see cref="Draw"/> with the WPF control.
        /// </summary>
        /// <param name="time"></param>
        protected sealed override void Render(GameTime time)
        {
            // just run as fast as possible, WPF itself is limited to 60 FPS so that's the max we will get
            Update(time);
            Draw(time);
        }

        /// <summary>
        /// Unload content is called once when the control is destroyed.
        /// </summary>
        protected virtual void UnloadContent()
        {
        }

        /// <summary>
        /// The update method that is called to update your game logic.
        /// </summary>
        /// <param name="gameTime"></param>
        protected virtual void Update(GameTime gameTime)
        {
            for (int i = 0; i < _sortedUpdateables.Count; i++)
            {
                if (_sortedUpdateables[i].Enabled)
                    _sortedUpdateables[i].Update(gameTime);
            }
        }

        private void ComponentRemoved(object sender, GameComponentCollectionEventArgs args)
        {
            var update = args.GameComponent as IUpdateable;
            if (update != null)
            {
                update.UpdateOrderChanged -= UpdateOrderChanged;
                _sortedUpdateables.Remove(update);
            }
            var draw = args.GameComponent as IDrawable;
            if (draw != null)
            {
                draw.DrawOrderChanged -= DrawOrderChanged;
                _sortedDrawables.Remove(draw);
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
                _sortedUpdateables.Add(update);
                update.UpdateOrderChanged += UpdateOrderChanged;
                SortUpdatables();
            }
            var draw = args.GameComponent as IDrawable;
            if (draw != null)
            {
                _sortedDrawables.Add(draw);
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
            _sortedDrawables.Sort((a, b) => a.DrawOrder.CompareTo(b.DrawOrder));
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
            _sortedUpdateables.Sort((a, b) => a.UpdateOrder.CompareTo(b.UpdateOrder));
        }



        #endregion
    }
}
