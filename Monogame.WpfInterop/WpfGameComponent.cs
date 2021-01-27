using Microsoft.Xna.Framework;
using System;

namespace MonoGame.Framework.WpfInterop
{
    /// <summary>
    /// A game component much like the original, but compatible with <see cref="WpfGame"/>.
    /// </summary>
    public class WpfGameComponent : IGameComponent, IUpdateable
    {
        #region Fields

        private bool _enabled = true;
        private int _updateOrder;

        #endregion

        #region Constructors

        public WpfGameComponent(WpfGame game)
        {
            Game = game;
        }

        #endregion

        #region Events

        public event EventHandler<EventArgs> EnabledChanged;

        public event EventHandler<EventArgs> UpdateOrderChanged;

        #endregion

        #region Properties

        public WpfGame Game { get; }

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

        public virtual void Initialize() { }

        public virtual void Update(GameTime gameTime) { }

        #endregion
    }
}