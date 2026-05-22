using Microsoft.Xna.Framework;
using Shared.Core.Misc;
using System;

namespace GameWorld.Core.Components
{

    public class BaseComponent : NotifyPropertyChangedImpl, IDrawable, IGameComponent, IUpdateable
    {
        #region Fields
        private bool _visible = true;
        private int _drawOrder = (int)ComponentDrawOrderEnum.Default;
        private bool _initialized;
        public bool Isinitialized { get => _initialized; }
        private bool _enabled = true;
        private int _updateOrder = (int)ComponentUpdateOrderEnum.Default;
        #endregion

        #region Events
        public event EventHandler<EventArgs> DrawOrderChanged;

        public event EventHandler<EventArgs> VisibleChanged;

        public event EventHandler<EventArgs> EnabledChanged;

        public event EventHandler<EventArgs> UpdateOrderChanged;
        #endregion

        public BaseComponent()
        {
        }

        #region Properties
        public int DrawOrder
        {
            get { return _drawOrder; }
            set
            {
                if (_drawOrder == value)
                    return;
                _drawOrder = value;
                var ev = DrawOrderChanged;
                ev?.Invoke(this, EventArgs.Empty);
            }
        }

        public bool Visible
        {
            get { return _visible; }
            set
            {
                if (_visible == value)
                    return;
                _visible = value;
                var ev = VisibleChanged;
                ev?.Invoke(this, EventArgs.Empty);
            }
        }

        public bool Enabled
        {
            get { return _enabled; }
            set
            {
                if (_enabled == value)
                    return;

                _enabled = value;
                var ev = EnabledChanged;
                ev?.Invoke(this, EventArgs.Empty);
            }
        }

        public int UpdateOrder
        {
            get { return _updateOrder; }
            set
            {
                if (_updateOrder == value)
                    return;

                _updateOrder = value;
                var ev = UpdateOrderChanged;
                ev?.Invoke(this, EventArgs.Empty);
            }
        }

        #endregion

        #region Methods

        public virtual void Initialize()
        {
            if (!_initialized)
            {
                _initialized = true;
                LoadContent();
            }
        }

        public virtual void Draw(GameTime gameTime) { }

        protected virtual void LoadContent() { }

        public virtual void Update(GameTime gameTime) { }

        #endregion
    }
}
