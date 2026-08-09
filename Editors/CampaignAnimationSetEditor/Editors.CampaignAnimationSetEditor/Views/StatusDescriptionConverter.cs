using System;
using System.Globalization;
using System.Windows.Data;
using Editors.CampaignAnimationSetEditor.Services;

namespace Editors.CampaignAnimationSetEditor.Views
{
    /// <summary>Explains what a status name means, based on <see cref="KnownCampaignStatus"/>.
    /// Anything the survey didn't confirm is worded as such rather than stated as fact.</summary>
    public class StatusDescriptionConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var name = value as string ?? "";
            return Enum.TryParse<KnownCampaignStatus>(name, out var known)
                ? known.Describe()
                : KnownCampaignStatusExtensions.UnknownStatusDescription;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            throw new NotSupportedException();
    }
}
