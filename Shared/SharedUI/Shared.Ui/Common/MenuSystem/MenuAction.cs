using System;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Shared.Core.Misc;

namespace Shared.Ui.Common.MenuSystem
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
            TriggerInternal();

        }

        public virtual void TriggerInternal()
        { }

        public Action ActionTriggeredCallback { get; set; }


        public MenuAction()
        {
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
