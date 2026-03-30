using GameWorld.Core.WpfWindow.Input;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Serilog;
using Shared.Core.ErrorHandling;
using Shared.Core.Services;
using System;

namespace GameWorld.Core.Components.Input
{
    public enum MouseButton
    {
        Left,
        Right,
        Middle,
    }

    /// <summary>
    /// Cursor types for modal transform operations (Blender-style)
    /// </summary>
    public enum ModalCursorType
    {
        Default,
        Move,       // Four arrows (G key - translate)
        Rotate,     // Circular arrows (R key - rotate)
        Scale,      // Diagonal arrows (S key - scale)
    }

    public interface IMouseComponent : IGameComponent
    {
        int DeletaScrollWheel();
        Vector2 DeltaPosition();
        Vector2 Position();

        bool IsMouseButtonDown(MouseButton button);
        bool IsMouseButtonPressed(MouseButton button);
        bool IsMouseButtonReleased(MouseButton button);

        bool IsMouseOwner(IGameComponent component);
        IGameComponent MouseOwner { get; set; }

        void ClearStates();
        MouseState State();
        MouseState LastState();

        void Update(GameTime t);

        // Infinite drag support (Blender-style cursor wrapping)
        void SetCursorPosition(int x, int y);
        Vector2 GetScreenSize();

        // Cursor appearance for modal transforms
        void SetModalCursor(ModalCursorType cursorType);
        void ResetCursor();
    }

    public class MouseComponent : BaseComponent, IDisposable, IMouseComponent
    {
        readonly ILogger _logger = Logging.Create<MouseComponent>();

        MouseState _currentMouseState;
        MouseState _lastMousesState;
        WpfMouse _wpfMouse;

        IGameComponent _mouseOwner;
        public IGameComponent MouseOwner
        {
            get { return _mouseOwner; }
            set
            {
                if (_mouseOwner != value)
                {
                    if (_mouseOwner != null && value != null)
                    {
                        var error = $"{value.GetType().Name} is trying to steal ownership {_mouseOwner.GetType().Name}";
                        _logger.Here().Error(error);
                        throw new Exception(error);
                    }

                    _mouseOwner = value;
                }
            }
        }

        public MouseComponent(IWpfGame game)
        {
            _wpfMouse = new WpfMouse(game, true);
            UpdateOrder = (int)ComponentUpdateOrderEnum.Input;
        }

        public bool IsMouseOwner(IGameComponent component)
        {
            if (MouseOwner == null || MouseOwner == component)
                return true;
            return false;
        }

        public override void Update(GameTime t)
        {
            var currentState = _wpfMouse.GetState();

            _lastMousesState = _currentMouseState;
            _currentMouseState = currentState;

            Console.WriteLine("Mouse" + _currentMouseState.LeftButton + " - " + _lastMousesState.LeftButton);

            if (_lastMousesState == null)
                _lastMousesState = currentState;
        }

        public bool IsMouseButtonReleased(MouseButton button)
        {
            switch (button)
            {
                case MouseButton.Left:
                    return _lastMousesState.LeftButton == ButtonState.Pressed && _currentMouseState.LeftButton == ButtonState.Released;
                case MouseButton.Right:
                    return _lastMousesState.RightButton == ButtonState.Pressed && _currentMouseState.RightButton == ButtonState.Released;
                case MouseButton.Middle:
                    return _lastMousesState.MiddleButton == ButtonState.Pressed && _currentMouseState.MiddleButton == ButtonState.Released;
            }

            throw new NotImplementedException("trying to use a mouse button which is not added");
        }

        public bool IsMouseButtonDown(MouseButton button)
        {
            switch (button)
            {
                case MouseButton.Left:
                    return _currentMouseState.LeftButton == ButtonState.Pressed;
                case MouseButton.Right:
                    return _currentMouseState.RightButton == ButtonState.Pressed;
                case MouseButton.Middle:
                    return _currentMouseState.MiddleButton == ButtonState.Pressed;
            }

            throw new NotImplementedException("trying to use a mouse button which is not added");
        }

        public bool IsMouseButtonPressed(MouseButton button)
        {
            switch (button)
            {
                case MouseButton.Left:
                    return _currentMouseState.LeftButton == ButtonState.Pressed && _lastMousesState.LeftButton == ButtonState.Released;
                case MouseButton.Right:
                    return _currentMouseState.RightButton == ButtonState.Pressed && _lastMousesState.RightButton == ButtonState.Released;
                case MouseButton.Middle:
                    return _currentMouseState.MiddleButton == ButtonState.Pressed && _lastMousesState.MiddleButton == ButtonState.Released;
            }

            throw new NotImplementedException("trying to use a mouse button which is not added");
        }

        public Vector2 Position()
        {
            return new Vector2(_currentMouseState.X, _currentMouseState.Y);
        }

        public Vector2 DeltaPosition()
        {
            var lastPos = new Vector2(_lastMousesState.X, _lastMousesState.Y);
            return lastPos - Position();
        }

        public int DeletaScrollWheel()
        {
            return _lastMousesState.ScrollWheelValue - _currentMouseState.ScrollWheelValue;
        }

        public MouseState State() { return _currentMouseState; }
        public MouseState LastState() { return _lastMousesState; }

        /// <summary>
        /// Set cursor position in window coordinates (for infinite drag)
        /// </summary>
        public void SetCursorPosition(int x, int y)
        {
            _wpfMouse.SetCursor(x, y);
        }

        /// <summary>
        /// Get the screen/render area size for infinite drag calculations
        /// </summary>
        public Vector2 GetScreenSize()
        {
            return _wpfMouse.GetElementSize();
        }

        /// <summary>
        /// Set cursor appearance for modal transform (Blender-style)
        /// </summary>
        public void SetModalCursor(ModalCursorType cursorType)
        {
            System.Windows.Input.Cursor cursor = cursorType switch
            {
                ModalCursorType.Move => System.Windows.Input.Cursors.SizeAll,      // Four-way arrows
                ModalCursorType.Rotate => System.Windows.Input.Cursors.Hand,       // Hand for rotation (closest to circular)
                ModalCursorType.Scale => System.Windows.Input.Cursors.SizeNWSE,    // Diagonal arrows for scale
                _ => System.Windows.Input.Cursors.Arrow
            };
            _wpfMouse.SetCursorType(cursor);
        }

        /// <summary>
        /// Reset cursor to default arrow
        /// </summary>
        public void ResetCursor()
        {
            _wpfMouse.ResetCursor();
        }

        public void ClearStates()
        {
            // Preserve scroll wheel value to prevent sudden zoom when ClearStates is called
            // after user has scrolled (see Camera.cs line 226-233)
            var scrollWheelValue = _currentMouseState.ScrollWheelValue;
            _currentMouseState = new MouseState(0, 0, scrollWheelValue, ButtonState.Released, ButtonState.Released, ButtonState.Released, ButtonState.Released, ButtonState.Released);
            _lastMousesState = new MouseState(0, 0, scrollWheelValue, ButtonState.Released, ButtonState.Released, ButtonState.Released, ButtonState.Released, ButtonState.Released);
        }

        public void Dispose()
        {
            _wpfMouse?.Dispose();
            _wpfMouse = null;
            _mouseOwner = null;
        }
    }
}
