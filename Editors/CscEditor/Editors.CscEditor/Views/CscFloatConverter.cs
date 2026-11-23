using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Editors.CscEditor.Views
{
    /// <summary>
    /// Text entry for the CSC editor's float fields. Accepts a comma or a full-width point as the
    /// decimal mark as well as '.', so "1,5" and "1．5" both read as 1.5 whatever the UI culture.
    /// Non-finite values are rejected. ConverterParameter "NonNegative" also rejects values
    /// below 0. A rejected entry leaves the bound value unchanged.
    /// </summary>
    public sealed class CscFloatConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => value;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            TryParse(value as string, parameter as string == "NonNegative", out var result)
                ? result
                : DependencyProperty.UnsetValue;

        internal static bool TryParse(string? text, bool nonNegative, out float result)
        {
            result = 0;
            if (text == null)
                return false;

            var normalized = text.Trim()
                .Replace(',', '.')
                .Replace('．', '.')   // full-width full stop
                .Replace('。', '.');  // ideographic full stop

            return float.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out result)
                && float.IsFinite(result)
                && (!nonNegative || result >= 0);
        }
    }
}
