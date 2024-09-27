using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace AssetEditor.WindowsTitleMenu
{
    public class WindowStateToThicknessConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var ws = (WindowState)value;
            if (ws == WindowState.Maximized)
            {
                return new Thickness(6);
            }
            else
            {
                // left, right and bottom borders are still drawn by the system
                return new Thickness(0, 1, 0, 0);
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
