using System.Windows.Media.Imaging;
using Shared.Core.Misc;

namespace Shared.Ui.Common.MenuSystem
{

    public class MenuBarButton
    {
        public NotifyAttr<bool> IsVisible { get; set; } = new NotifyAttr<bool>(true);
        public BitmapImage? Image { get; set; }
        public MenuAction? Action { get; set; }
        public ButtonVisibilityRule ShowRule { get; set; } = ButtonVisibilityRule.Always;
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
