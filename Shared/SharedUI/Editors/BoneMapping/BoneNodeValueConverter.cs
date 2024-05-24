using System;
using System.Windows.Data;
using System.Windows.Media;

namespace Shared.Ui.Editors.BoneMapping
{
    public class BoneNodeValueConverter : IValueConverter
    {
        public SolidColorBrush DefaultBrush { get; set; } = new SolidColorBrush(Colors.White);
        public SolidColorBrush NotMappedBrush { get; set; } = new SolidColorBrush(Colors.Red);
        public SolidColorBrush NotUsedBrush { get; set; } = new SolidColorBrush(Colors.Gray);

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var typedValue = value as AnimatedBone;
            if (typedValue == null)
                return DefaultBrush;


            if (typedValue.IsUsedByCurrentModel.Value)
                return NotUsedBrush;

            if (typedValue.MappedBoneIndex.Value == -1)
                return NotMappedBrush;

            return DefaultBrush;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

}
