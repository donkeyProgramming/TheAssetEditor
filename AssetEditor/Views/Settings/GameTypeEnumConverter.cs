using System;
using System.Globalization;
using System.Windows.Data;
using Shared.Core.Services;
using static Shared.Core.Services.GameInformationFactory;

namespace AssetEditor.Views.Settings
{
    public class GameTypeEnumConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return value;

            else if (value is GameTypeEnum game)
                return GetStringFromGameTypeEnum(game);

            else
                return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }
    }
}
