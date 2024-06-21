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
            if (value == null || parameter == null)
                return DependencyProperty.UnsetValue;

            if (value is Enum enumValue && parameter is Enum targetValue)
            {
                // For boolean conversion
                if (targetType == typeof(bool))
                {
                    // Check if enumValue matches targetValue
                    return enumValue.Equals(targetValue);
                }

                // For visibility conversion
                else if (targetType == typeof(Visibility))
                {
                    // Check if enumValue matches targetValue
                    return enumValue.Equals(targetValue) ? Visibility.Visible : Visibility.Collapsed;
                }
            }

            return DependencyProperty.UnsetValue;
        }


        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null || !(parameter is Enum))
                return DependencyProperty.UnsetValue;

            if (value is bool boolValue)
            {
                if (boolValue)
                {
                    // Checkbox is checked, return the enum value
                    return parameter;
                }

                else
                {
                    // Checkbox is unchecked, return None
                    return Enum.Parse(parameter.GetType(), "None");
                }
            }

            return DependencyProperty.UnsetValue;
        }
    }
}
