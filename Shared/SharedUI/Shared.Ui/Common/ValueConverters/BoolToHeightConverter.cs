using System;
using System.Globalization;
using System.Windows.Data;

namespace Shared.Ui.Common.ValueConverters
{
    [ValueConversion(typeof(bool), typeof(double))]
    public sealed class BoolToHeightConverter : IValueConverter
    {
        public double TrueValue { get; set; }
        public double FalseValue { get; set; }

        public BoolToHeightConverter()
        {
            // set defaults
            TrueValue = double.NaN;
            FalseValue = 0;
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
