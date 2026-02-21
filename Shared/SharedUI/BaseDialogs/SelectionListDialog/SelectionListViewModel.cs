using System.Collections.ObjectModel;
using Shared.Core.Misc;

namespace Shared.Ui.BaseDialogs.SelectionListDialog
{

    public interface ISelectionListItem
    {
        NotifyAttr<bool> IsChecked { get; set; }
        string DisplayName { get; set; }
    }

    public class SelectionListViewModel<T>
    {
        public string WindowTitle { get; set; } = "Selection List";

        public class Item : ISelectionListItem
        {
            public NotifyAttr<bool> IsChecked { get; set; } = new NotifyAttr<bool>(false);
            public string DisplayName { get; set; } = "";
            public required T ItemValue { get; set; }
        }

        public ObservableCollection<Item> ItemList { get; set; } = [];

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
