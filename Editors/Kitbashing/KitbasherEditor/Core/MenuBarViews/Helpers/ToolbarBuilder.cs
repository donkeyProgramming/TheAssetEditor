using System.Collections.ObjectModel;
using Editors.KitbasherEditor.Core.MenuBarViews;
using Shared.Ui.Common.MenuSystem;

namespace KitbasherEditor.ViewModels.MenuBarViews.Helpers
{
    public class ToolbarBuilder
    {
        ObservableCollection<ToolbarItem> _toolBarItems { get; set; } = new();
        private readonly Dictionary<Type, MenuAction> _actionList = new();

        public ToolbarBuilder(Dictionary<Type, MenuAction> actionList)
        {
            _actionList = actionList;
        }

        public ObservableCollection<ToolbarItem> Build() => _toolBarItems;


        public void CreateToolBarItem<T>(ToolbarItem parent, string name) where T : ITransientKitbasherUiCommand
        {
            parent.Children.Add(new ToolbarItem() { Name = name, Action = GetMenuAction<T>() });
        }

        public void CreateToolBarSeparator(ToolbarItem parent) => parent.Children.Add(new ToolbarItem() { IsSeparator = true });

        public ToolbarItem CreateRootToolBar(string name)
        {
            var toolBarItem = new ToolbarItem() { Name = name };
            _toolBarItems.Add(toolBarItem);
            return toolBarItem;
        }

        MenuAction GetMenuAction<T>() where T : ITransientKitbasherUiCommand
        {
            return _actionList.First(x => x.Key == typeof(T)).Value;
        }
    }
}
