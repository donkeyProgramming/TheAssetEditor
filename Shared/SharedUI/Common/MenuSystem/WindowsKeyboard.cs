using System.Windows.Input;

namespace Shared.Ui.Common.MenuSystem
{
    public interface IKeyboardHandler
    {
        bool OnKeyReleased(Key key, Key systemKey, ModifierKeys modifierKeys);
        void OnKeyDown(Key key, Key systemKey, ModifierKeys modifiers);
    }

    public interface IWindowsKeyboard
    {
        bool IsKeyDown(Key key);
    }

    public class WindowKeyboard : IWindowsKeyboard
    {
        public bool IsKeyDown(Key key)
        {
            return Keyboard.IsKeyDown(key);
        }
    }
}
