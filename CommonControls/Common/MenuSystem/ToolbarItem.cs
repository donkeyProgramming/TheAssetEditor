using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace CommonControls.Common.MenuSystem
{
    public class ToolbarItem
    {
        public NotifyAttr<string> NameAttribute { get; set; } = new NotifyAttr<string>("");
        public ObservableCollection<ToolbarItem> Children { get; set; } = new ObservableCollection<ToolbarItem>();

        string _name;
        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                UpdateName();
            }
        }

        MenuAction _action;
        public MenuAction Action
        {
            get => _action;
            set
            {
                _action = value;
                UpdateName();
            }
        }

        public bool IsSeparator { get; set; } = false;

        public void UpdateName()
        {
            if (Action != null)
                NameAttribute.Value = Name + Action.ToopTipText();
            else
                NameAttribute.Value = Name;
        }
    }
}
