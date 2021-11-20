using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;

namespace CommonControls.Common.MenuSystem
{
    public class ActionHotkeyHandler
    {
        List<MenuAction> _actions = new List<MenuAction>();

        public void Register(MenuAction action)
        {
            _actions.Add(action);
        }


        public bool TriggerCommand(Key key, ModifierKeys modifierKeys)
        {
            bool isHandled = false;
            foreach (var item in _actions)
            {
                if (item.Hotkey == null)
                    continue;

                if (item.Hotkey.Key == key && item.Hotkey.ModifierKeys == modifierKeys)
                {
                    item.TriggerAction();
                    isHandled = true;
                }
            }

            return isHandled;
        }


    }
}
