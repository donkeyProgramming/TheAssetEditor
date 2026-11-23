using System.Windows.Media;
using Shared.Ui.Common.Timeline;

namespace Shared.UiTest.Common.Timeline
{
    internal class TimelineLayoutTests
    {
        sealed class Item(double start, double? end, int lane = 0) : ITimelineItem
        {
            public double Start { get; } = start;
            public double? End { get; } = end;
            public int Lane { get; } = lane;
            public Color Colour => Colors.White;
            public bool IsSelected { get; set; }
            public string Tooltip => "";
        }

        // 0..10 s over 1000 px, so 1 s = 100 px.
        static readonly TimeAxis Axis = new(0, 10, 1000);

        static TimelinePlacement PlacementOf(TimelineLayoutResult layout, ITimelineItem item) =>
            layout.Placements.Single(p => ReferenceEquals(p.Item, item));

        [Test]
        public void NoItems_NoRows()
        {
            var layout = TimelineLayout.Pack([], Axis);

            Assert.That(layout.RowCount, Is.EqualTo(0));
            Assert.That(layout.Placements, Is.Empty);
        }

        [Test]
        public void Ranges_MapToPixels()
        {
            var item = new Item(1, 3);

            var placement = PlacementOf(TimelineLayout.Pack([item], Axis), item);

            Assert.That(placement.Left, Is.EqualTo(100).Within(1e-9));
            Assert.That(placement.Width, Is.EqualTo(200).Within(1e-9));
        }

        [Test]
        public void Instant_IsCentredOnItsTime_WithMinimumHitWidth()
        {
            var item = new Item(5, null);

            var placement = PlacementOf(TimelineLayout.Pack([item], Axis), item);

            Assert.That(placement.Width, Is.EqualTo(TimelineLayout.MinimumHitWidth));
            Assert.That(placement.Left + placement.Width / 2, Is.EqualTo(500).Within(1e-9));
        }

        [Test]
        public void ShortRange_GetsMinimumHitWidth()
        {
            var item = new Item(2, 2.01);

            var placement = PlacementOf(TimelineLayout.Pack([item], Axis), item);

            Assert.That(placement.Width, Is.EqualTo(TimelineLayout.MinimumHitWidth));
        }

        [Test]
        public void ItemsAtTheEdges_StayInside()
        {
            var first = new Item(0, null);
            var last = new Item(10, null);

            var layout = TimelineLayout.Pack([first, last], Axis);

            Assert.That(PlacementOf(layout, first).Left, Is.EqualTo(0));
            Assert.That(PlacementOf(layout, last).Right, Is.EqualTo(1000).Within(1e-9));
        }

        [Test]
        public void OverlappingItems_TakeSeparateRows_AndFreeRowsAreReused()
        {
            var a = new Item(0, 4);
            var b = new Item(2, 6);
            var c = new Item(5, 8);

            var layout = TimelineLayout.Pack([a, b, c], Axis);

            Assert.That(layout.RowCount, Is.EqualTo(2));
            Assert.That(PlacementOf(layout, a).Row, Is.EqualTo(0));
            Assert.That(PlacementOf(layout, b).Row, Is.EqualTo(1));
            Assert.That(PlacementOf(layout, c).Row, Is.EqualTo(0));
        }

        [Test]
        public void TouchingItems_NeedTheRowGap()
        {
            // b starts exactly where a ends, closer than RowGap, so it goes below.
            var a = new Item(0, 2);
            var b = new Item(2, 4);

            var layout = TimelineLayout.Pack([a, b], Axis);

            Assert.That(PlacementOf(layout, b).Row, Is.EqualTo(1));
        }

        [Test]
        public void Lanes_StackInAscendingOrder_EachPackedOnItsOwn()
        {
            var lane1a = new Item(0, 4, lane: 1);
            var lane0a = new Item(0, 4, lane: 0);
            var lane0b = new Item(1, 5, lane: 0);
            var lane1b = new Item(6, 8, lane: 1);

            var layout = TimelineLayout.Pack([lane1a, lane0a, lane0b, lane1b], Axis);

            Assert.That(layout.RowCount, Is.EqualTo(3));
            Assert.That(layout.LaneFirstRows, Is.EqualTo(new[] { 0, 2 }));
            Assert.That(PlacementOf(layout, lane0a).Row, Is.EqualTo(0));
            Assert.That(PlacementOf(layout, lane0b).Row, Is.EqualTo(1));
            Assert.That(PlacementOf(layout, lane1a).Row, Is.EqualTo(2));
            Assert.That(PlacementOf(layout, lane1b).Row, Is.EqualTo(2));
        }

        [Test]
        public void EqualItems_KeepSourceOrder()
        {
            var first = new Item(1, 2);
            var second = new Item(1, 2);

            var layout = TimelineLayout.Pack([first, second], Axis);

            Assert.That(PlacementOf(layout, first).Row, Is.EqualTo(0));
            Assert.That(PlacementOf(layout, second).Row, Is.EqualTo(1));
        }

        [Test]
        public void TimeAxis_RoundTrips()
        {
            var axis = new TimeAxis(-1, 3, 400);

            Assert.That(axis.TimeToX(0), Is.EqualTo(100).Within(1e-9));
            Assert.That(axis.XToTime(100), Is.EqualTo(0).Within(1e-9));
            Assert.That(axis.XToTime(-50), Is.EqualTo(-1), "clamped to the start");
            Assert.That(axis.XToTime(900), Is.EqualTo(3), "clamped to the end");
        }
    }
}
