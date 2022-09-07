using CommonControls.Common;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace CommonControls.SelectionListDialog
{
    /// <summary>
    /// Interaction logic for SelectionListWindow.xaml
    /// </summary>
    public partial class SelectionListWindow : Window
    {
        public bool Result { get; set; } = false;
        public delegate bool OnSeachDelegate(object item, Regex regex);

        SolidColorBrush _noErrorBackground = new SolidColorBrush(Colors.White);
        SolidColorBrush _errorBackground = new SolidColorBrush(Colors.Red);
        public SelectionListWindow()
        {
            InitializeComponent();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            Result = true;
            Close();
        }

        private void FilterConditionChanged<T>( ObservableCollection<SelectionListViewModel<T>.Item> originalList)
        {
            SelectionListViewModel<T> typedDataContext = (SelectionListViewModel<T>)DataContext;
            using (new WaitCursor())
            {
                var toolTip = SearchTextBox.ToolTip as ToolTip;
                if (toolTip == null)
                {
                    toolTip = new ToolTip();
                    SearchTextBox.ToolTip = toolTip;
                }

                var filterText = SearchTextBox.Text;
                if (string.IsNullOrWhiteSpace(filterText))
                {
                    typedDataContext.ItemList.Clear();
                    foreach (var item in originalList)
                    {
                        typedDataContext.ItemList.Add(item);
                    }
                    toolTip.IsOpen = false;
                    SearchTextBox.Background = _noErrorBackground;
                    return;
                }

                try
                {
                    Regex rx = null;
                    typedDataContext.ItemList.Clear();
                    rx = new Regex(filterText, RegexOptions.Compiled | RegexOptions.IgnoreCase);
                    foreach (var item in originalList)
                    {
                        var result = rx.Matches(item.DisplayName).Count > 0;
                        if (result)
                            typedDataContext.ItemList.Add(item);
                            
                    }
                    toolTip.IsOpen = false;
                    SearchTextBox.Background = _noErrorBackground;
                    DataContext = typedDataContext;
                }
                catch (Exception e)
                {
                    SearchTextBox.Background = _errorBackground;
                    toolTip.IsOpen = true;
                    toolTip.Content = e.Message;
                    toolTip.Content += "\n\nCommon usage:\n";
                    toolTip.Content += "Value0.*Value1.*Value2 -> for searching for multiple substrings";
                }
            }
        }

        public void SetDataContextAndFilterConfig<T>(SelectionListViewModel<T> dc)
        {
            DataContext = dc;
            var list = new ObservableCollection<SelectionListViewModel<T>.Item>(dc.ItemList);
            SearchTextBox.TextChanged += (sender, e) => FilterConditionChanged<T>(list);
        }
        public static SelectionListWindow ShowDialog<T>(string titel, IEnumerable<SelectionListViewModel<T>.Item> itemList, bool modal = true)
        {
            var window = new SelectionListWindow();
            var dataContext = new SelectionListViewModel<T>()
            {
                WindowTitle = titel,
            };

            foreach (var item in itemList)
                dataContext.ItemList.Add(item);

            window.SetDataContextAndFilterConfig<T>(dataContext);

            if (modal)
                window.ShowDialog();
            else
                window.Show();

            return window;
        }
    }
}
