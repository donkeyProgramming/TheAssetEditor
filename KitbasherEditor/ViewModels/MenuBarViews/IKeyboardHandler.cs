using System.Collections.Generic;
using System.Windows.Input;

namespace KitbasherEditor.ViewModels.MenuBarViews
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
