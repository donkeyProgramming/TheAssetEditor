using System.Collections.ObjectModel;
using System.Windows.Media.Imaging;
using Editors.KitbasherEditor.Core.MenuBarViews;
using Shared.Ui.Common.MenuSystem;

namespace KitbasherEditor.ViewModels.MenuBarViews.Helpers
{
    public class ButtonBuilder
    {
        private readonly ObservableCollection<MenuBarButton> _menuBarButtons = new ObservableCollection<MenuBarButton>();
        private readonly Dictionary<Type, MenuAction> _actionList = new();

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
