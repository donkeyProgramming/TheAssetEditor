using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace CommonControls.PackFileBrowser
{
    /// <summary>
    /// Interaction logic for PackFileBrowserView.xaml
    /// </summary>
    public partial class PackFileBrowserView : UserControl
    {
        public PackFileBrowserView()
        {
            InitializeComponent();
        }

        public ContextMenu CustomContextMenu
        {
            get { return (ContextMenu)GetValue(CustomContextMenuProperty); }
            set { SetValue(CustomContextMenuProperty, value); }
        }

        public static readonly DependencyProperty CustomContextMenuProperty = DependencyProperty.Register("CustomContextMenu", typeof(ContextMenu), typeof(PackFileBrowserView), new UIPropertyMetadata(null));
    }


    public class SortedCollectionViewSource : IValueConverter
    {
        public string Property0 { get; set; }
        public string Property1 { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var s = CollectionViewSource.GetDefaultView(value);
            s.SortDescriptions.Add(new SortDescription(Property0, ListSortDirection.Ascending));
            s.SortDescriptions.Add(new SortDescription(Property1, ListSortDirection.Ascending));
            return s;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
