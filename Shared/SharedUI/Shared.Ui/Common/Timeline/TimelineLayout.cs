using System;
using System.Collections.Generic;
using System.Linq;

namespace Shared.Ui.Common.Timeline
{
    /// <summary>One item placed by <see cref="TimelineLayout.Pack"/>, in pixels. Row counts from the top of the track area.</summary>
    public readonly record struct TimelinePlacement(ITimelineItem Item, double Left, double Width, int Row)
    {
        public double Right => Left + Width;
        public bool IsInstant => Item.End == null;
    }

    public sealed record TimelineLayoutResult(IReadOnlyList<TimelinePlacement> Placements, int RowCount, IReadOnlyList<int> LaneFirstRows);

    /// <summary>
    /// Places timeline items into rows. Items are grouped by <see cref="ITimelineItem.Lane"/>, lanes are drawn
    /// top to bottom in ascending order, and inside a lane each item takes the first row it doesn't overlap.
    /// The packing is ported from Szy-Cathay/TheAssetEditor-CN MetaDataTimelinePanel.CreateLayout.
    /// </summary>
    public static class TimelineLayout
    {
        public const double MinimumHitWidth = 12;
        public const double RowGap = 2;

        public static TimelineLayoutResult Pack(IEnumerable<ITimelineItem> items, TimeAxis axis)
        {
            var placements = new List<TimelinePlacement>();
            var laneFirstRows = new List<int>();
            var rowOffset = 0;

            var lanes = items
                .Select((item, index) => (Item: item, Index: index))
                .GroupBy(x => x.Item.Lane)
                .OrderBy(g => g.Key);

            foreach (var lane in lanes)
            {
                var placed = lane
                    .Select(x => (Placement: Place(x.Item, axis), x.Index))
                    .OrderBy(x => x.Placement.Left)
                    .ThenBy(x => x.Placement.Right)
                    .ThenBy(x => x.Index)
                    .ToList();

                var rowEnds = new List<double>();
                foreach (var (placement, _) in placed)
                {
                    var row = rowEnds.FindIndex(end => end + RowGap <= placement.Left);
                    if (row < 0)
                    {
                        row = rowEnds.Count;
                        rowEnds.Add(placement.Right);
                    }
                    else
                    {
                        rowEnds[row] = placement.Right;
                    }
                    placements.Add(placement with { Row = rowOffset + row });
                }

                laneFirstRows.Add(rowOffset);
                rowOffset += Math.Max(1, rowEnds.Count);
            }

            return new TimelineLayoutResult(placements, rowOffset, laneFirstRows);
        }

        static TimelinePlacement Place(ITimelineItem item, TimeAxis axis)
        {
            var width = axis.Width;
            var startX = axis.TimeToX(item.Start);
            double left, itemWidth;
            if (item.End is double end)
            {
                var endX = axis.TimeToX(Math.Max(end, item.Start));
                itemWidth = Math.Max(MinimumHitWidth, endX - startX);
                left = startX;
            }
            else
            {
                itemWidth = MinimumHitWidth;
                left = startX - itemWidth / 2;
            }

            itemWidth = Math.Min(itemWidth, Math.Max(width, 0));
            left = Math.Clamp(left, 0, Math.Max(0, width - itemWidth));
            return new TimelinePlacement(item, left, itemWidth, 0);
        }
    }
}
