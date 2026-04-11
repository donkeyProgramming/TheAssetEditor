using System.Windows.Input;
using Shared.Ui.Common.MenuSystem;

namespace Test.TestingUtility.Shared
{


    public class TestKeyboard : IWindowsKeyboard
    {
        readonly Dictionary<Key, bool> _isKeyDown = new();
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
