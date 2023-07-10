// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
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
