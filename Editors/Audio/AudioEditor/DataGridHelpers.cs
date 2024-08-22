using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace Editors.Audio.AudioEditor
{
    internal static class DataGridHelpers
    {

        public static T FindVisualChild<T>(DependencyObject parent, string name) where T : DependencyObject
        {
            for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                if (child is T typedChild && child is FrameworkElement element && element.Name == name)
                    return typedChild;

                else
                {
                    var foundChild = FindVisualChild<T>(child, name);

                    if (foundChild != null)
                        return foundChild;
                }
            }

            return null;
        }

        public static T FindVisualParent<T>(DependencyObject child) where T : DependencyObject
        {
            while ((child = VisualTreeHelper.GetParent(child)) != null)
            {
                if (child is T parent)
                    return parent;
            }
            return null;
        }

        public static DataGrid GetDataGrid()
        {
            var mainWindow = Application.Current.MainWindow;
            return FindVisualChild<DataGrid>(mainWindow, "AudioEditorDataGrid");
        }

        public class ConvertToolTipCollectionToString : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                if (value is IEnumerable<string> collection)
                    return string.Join(", ", collection.Select(item => $"\"{item}\""));

                else if (value is IList<string> list)
                    return string.Join(", ", list.Select(item => $"\"{item}\""));

                else if (value is IEnumerable enumerable)
                {
                    var stringValue = new StringBuilder();

                    foreach (var item in enumerable)
                    {
                        stringValue.Append($"\"{item.ToString()}\"");
                        stringValue.Append(", ");
                    }

                    return stringValue.ToString().TrimEnd([',', ' ']);
                }

                return string.Empty;
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                return null;
            }
        }
    }
}
