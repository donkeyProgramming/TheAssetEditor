using System.Globalization;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using Shared.Ui.Common.Timeline;

namespace Editors.AnimationMeta.SuperView.Timeline
{
    /// <summary>One META tag on the animation player's timeline. Persistent tags sit in the top lane, animation tags below.</summary>
    public sealed partial class MetaDataTimelineItem : ObservableObject, ITimelineItem
    {
        [ObservableProperty] bool _isSelected;

        public MetaDataInspectionItem Item { get; }
        public double Start { get; }
        public double? End { get; }
        public int Lane => Item.Owner == MetaDataDocumentOwner.Persistent ? 0 : 1;
        public Color Colour { get; }
        public string Tooltip { get; }

        public MetaDataTimelineItem(MetaDataInspectionItem item, double clipDuration, string ownerName, string wholeAnimationText)
        {
            Item = item;
            var range = item.TimeRange!.Value;
            switch (item.MarkerKind)
            {
                case MetaDataTimelineMarkerKind.WholeAnimation:
                    Start = 0;
                    End = clipDuration;
                    break;
                case MetaDataTimelineMarkerKind.Range:
                    Start = range.StartTime;
                    End = range.EndTime;
                    break;
                default:
                    Start = range.StartTime;
                    End = null;
                    break;
            }

            Colour = TagColour(item.Source.Name ?? "");
            var time = item.MarkerKind switch
            {
                MetaDataTimelineMarkerKind.WholeAnimation => wholeAnimationText,
                MetaDataTimelineMarkerKind.Range => string.Format(CultureInfo.InvariantCulture, "{0:0.###} - {1:0.###} s", range.StartTime, range.EndTime),
                _ => string.Format(CultureInfo.InvariantCulture, "{0:0.###} s", range.StartTime),
            };
            Tooltip = $"{item.Source.DisplayName}\n{ownerName}: {time}";
        }

        /// <summary>A stable colour per tag name, lightened so it reads on the dark track.
        /// Ported from Szy-Cathay/TheAssetEditor-CN MetaDataTimelineTypeBrushConverter.</summary>
        public static Color TagColour(string tagName)
        {
            const ulong offsetBasis = 14695981039346656037;
            const ulong prime = 1099511628211;
            var hash = offsetBasis;
            foreach (var character in tagName)
            {
                hash ^= char.ToUpperInvariant(character);
                hash *= prime;
            }

            const double anchorWeight = 0.45;
            static byte Blend(ulong channel) => (byte)Math.Round((byte)channel * (1 - anchorWeight) + 255 * anchorWeight);
            return Color.FromRgb(Blend(hash >> 8), Blend(hash >> 24), Blend(hash >> 40));
        }
    }
}
