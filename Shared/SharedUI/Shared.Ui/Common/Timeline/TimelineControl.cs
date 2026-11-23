using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace Shared.Ui.Common.Timeline
{
    /// <summary>
    /// A time ruler with a playhead, and below it rows of range bars and instant ticks (<see cref="ITimelineItem"/>).
    /// Drag on the ruler or on empty track space to scrub <see cref="CurrentTime"/>. Click an item to select it,
    /// double-click to raise <see cref="ItemDoubleClicked"/>. Hovering an item shows its tooltip.
    /// With no items it is a plain scrub bar. <see cref="MaxVisibleRows"/> caps the height; the mouse wheel scrolls the rest.
    /// </summary>
    public class TimelineControl : FrameworkElement
    {
        const double TrackPadding = 2;
        const double EmptyTrackHeight = 10;
        const double InstantWidth = 3;
        const double MinimumBarWidth = 3;

        public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register(
            nameof(ItemsSource), typeof(IEnumerable), typeof(TimelineControl),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender, OnItemsSourceChanged));

        public static readonly DependencyProperty CurrentTimeProperty = DependencyProperty.Register(
            nameof(CurrentTime), typeof(double), typeof(TimelineControl),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public static readonly DependencyProperty DurationProperty = DependencyProperty.Register(
            nameof(Duration), typeof(double), typeof(TimelineControl),
            new FrameworkPropertyMetadata(1.0, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty ViewStartProperty = DependencyProperty.Register(
            nameof(ViewStart), typeof(double), typeof(TimelineControl),
            new FrameworkPropertyMetadata(double.NaN, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty ViewEndProperty = DependencyProperty.Register(
            nameof(ViewEnd), typeof(double), typeof(TimelineControl),
            new FrameworkPropertyMetadata(double.NaN, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty RowHeightProperty = DependencyProperty.Register(
            nameof(RowHeight), typeof(double), typeof(TimelineControl),
            new FrameworkPropertyMetadata(12.0, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty MaxVisibleRowsProperty = DependencyProperty.Register(
            nameof(MaxVisibleRows), typeof(int), typeof(TimelineControl),
            new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty ShowRulerProperty = DependencyProperty.Register(
            nameof(ShowRuler), typeof(bool), typeof(TimelineControl),
            new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty SelectedItemProperty = DependencyProperty.Register(
            nameof(SelectedItem), typeof(ITimelineItem), typeof(TimelineControl),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedItemChanged));

        public static readonly DependencyProperty ItemClickCommandProperty = DependencyProperty.Register(
            nameof(ItemClickCommand), typeof(ICommand), typeof(TimelineControl), new PropertyMetadata(null));

        public static readonly DependencyProperty ItemDoubleClickCommandProperty = DependencyProperty.Register(
            nameof(ItemDoubleClickCommand), typeof(ICommand), typeof(TimelineControl), new PropertyMetadata(null));

        /// <summary>The items to draw, each an <see cref="ITimelineItem"/>. Observed when it raises collection changes.</summary>
        public IEnumerable? ItemsSource
        {
            get => (IEnumerable?)GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }

        public double CurrentTime
        {
            get => (double)GetValue(CurrentTimeProperty);
            set => SetValue(CurrentTimeProperty, value);
        }

        /// <summary>Scrubbing is clamped to [0, Duration].</summary>
        public double Duration
        {
            get => (double)GetValue(DurationProperty);
            set => SetValue(DurationProperty, value);
        }

        /// <summary>Optional start of the drawn span; NaN means 0. Lets the control line up with another axis.</summary>
        public double ViewStart
        {
            get => (double)GetValue(ViewStartProperty);
            set => SetValue(ViewStartProperty, value);
        }

        /// <summary>Optional end of the drawn span; NaN means <see cref="Duration"/>.</summary>
        public double ViewEnd
        {
            get => (double)GetValue(ViewEndProperty);
            set => SetValue(ViewEndProperty, value);
        }

        public double RowHeight
        {
            get => (double)GetValue(RowHeightProperty);
            set => SetValue(RowHeightProperty, value);
        }

        /// <summary>When above 0, at most this many rows show and the mouse wheel scrolls the rest.</summary>
        public int MaxVisibleRows
        {
            get => (int)GetValue(MaxVisibleRowsProperty);
            set => SetValue(MaxVisibleRowsProperty, value);
        }

        public bool ShowRuler
        {
            get => (bool)GetValue(ShowRulerProperty);
            set => SetValue(ShowRulerProperty, value);
        }

        public ITimelineItem? SelectedItem
        {
            get => (ITimelineItem?)GetValue(SelectedItemProperty);
            set => SetValue(SelectedItemProperty, value);
        }

        /// <summary>Executed with the clicked <see cref="ITimelineItem"/> after it is selected.</summary>
        public ICommand? ItemClickCommand
        {
            get => (ICommand?)GetValue(ItemClickCommandProperty);
            set => SetValue(ItemClickCommandProperty, value);
        }

        public ICommand? ItemDoubleClickCommand
        {
            get => (ICommand?)GetValue(ItemDoubleClickCommandProperty);
            set => SetValue(ItemDoubleClickCommandProperty, value);
        }

        public event Action<ITimelineItem>? ItemClicked;
        public event Action<ITimelineItem>? ItemDoubleClicked;

        /// <summary>Raised when a scrub drag starts and ends, so a player can pause while the user drags.</summary>
        public event Action? ScrubStarted;
        public event Action? ScrubCompleted;

        readonly ToolTip _toolTip = new() { Placement = PlacementMode.Mouse };
        readonly List<INotifyPropertyChanged> _observedItems = [];
        TimelineLayoutResult _layout = new([], 0, []);
        ITimelineItem? _hovered;
        bool _scrubbing;
        int _firstVisibleRow;
        bool _scrollToSelection;

        public TimelineControl()
        {
            ClipToBounds = true;
            Focusable = true;
            _toolTip.PlacementTarget = this;
        }

        IEnumerable<ITimelineItem> Items => ItemsSource?.OfType<ITimelineItem>() ?? [];

        double RulerHeight => ShowRuler ? TimeAxis.RulerHeight : 0;

        public TimeAxis Axis(double width)
        {
            var start = double.IsNaN(ViewStart) ? 0 : ViewStart;
            var end = double.IsNaN(ViewEnd) ? Duration : ViewEnd;
            if (end - start <= 0)
                end = start + 0.001;
            return new TimeAxis(start, end, width);
        }

        // ---------------------------------------------------------------------
        // Items
        // ---------------------------------------------------------------------

        static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (TimelineControl)d;
            if (e.OldValue is INotifyCollectionChanged oldCollection)
                oldCollection.CollectionChanged -= control.ItemsCollectionChanged;
            if (e.NewValue is INotifyCollectionChanged newCollection)
                newCollection.CollectionChanged += control.ItemsCollectionChanged;
            control.ObserveItems();
        }

        void ItemsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            ObserveItems();
            if (SelectedItem != null && !Items.Contains(SelectedItem))
                SelectedItem = null;
            InvalidateMeasure();
            InvalidateVisual();
        }

        void ObserveItems()
        {
            foreach (var item in _observedItems)
                item.PropertyChanged -= ItemPropertyChanged;
            _observedItems.Clear();
            foreach (var item in Items.OfType<INotifyPropertyChanged>())
            {
                item.PropertyChanged += ItemPropertyChanged;
                _observedItems.Add(item);
            }
            SetHovered(null);
        }

        void ItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName is nameof(ITimelineItem.Start) or nameof(ITimelineItem.End) or nameof(ITimelineItem.Lane) or null or "")
                InvalidateMeasure();
            InvalidateVisual();
        }

        static void OnSelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (TimelineControl)d;
            foreach (var item in control.Items)
            {
                var isSelected = ReferenceEquals(item, e.NewValue);
                if (item.IsSelected != isSelected)
                    item.IsSelected = isSelected;
            }
            control._scrollToSelection = e.NewValue != null;
            control.ScrollSelectionIntoView();
        }

        // Done once the selection has been laid out, which may be after it was set (a selection made before the view was shown).
        void ScrollSelectionIntoView()
        {
            if (!_scrollToSelection || SelectedItem == null)
                return;
            var placement = _layout.Placements.FirstOrDefault(p => ReferenceEquals(p.Item, SelectedItem));
            if (placement.Item == null)
                return;
            _scrollToSelection = false;
            var visibleRows = VisibleRowCount;
            if (placement.Row < _firstVisibleRow)
                _firstVisibleRow = placement.Row;
            else if (placement.Row >= _firstVisibleRow + visibleRows)
                _firstVisibleRow = placement.Row - visibleRows + 1;
            InvalidateVisual();
        }

        // ---------------------------------------------------------------------
        // Layout
        // ---------------------------------------------------------------------

        protected override Size MeasureOverride(Size availableSize)
        {
            var width = double.IsFinite(availableSize.Width) ? availableSize.Width : ActualWidth;
            _layout = TimelineLayout.Pack(Items, Axis(width));
            ClampFirstVisibleRow();
            ScrollSelectionIntoView();
            var trackHeight = _layout.RowCount == 0 ? EmptyTrackHeight : VisibleRowCount * RowHeight + TrackPadding * 2;
            return new Size(width, RulerHeight + trackHeight);
        }

        int VisibleRowCount => MaxVisibleRows > 0 ? Math.Min(_layout.RowCount, MaxVisibleRows) : _layout.RowCount;

        void ClampFirstVisibleRow() => _firstVisibleRow = Math.Clamp(_firstVisibleRow, 0, Math.Max(0, _layout.RowCount - VisibleRowCount));

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);
            if (!sizeInfo.WidthChanged)
                return;
            var rowCount = _layout.RowCount;
            _layout = TimelineLayout.Pack(Items, Axis(ActualWidth));
            ClampFirstVisibleRow();
            if (_layout.RowCount != rowCount)
                InvalidateMeasure();
        }

        double RowTop(int row) => RulerHeight + TrackPadding + (row - _firstVisibleRow) * RowHeight;

        bool IsRowVisible(int row) => row >= _firstVisibleRow && row < _firstVisibleRow + VisibleRowCount;

        // ---------------------------------------------------------------------
        // Rendering
        // ---------------------------------------------------------------------

        static readonly Brush TrackBrush = Frozen(new SolidColorBrush(Color.FromRgb(30, 30, 34)));
        static readonly Brush OutOfRangeBrush = Frozen(new SolidColorBrush(Color.FromArgb(150, 15, 15, 17)));
        static readonly Pen GridPen = Frozen(new Pen(Frozen(new SolidColorBrush(Color.FromRgb(48, 48, 54))), 1));
        static readonly Pen LaneSeparatorPen = Frozen(new Pen(Frozen(new SolidColorBrush(Color.FromRgb(62, 62, 70))), 1));
        static readonly Brush ScrollThumbBrush = Frozen(new SolidColorBrush(Color.FromArgb(150, 220, 220, 230)));
        static readonly Pen SelectedPen = Frozen(new Pen(Brushes.White, 1.5));
        static readonly Pen HoverPen = Frozen(new Pen(Frozen(new SolidColorBrush(Color.FromArgb(200, 255, 255, 255))), 1));

        static T Frozen<T>(T freezable) where T : Freezable
        {
            freezable.Freeze();
            return freezable;
        }

        protected override void OnRender(DrawingContext dc)
        {
            var width = ActualWidth;
            var height = ActualHeight;
            dc.DrawRectangle(TrackBrush, null, new Rect(0, 0, width, height));
            if (width < 2)
                return;

            var axis = Axis(width);
            DrawGrid(dc, axis, height);
            DrawOutOfRange(dc, axis, height);

            for (var i = 1; i < _layout.LaneFirstRows.Count; i++)
            {
                if (!IsRowVisible(_layout.LaneFirstRows[i]))
                    continue;
                var y = Math.Floor(RowTop(_layout.LaneFirstRows[i])) + 0.5;
                dc.DrawLine(LaneSeparatorPen, new Point(0, y), new Point(width, y));
            }

            // Wide bars first so ticks and short bars stay on top, selection last.
            var ordered = _layout.Placements
                .Where(p => IsRowVisible(p.Row))
                .OrderBy(p => p.IsInstant)
                .ThenByDescending(p => p.Width)
                .ThenBy(p => p.Item.IsSelected);
            foreach (var placement in ordered)
                DrawItem(dc, axis, placement);
            DrawScrollIndicator(dc, width);

            if (ShowRuler)
                axis.DrawRuler(dc);
            axis.DrawPlayhead(dc, CurrentTime, height, Brushes.OrangeRed);
        }

        // A thin thumb on the right edge when rows are scrolled out of view.
        void DrawScrollIndicator(DrawingContext dc, double width)
        {
            var visibleRows = VisibleRowCount;
            if (visibleRows == 0 || _layout.RowCount <= visibleRows)
                return;
            var trackTop = RulerHeight + TrackPadding;
            var trackHeight = visibleRows * RowHeight;
            var thumbHeight = Math.Max(4, trackHeight * visibleRows / _layout.RowCount);
            var thumbTop = trackTop + (trackHeight - thumbHeight) * _firstVisibleRow / (_layout.RowCount - visibleRows);
            dc.DrawRoundedRectangle(ScrollThumbBrush, null, new Rect(width - 4, thumbTop, 3, thumbHeight), 1.5, 1.5);
        }

        void DrawGrid(DrawingContext dc, TimeAxis axis, double height)
        {
            var step = TimeAxis.NiceStep(axis.Span, 10);
            for (var t = Math.Floor(axis.Min / step) * step; t <= axis.Max + 0.001; t += step)
            {
                var x = Math.Floor(axis.TimeToX(t)) + 0.5;
                dc.DrawLine(GridPen, new Point(x, RulerHeight), new Point(x, height));
            }
        }

        void DrawOutOfRange(DrawingContext dc, TimeAxis axis, double height)
        {
            var zeroX = axis.TimeToX(0);
            if (zeroX > 0)
                dc.DrawRectangle(OutOfRangeBrush, null, new Rect(0, RulerHeight, zeroX, height - RulerHeight));
            var endX = axis.TimeToX(Duration);
            if (endX < axis.Width)
                dc.DrawRectangle(OutOfRangeBrush, null, new Rect(endX, RulerHeight, axis.Width - endX, height - RulerHeight));
        }

        void DrawItem(DrawingContext dc, TimeAxis axis, TimelinePlacement placement)
        {
            var item = placement.Item;
            var top = RowTop(placement.Row);
            var brush = new SolidColorBrush(item.Colour);
            brush.Freeze();
            var pen = item.IsSelected ? SelectedPen : ReferenceEquals(item, _hovered) ? HoverPen : null;

            if (placement.IsInstant)
            {
                // Kept inside the control so a tick at the very start or end isn't half clipped.
                var x = Math.Clamp(axis.TimeToX(item.Start), InstantWidth / 2, Math.Max(InstantWidth / 2, axis.Width - InstantWidth / 2));
                var rect = new Rect(x - InstantWidth / 2, top + 1, InstantWidth, RowHeight - 2);
                dc.DrawRectangle(brush, pen, rect);
            }
            else
            {
                var startX = axis.TimeToX(item.Start);
                var endX = axis.TimeToX(Math.Max(item.End!.Value, item.Start));
                var barWidth = Math.Max(MinimumBarWidth, endX - startX);
                var barHeight = item.IsSelected ? RowHeight - 2 : RowHeight - 4;
                var rect = new Rect(startX, top + (RowHeight - barHeight) / 2, barWidth, barHeight);
                dc.DrawRoundedRectangle(brush, pen, rect, 2, 2);
            }
        }

        // ---------------------------------------------------------------------
        // Interaction
        // ---------------------------------------------------------------------

        ITimelineItem? HitTestItem(Point position)
        {
            if (position.Y < RulerHeight)
                return null;

            // Ticks before bars, then the narrowest, so a short item over a long one stays clickable.
            return _layout.Placements
                .Where(p => IsRowVisible(p.Row))
                .Where(p => position.X >= p.Left && position.X <= p.Right
                    && position.Y >= RowTop(p.Row) && position.Y < RowTop(p.Row) + RowHeight)
                .OrderByDescending(p => p.IsInstant)
                .ThenBy(p => p.Width)
                .Select(p => (ITimelineItem?)p.Item)
                .FirstOrDefault();
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            Focus();
            var position = e.GetPosition(this);
            var item = HitTestItem(position);

            if (item != null)
            {
                SelectedItem = item;
                if (e.ClickCount == 2)
                {
                    ItemDoubleClicked?.Invoke(item);
                    Execute(ItemDoubleClickCommand, item);
                }
                else
                {
                    ItemClicked?.Invoke(item);
                    Execute(ItemClickCommand, item);
                }
                e.Handled = true;
                return;
            }

            _scrubbing = true;
            CaptureMouse();
            SetHovered(null);
            ScrubStarted?.Invoke();
            ScrubTo(position.X);
            e.Handled = true;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            var position = e.GetPosition(this);
            if (_scrubbing)
            {
                ScrubTo(position.X);
                return;
            }
            SetHovered(HitTestItem(position));
        }

        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonUp(e);
            EndScrub();
        }

        protected override void OnLostMouseCapture(MouseEventArgs e)
        {
            base.OnLostMouseCapture(e);
            EndScrub();
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            base.OnMouseWheel(e);
            var maxFirstRow = _layout.RowCount - VisibleRowCount;
            if (maxFirstRow <= 0)
                return;
            _firstVisibleRow = Math.Clamp(_firstVisibleRow - Math.Sign(e.Delta), 0, maxFirstRow);
            SetHovered(HitTestItem(e.GetPosition(this)));
            InvalidateVisual();
            e.Handled = true;
        }

        protected override void OnMouseLeave(MouseEventArgs e)
        {
            base.OnMouseLeave(e);
            if (!_scrubbing)
                SetHovered(null);
        }

        void ScrubTo(double x)
        {
            var time = Axis(ActualWidth).XToTime(x);
            CurrentTime = Math.Clamp(time, 0, Math.Max(0, Duration));
        }

        void EndScrub()
        {
            if (!_scrubbing)
                return;
            _scrubbing = false;
            if (IsMouseCaptured)
                ReleaseMouseCapture();
            ScrubCompleted?.Invoke();
        }

        void SetHovered(ITimelineItem? item)
        {
            if (ReferenceEquals(item, _hovered))
                return;
            _hovered = item;
            InvalidateVisual();

            _toolTip.IsOpen = false;
            if (item != null && !string.IsNullOrEmpty(item.Tooltip))
            {
                _toolTip.Content = item.Tooltip;
                _toolTip.IsOpen = true;
            }
        }

        static void Execute(ICommand? command, ITimelineItem item)
        {
            if (command?.CanExecute(item) == true)
                command.Execute(item);
        }
    }
}
