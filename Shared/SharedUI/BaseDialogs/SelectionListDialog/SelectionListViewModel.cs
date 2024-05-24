// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.ObjectModel;
using Shared.Core.Misc;

namespace Shared.Ui.BaseDialogs.SelectionListDialog
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
