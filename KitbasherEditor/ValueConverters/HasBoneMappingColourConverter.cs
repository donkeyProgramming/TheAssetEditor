using CommonControls.Editors.BoneMapping;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Windows.Data;
using System.Windows.Media;

namespace KitbasherEditor.ValueConverters
{
    [ValueConversion(typeof(bool), typeof(AnimatedBone))]
    public class IsBoneUsedColourConverter : IValueConverter
    {
        SolidColorBrush TrueValue { get; set; }
        SolidColorBrush FalseValue { get; set; }

        public IsBoneUsedColourConverter()
        {
            // set defaults
            TrueValue = new SolidColorBrush(Colors.Black);
            FalseValue = new SolidColorBrush(Colors.Gray);
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is AnimatedBone))
                return FalseValue;

            if ((value as AnimatedBone).IsUsedByCurrentModel.Value)
                return TrueValue;
            return FalseValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
