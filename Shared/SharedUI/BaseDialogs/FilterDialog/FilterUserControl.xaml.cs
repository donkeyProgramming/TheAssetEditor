using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using Shared.Ui.Common;

namespace CommonControls.FilterDialog
{
    /// <summary>
    /// Interaction logic for FilterUserControl.xaml
    /// </summary>
    [ContentProperty("InnerContent")]
    public partial class FilterUserControl : UserControl
    {
        SolidColorBrush _noErrorBackground = new SolidColorBrush(Colors.White);
        SolidColorBrush _errorBackground = new SolidColorBrush(Colors.Red);

        public delegate bool OnSeachDelegate(object item, Regex regex);

        public EventHandler OnItemDoubleClicked;
        public EventHandler OnItemSelected;

        public FilterUserControl()
        {
            InitializeComponent();
            SearchTextBox.TextChanged += (sender, e) => FilterConditionChanged();
            ResultList.SelectionChanged += (sender, e) => OnItemSelected?.Invoke(null, null);
        }


        public FrameworkElement InnerContent
        {
            get { return (FrameworkElement)GetValue(InnerContentProperty); }
            set { SetValue(InnerContentProperty, value); }
        }

        public static readonly DependencyProperty InnerContentProperty = DependencyProperty.Register("InnerContent", typeof(FrameworkElement), typeof(FilterUserControl), new UIPropertyMetadata(null));


        public string DisplayMemberPath
        {
            get { return (string)GetValue(DisplayMemberPathProperty); }
            set { SetValue(DisplayMemberPathProperty, value); }
        }

        public static readonly DependencyProperty DisplayMemberPathProperty =
            DependencyProperty.Register("DisplayMemberPath", typeof(string), typeof(FilterUserControl), new PropertyMetadata(null));

        public OnSeachDelegate OnSearch
        {
            get { return (OnSeachDelegate)GetValue(OnSearchProperty); }
            set { SetValue(OnSearchProperty, value); }
        }

        public static readonly DependencyProperty OnSearchProperty =
            DependencyProperty.Register("OnSearch", typeof(OnSeachDelegate), typeof(FilterUserControl), new PropertyMetadata(null));


        public object SelectedItem
        {
            get { return (object)GetValue(SelectedItemProperty); }
            set { SetValue(SelectedItemProperty, value); }
        }

        public static readonly DependencyProperty SelectedItemProperty =
            DependencyProperty.Register("SelectedItem", typeof(object), typeof(FilterUserControl), new PropertyMetadata(null));


        public IEnumerable SearchItems
        {
            get { return (IEnumerable)GetValue(SearchItemsProperty); }
            set { SetValue(SearchItemsProperty, value); FilterConditionChanged(); }
        }

        public static readonly DependencyProperty SearchItemsProperty =
            DependencyProperty.Register("SearchItems", typeof(IEnumerable), typeof(FilterUserControl),
                new PropertyMetadata
                {
                    PropertyChangedCallback = (obj, e) => { (obj as FilterUserControl).FilterConditionChanged(); }
                });


        private void FilterConditionChanged()
        {
            using (new WaitCursor())
            {
                if (SearchItems == null)
                    return;

                var toolTip = SearchTextBox.ToolTip as ToolTip;
                if (toolTip == null)
                {
                    toolTip = new ToolTip();
                    SearchTextBox.ToolTip = toolTip;
                }

                var filterText = SearchTextBox.Text;
                if (string.IsNullOrWhiteSpace(filterText) || OnSearch == null)
                {
                    ResultList.ItemsSource = SearchItems;
                    toolTip.IsOpen = false;
                    SearchTextBox.Background = _noErrorBackground;
                    return;
                }

                try
                {
                    Regex rx = null;
                    rx = new Regex(filterText, RegexOptions.Compiled | RegexOptions.IgnoreCase);

                    List<object> displayList = new List<object>();
                    foreach (var item in SearchItems)
                    {
                        var result = OnSearch(item, rx);
                        if (result)
                            displayList.Add(item);
                    }
                    ResultList.ItemsSource = displayList;
                    toolTip.IsOpen = false;
                    SearchTextBox.Background = _noErrorBackground;
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

        private void ResultList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            OnItemDoubleClicked?.Invoke(sender, e);
        }
    }
}
