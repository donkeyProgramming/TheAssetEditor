using CommonControls.Common.MenuSystem;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace KitbasherEditor.ViewModels.MenuBarViews.Helpers
{
    public class ToolBarBuilder
    {
        ObservableCollection<ToolbarItem> _toolBarItems { get; set; } = new ObservableCollection<ToolbarItem>();
        Dictionary<Type, MenuAction> _actionList = new();

        public ToolBarBuilder(Dictionary<Type, MenuAction> actionList)
        {
            _actionList = actionList;
        }

        public ObservableCollection<ToolbarItem> Build() => _toolBarItems;


        public void CreateToolBarItem<T>(ToolbarItem parent, string name) where T : IKitbasherUiCommand
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

        MenuAction GetMenuAction<T>() where T : IKitbasherUiCommand
        {
            return _actionList.First(x => x.Key == typeof(T)).Value;
        }
    }
}
