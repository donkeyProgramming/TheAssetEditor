using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace AssetEditor.WindowsTitleMenu
{
    public class WindowStateToPathConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var ws = (WindowState)value;
            if (ws == WindowState.Normal)
            {
                return Geometry.Parse("M 13.5,10.5 H 22.5 V 19.5 H 13.5 Z");
            }
            else
            {
                return Geometry.Parse("M 13.5,12.5 H 20.5 V 19.5 H 13.5 Z M 15.5,12.5 V 10.5 H 22.5 V 17.5 H 20.5");
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
