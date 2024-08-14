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
        public void ClearStates()
        {
            _currentMouseState = new MouseState();
            _lastMousesState = new MouseState();
        }

        public void Dispose()
        {
            _wpfMouse?.Dispose();
            _wpfMouse = null;
            _mouseOwner = null;
        }
    }
}
