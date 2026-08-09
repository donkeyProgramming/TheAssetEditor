using System;
using System.Globalization;
using System.Windows.Data;
using Editors.CampaignAnimationSetEditor.Services;

namespace Editors.CampaignAnimationSetEditor.Views
{
    /// <summary>Shows the known pose name for a PoseId, falling back to the raw number for the rare
    /// vanilla outlier values that don't fit the confirmed KnownPoseId scheme.</summary>
    public class PoseIdDisplayConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int poseId && Enum.IsDefined(typeof(KnownPoseId), poseId))
                return ((KnownPoseId)poseId).ToString();

            return value?.ToString() ?? "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            throw new NotSupportedException();
    }
}
