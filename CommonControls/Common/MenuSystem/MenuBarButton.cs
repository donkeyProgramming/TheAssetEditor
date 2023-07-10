// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Windows.Media.Imaging;

namespace CommonControls.Common.MenuSystem
{

    public class MenuBarButton
    {
        public NotifyAttr<bool> IsVisible { get; set; } = new NotifyAttr<bool>(true);
        public BitmapImage Image { get; set; }
        public MenuAction Action { get; set; }
        public ButtonVisabilityRule ShowRule { get; set; } = ButtonVisabilityRule.Always;
        public bool IsSeperator { get; set; } = false;

        public MenuBarButton(MenuAction action)
        {
            Action = action;
        }
    }

    public class MenuBarGroupButton : MenuBarButton
    {
        public string GroupName { get; set; } = "";
        public NotifyAttr<bool> IsChecked { get; set; } = new NotifyAttr<bool>(false);

        public MenuBarGroupButton(MenuAction action, string groupName, bool isChecked = false) : base(action)
        {
            GroupName = groupName;
            IsChecked.Value = isChecked;
            action.ActionTriggeredCallback = OnActionTriggered;
        }

        void OnActionTriggered() => IsChecked.Value = true;
    }
}
