using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Editors.Audio.Presentation.AudioEditor
{
    public class EnumConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Enum enumValue && parameter is Enum targetValue && targetType == typeof(bool))
            {
                // Return true if enumValue matches targetValue
                return enumValue.Equals(targetValue);
            }

            return DependencyProperty.UnsetValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue && parameter is Enum)
            {
                // Return the enum value if checkbox is checked, otherwise return 'None'
                return boolValue ? parameter : Enum.Parse(parameter.GetType(), "None");
            }

            return DependencyProperty.UnsetValue;
        }
    }
}
