using Shared.Ui.Common.MenuSystem;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Media.Imaging;

namespace KitbasherEditor.ViewModels.MenuBarViews.Helpers
{
    public class ButtonBuilder
    {
        ObservableCollection<MenuBarButton> _menuBarButtons = new ObservableCollection<MenuBarButton>();
        Dictionary<Type, MenuAction> _actionList = new();

        public ButtonBuilder(Dictionary<Type, MenuAction> actionList)
        {
            _actionList = actionList;
        }

        public ObservableCollection<MenuBarButton> Build() => _menuBarButtons;


        public void CreateButton<T>(BitmapImage image, ButtonVisibilityRule buttonVisibilityRule = ButtonVisibilityRule.Always) where T : IKitbasherUiCommand
        {
            var action = GetMenuAction<T>();
            _menuBarButtons.Add(new MenuBarButton(action) { Image = image, ShowRule = buttonVisibilityRule });
        }

        public void CreateGroupedButton<T>(string groupName, bool isChecked, BitmapImage image) where T : IKitbasherUiCommand
        {
            var action = GetMenuAction<T>();
            _menuBarButtons.Add(new MenuBarGroupButton(action, groupName, isChecked) { Image = image });
        }

        public void CreateButtonSeparator()
        {
            _menuBarButtons.Add(new MenuBarButton(null) { IsSeperator = true });
        }

        MenuAction GetMenuAction<T>() where T : IKitbasherUiCommand
        {
            return _actionList.First(x => x.Key == typeof(T)).Value;
        }
    }
}
