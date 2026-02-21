// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Windows.Controls;
using Shared.Ui.BaseDialogs.SelectionListDialog;

namespace CommonControls.SelectionListDialog
{
    /// <summary>
    /// Interaction logic for SelectionListView.xaml
    /// </summary>
    public partial class SelectionListView : UserControl
    {
        public SelectionListView()
        {
            InitializeComponent();
        }

        private void ItemsListView_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var listView = sender as ListView;
            var selectedItem = listView?.SelectedItem as ISelectionListItem;
            selectedItem?.IsChecked.Value = !selectedItem.IsChecked.Value;

            var window = System.Windows.Window.GetWindow(this) as SelectionListWindow;
            if (window == null)
                return;

            window.OnItemDoubleClicked();
        }
    }
}
