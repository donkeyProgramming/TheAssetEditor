using GameWorld.Core.WpfWindow.Input;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Shared.Core.Services;

namespace GameWorld.Core.Components.Input
{
    public interface IKeyboardComponent : IGameComponent
    {
        bool IsKeyComboReleased(Keys key, Keys modificationKey);
        bool IsKeyDown(Keys key);
        bool IsKeyDownOrReleased(Keys key);
        bool IsKeyReleased(Keys key);
        bool IsKeyUp(Keys key);
        void Update(GameTime t);
    }

    public class KeyboardComponent : BaseComponent, IKeyboardComponent
    {
        KeyboardState _currentKeyboardState;
        KeyboardState _lastKeyboardState;
        WpfKeyboard _wpfKeyboard;

        public KeyboardComponent(IWpfGame game)
        {
            _wpfKeyboard = new WpfKeyboard(game);
            UpdateOrder = (int)ComponentUpdateOrderEnum.Input;
        }

        public override void Update(GameTime t)
        {
            var keyboardState = _wpfKeyboard.GetState();

            _lastKeyboardState = _currentKeyboardState;
            _currentKeyboardState = keyboardState;

            if (_lastKeyboardState == null)
                _lastKeyboardState = keyboardState;
        }

        public bool IsKeyReleased(Keys key)
        {
            var currentUp = _currentKeyboardState.IsKeyUp(key);
            var lastDown = _lastKeyboardState.IsKeyDown(key);
            return currentUp && lastDown;
        }

        public bool IsKeyComboReleased(Keys key, Keys modificationKey)
        {
            var value = IsKeyReleased(key) && IsKeyDownOrReleased(modificationKey);
            return value;
        }

        public bool IsKeyDown(Keys key)
        {
            return _currentKeyboardState.IsKeyDown(key);
        }

        public bool IsKeyDownOrReleased(Keys key)
        {
            return _currentKeyboardState.IsKeyDown(key) || IsKeyReleased(key);
        }

        public bool IsKeyUp(Keys key)
        {
            return _currentKeyboardState.IsKeyUp(key);
        }
    }
}
