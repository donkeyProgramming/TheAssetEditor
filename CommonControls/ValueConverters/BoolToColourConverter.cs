using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;

namespace CommonControls.ValueConverters
{
    [ValueConversion(typeof(bool), typeof(SolidColorBrush))]
    public class BoolToColourConverter : IValueConverter
    {
        public SolidColorBrush TrueValue { get; set; }
        public SolidColorBrush FalseValue { get; set; }
        
        public BoolToColourConverter()
        {
            // set defaults
            TrueValue = new SolidColorBrush(Colors.Black);
            FalseValue = new SolidColorBrush(Colors.Red);
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is bool))
                return null;
            return (bool)value ? TrueValue : FalseValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (Equals(value, TrueValue))
                return true;
            if (Equals(value, FalseValue))
                return false;
            return null;
        }
    }
}


/*
 */