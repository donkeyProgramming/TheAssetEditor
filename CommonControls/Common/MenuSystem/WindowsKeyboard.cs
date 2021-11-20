using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;

namespace CommonControls.Common.MenuSystem
{
    public interface IKeyboardHandler
    {
        bool OnKeyReleased(Key key, Key systemKey, ModifierKeys modifierKeys);
        void OnKeyDown(Key key, Key systemKey, ModifierKeys modifiers);
    }

    public class WindowKeyboard
    {
        Dictionary<Key, bool> _isKeyDown = new Dictionary<Key, bool>();
        public bool IsKeyDown(Key key)
        {
            _isKeyDown.TryGetValue(key, out var value);
            return value;
        }

        public void SetKeyDown(Key key, bool status)
        {
            _isKeyDown[key] = status;
        }
    }
}
