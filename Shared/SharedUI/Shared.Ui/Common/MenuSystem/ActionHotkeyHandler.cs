using System.Collections.Generic;
using System.Windows.Input;

namespace Shared.Ui.Common.MenuSystem
{
    public class ActionHotkeyHandler
    {
        readonly List<MenuAction> _actions = new();

        public void Register(MenuAction action)
        {
            _actions.Add(action);
        }

        public bool TriggerCommand(Key key, ModifierKeys modifierKeys)
        {
            var isHandled = false;
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
