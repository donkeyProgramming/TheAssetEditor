// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.ObjectModel;
using Shared.Core.Misc;

namespace Shared.Ui.Common.MenuSystem
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
