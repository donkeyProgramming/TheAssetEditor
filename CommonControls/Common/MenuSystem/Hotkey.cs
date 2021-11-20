using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;

namespace CommonControls.Common.MenuSystem
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
