using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace AssetEditor.Themes.Attached
{
    public class WatermarkComboBoxDropDownVisibilityConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var text = values[0] as string;
            var isKeyboardFocusWithin = (bool)values[1];

            // Show the watermark if the Text is empty and the control does not have focus
            if (string.IsNullOrEmpty(text) && !isKeyboardFocusWithin)
                return Visibility.Visible;

            // Otherwise, hide the watermark
            return Visibility.Collapsed;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

}
