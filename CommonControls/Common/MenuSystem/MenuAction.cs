// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;

namespace CommonControls.Common.MenuSystem
{
    public class MenuAction
    {
        public Hotkey Hotkey { get; set; }
        public ICommand Command { get; set; }
        public NotifyAttr<string> ToolTipAttribute { get; set; } = new NotifyAttr<string>();
        public ActionEnabledRule EnableRule { get; set; }
        public NotifyAttr<bool> IsActionEnabled { get; set; } = new NotifyAttr<bool>(true);

        public void TriggerAction()
        {
            if (ActionTriggeredCallback != null)
                ActionTriggeredCallback();

            _func();
        }

        public Action ActionTriggeredCallback { get; set; }
        Action _func { get; set; }

        public MenuAction(Action function)
        {
            _func = function;
            Command = new RelayCommand(TriggerAction);
        }

        public string _toopTipText;
        public string ToolTip
        {
            set
            {
                _toopTipText = value;
                UpdateToolTip();
            }
        }

        public string ToopTipText()
        {
            if (Hotkey == null)
                return "";

            if (Hotkey.ModifierKeys == ModifierKeys.None)
                return $" ({Hotkey.Key})";
            return $" ({Hotkey.Key} + {Hotkey.ModifierKeys})";
        }

        public void UpdateToolTip()
        {
            ToolTipAttribute.Value = _toopTipText + ToopTipText();
        }
    }
}
