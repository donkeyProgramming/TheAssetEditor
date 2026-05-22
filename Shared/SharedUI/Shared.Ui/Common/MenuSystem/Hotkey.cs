using System.Windows.Input;

namespace Shared.Ui.Common.MenuSystem
{
    public class Hotkey
    {
        public ModifierKeys ModifierKeys { get; set; }
        public Key Key { get; set; }

        public Hotkey(Key key, ModifierKeys modifierKeys)
        {
            Key = key;
            ModifierKeys = modifierKeys;
        }
    }
}
