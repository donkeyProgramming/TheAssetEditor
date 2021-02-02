using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using MonoGame.Framework.WpfInterop;
using MonoGame.Framework.WpfInterop.Input;
using System;
using System.Collections.Generic;
using System.Text;

namespace View3D.Input
{
    public enum MouseButton
    {
        Left,
        Right,
    }

    public class MouseComponent : BaseComponent
    {
        MouseState  _currentMouseState;
        MouseState _lastMousesState;
        WpfMouse _wpfMouse;

        public MouseComponent(WpfGame game) : base(game)
        {
           _wpfMouse = new WpfMouse(game);
           _wpfMouse.CaptureMouseWithin = true;
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

        public bool IsMouseButtonPressed(MouseButton button)
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

    }
}
