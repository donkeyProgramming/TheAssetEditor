using CommonControls.Common;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace CommonControls.SelectionListDialog
{
    public class SelectionListViewModel<T>
    {
        public string WindowTitle { get; set; } = "Selection List";

        public class Item
        {
            public NotifyAttr<bool> IsChecked { get; set; } = new NotifyAttr<bool>(false);
            public string DisplayName { get; set; } = "";

            public T ItemValue { get; set; }
        }

        public ObservableCollection<Item> ItemList { get; set; } = new ObservableCollection<Item>();

        public void SelectAll()
        {
            foreach (var item in ItemList)
                item.IsChecked.Value = true;
        }

        public void DeselectAll()
        {
            foreach (var item in ItemList)
                item.IsChecked.Value = false;
        }

    }
}
