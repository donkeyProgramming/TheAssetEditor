// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Windows;
using System.Windows.Controls;
using static CommonControls.FilterDialog.FilterUserControl;

namespace CommonControls.FilterDialog
{
    /// <summary>
    /// Interaction logic for CollapsableFilterControl.xaml
    /// </summary>
    public partial class CollapsableFilterControl : UserControl
    {
        public CollapsableFilterControl()
        {
            InitializeComponent();
            FilterBox.OnItemSelected += (a, b) => HandleOnItemSelected();
            FilterBox.OnItemDoubleClicked += (a, b) => HandleItemDoubleClicked();
            BrowseButton.Click += (a, b) => ToggleSearchFiled();
            ClearButton.Click += (a, b) => ClearSelection();
            FilterBox.Visibility = Visibility.Collapsed;
        }

        void HandleItemDoubleClicked()
        {
            HandleOnItemSelected();
            FilterBox.Visibility = Visibility.Collapsed;
            BrowseButton.Content = "Browse";
        }

        void HandleOnItemSelected()
        {
            var selectedItem = FilterBox.SelectedItem;
            if (selectedItem != null)
            {
                if (string.IsNullOrWhiteSpace(DisplayMemberPath))
                    SelectedFileName.Text = selectedItem.ToString();
                else
                {
                    var val = selectedItem.GetType().GetProperty(DisplayMemberPath).GetValue(selectedItem, null);
                    SelectedFileName.Text = val.ToString();
                }
            }
            else
            {
                SelectedFileName.Text = "";
            }
        }

        void ClearSelection()
        {
            if (MessageBox.Show("Are you sure?", "", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                FilterBox.SelectedItem = null;
                HandleOnItemSelected();
            }
        }

        void ToggleSearchFiled()
        {
            if (FilterBox.Visibility == Visibility.Visible)
            {
                FilterBox.Visibility = Visibility.Collapsed;
                BrowseButton.Content = "Browse";
            }
            else
            {
                BrowseButton.Content = "Hide";
                FilterBox.Visibility = Visibility.Visible;
            }
        }
        #region properties

        public FrameworkElement InnerContent
        {
            get { return (FrameworkElement)GetValue(InnerContentProperty); }
            set { SetValue(InnerContentProperty, value); }
        }

        public static readonly DependencyProperty InnerContentProperty = DependencyProperty.Register("InnerContent", typeof(FrameworkElement), typeof(CollapsableFilterControl), new UIPropertyMetadata(null));

        public int LabelTotalWidth
        {
            get { return (int)GetValue(LabelTotalWidthProperty); }
            set { SetValue(LabelTotalWidthProperty, value); }
        }

        public static readonly DependencyProperty LabelTotalWidthProperty =
            DependencyProperty.Register("LabelTotalWidth", typeof(int), typeof(CollapsableFilterControl), new PropertyMetadata(null));


        public bool ShowLabel
        {
            get { return (bool)GetValue(ShowLabelProperty); }
            set { SetValue(ShowLabelProperty, value); }
        }

        public static readonly DependencyProperty ShowLabelProperty =
            DependencyProperty.Register("ShowLabel", typeof(bool), typeof(CollapsableFilterControl), new PropertyMetadata(true));

        public string LabelText
        {
            get { return (string)GetValue(LabelTextProperty); }
            set { SetValue(LabelTextProperty, value); }
        }

        public static readonly DependencyProperty LabelTextProperty =
            DependencyProperty.Register("LabelText", typeof(string), typeof(CollapsableFilterControl), new PropertyMetadata(null));

        public string DisplayMemberPath
        {
            get { return (string)GetValue(DisplayMemberPathProperty); }
            set { SetValue(DisplayMemberPathProperty, value); }
        }

        public static readonly DependencyProperty DisplayMemberPathProperty =
            DependencyProperty.Register("DisplayMemberPath", typeof(string), typeof(CollapsableFilterControl), new PropertyMetadata(null));


        public OnSeachDelegate OnSearch
        {
            get { return (OnSeachDelegate)GetValue(OnSearchProperty); }
            set { SetValue(OnSearchProperty, value); }
        }

        public static readonly DependencyProperty OnSearchProperty =
            DependencyProperty.Register("OnSearch", typeof(OnSeachDelegate), typeof(CollapsableFilterControl), new PropertyMetadata(null));


        public IEnumerable SearchItems
        {
            get { return (IEnumerable)GetValue(SearchItemsProperty); }
            set { SetValue(SearchItemsProperty, value); }
        }

        public static readonly DependencyProperty SearchItemsProperty =
            DependencyProperty.Register("SearchItems", typeof(IEnumerable), typeof(CollapsableFilterControl), new PropertyMetadata(null));


        public object SelectedItem
        {
            get { return (object)GetValue(SelectedItemProperty); }
            set { SetValue(SelectedItemProperty, value); }
        }

        public static readonly DependencyProperty SelectedItemProperty =
            DependencyProperty.Register("SelectedItem", typeof(object), typeof(CollapsableFilterControl), new PropertyMetadata(null));

        #endregion
    }
}
