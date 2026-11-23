using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Editors.CscEditor.Data;
using Shared.Core.Misc;
using Shared.GameFormats.Esf;

namespace Editors.CscEditor.Views
{
    /// <summary>One curve shown in the editor: a named, coloured channel.</summary>
    public class CurveSeries : NotifyPropertyChangedImpl
    {
        public required string Name { get; init; }
        public required Color Colour { get; init; }
        public required CscChannel Channel { get; init; }

        bool _isVisible = true;
        public bool IsVisible { get => _isVisible; set => SetAndNotify(ref _isVisible, value); }

        public SolidColorBrush Brush => new(Colour);
    }

    /// <summary>
    /// A keyframe curve editor drawn directly with a DrawingContext. Interactions:
    /// left-drag a keyframe to move it (time/value), double-click a curve area to add a keyframe
    /// to the selected series, right-click a keyframe to delete it, drag in the top time ruler to
    /// scrub the timeline cursor.
    /// </summary>
    public class CurveEditorControl : FrameworkElement
    {
        const double RulerHeight = 18;
        const double KeySize = 7;
        const double HitRadius = 8;

        public static readonly DependencyProperty SeriesSourceProperty = DependencyProperty.Register(
            nameof(SeriesSource), typeof(ObservableCollection<CurveSeries>), typeof(CurveEditorControl),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, OnSeriesChanged));

        public static readonly DependencyProperty SelectedSeriesProperty = DependencyProperty.Register(
            nameof(SelectedSeries), typeof(CurveSeries), typeof(CurveEditorControl),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty CurrentTimeProperty = DependencyProperty.Register(
            nameof(CurrentTime), typeof(double), typeof(CurveEditorControl),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public static readonly DependencyProperty DurationProperty = DependencyProperty.Register(
            nameof(Duration), typeof(double), typeof(CurveEditorControl),
            new FrameworkPropertyMetadata(20.0, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty IsReadOnlyStructureProperty = DependencyProperty.Register(
            nameof(IsReadOnlyStructure), typeof(bool), typeof(CurveEditorControl), new PropertyMetadata(false));

        public static readonly DependencyProperty IsLoopingProperty = DependencyProperty.Register(
            nameof(IsLooping), typeof(bool), typeof(CurveEditorControl),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty ElementBeginProperty = DependencyProperty.Register(
            nameof(ElementBegin), typeof(double), typeof(CurveEditorControl),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty ElementEndProperty = DependencyProperty.Register(
            nameof(ElementEnd), typeof(double), typeof(CurveEditorControl),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty ElementTimingModeProperty = DependencyProperty.Register(
            nameof(ElementTimingMode), typeof(string), typeof(CurveEditorControl),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

        public ObservableCollection<CurveSeries>? SeriesSource
        {
            get => (ObservableCollection<CurveSeries>?)GetValue(SeriesSourceProperty);
            set => SetValue(SeriesSourceProperty, value);
        }

        public CurveSeries? SelectedSeries
        {
            get => (CurveSeries?)GetValue(SelectedSeriesProperty);
            set => SetValue(SelectedSeriesProperty, value);
        }

        public double CurrentTime
        {
            get => (double)GetValue(CurrentTimeProperty);
            set => SetValue(CurrentTimeProperty, value);
        }

        public double Duration
        {
            get => (double)GetValue(DurationProperty);
            set => SetValue(DurationProperty, value);
        }

        /// <summary>When set, keyframes can be moved but not added/removed (files whose
        /// COMPOSITE_SCENE manifest could not be parsed and so cannot be re-counted).</summary>
        public bool IsReadOnlyStructure
        {
            get => (bool)GetValue(IsReadOnlyStructureProperty);
            set => SetValue(IsReadOnlyStructureProperty, value);
        }

        /// <summary>When set, each visible curve gets small markers at the left/right edges
        /// showing the value its counterpart end has - lines up start/end values for a seamless loop.</summary>
        public bool IsLooping
        {
            get => (bool)GetValue(IsLoopingProperty);
            set => SetValue(IsLoopingProperty, value);
        }

        /// <summary>Selected element's active-time window, shaded outside [Begin, End) (or just
        /// before Begin when TimingMode is "infinite") to show where the element doesn't exist.</summary>
        public double ElementBegin
        {
            get => (double)GetValue(ElementBeginProperty);
            set => SetValue(ElementBeginProperty, value);
        }

        public double ElementEnd
        {
            get => (double)GetValue(ElementEndProperty);
            set => SetValue(ElementEndProperty, value);
        }

        public string? ElementTimingMode
        {
            get => (string?)GetValue(ElementTimingModeProperty);
            set => SetValue(ElementTimingModeProperty, value);
        }

        /// <summary>Raised whenever curve data was changed through this control.</summary>
        public event Action? CurvesModified;

        /// <summary>Raised when a key/handle drag, a key add or a key delete starts and when it
        /// ends, so the whole interaction becomes one undo step.</summary>
        public event Action? EditGestureStarted;
        public event Action? EditGestureCompleted;
        bool _editGestureActive;

        CscKeyframe? _dragKey;
        CurveSeries? _dragSeries;
        bool _draggingTime;

        /// <summary>The one keyframe currently showing its bezier tangent handles - only set by
        /// an explicit click on a keyframe, cleared by clicking empty canvas.</summary>
        CscKeyframe? _selectedKey;
        CurveSeries? _selectedKeySeries;
        bool _draggingHandleOut;
        bool _draggingHandleIn;

        public CurveEditorControl()
        {
            ClipToBounds = true;
            Focusable = true;
        }

        static void OnSeriesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (CurveEditorControl)d;
            if (e.OldValue is ObservableCollection<CurveSeries> oldSeries)
                oldSeries.CollectionChanged -= control.SeriesCollectionChanged;
            if (e.NewValue is ObservableCollection<CurveSeries> newSeries)
                newSeries.CollectionChanged += control.SeriesCollectionChanged;
            control._selectedKey = null;
            control._selectedKeySeries = null;
        }

        void SeriesCollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) => InvalidateVisual();

        public void Redraw() => InvalidateVisual();

        // ---------------------------------------------------------------------
        // Coordinate mapping
        // ---------------------------------------------------------------------

        (double Min, double Max) ValueRange()
        {
            double min = double.MaxValue, max = double.MinValue;
            foreach (var series in VisibleSeries())
            {
                min = Math.Min(min, series.Channel.Header);
                max = Math.Max(max, series.Channel.Header);
                foreach (var key in series.Channel.Keyframes)
                {
                    min = Math.Min(min, key.Value);
                    max = Math.Max(max, key.Value);
                }
            }

            if (min > max)
                return (-1, 1);
            if (max - min < 0.0001)
                return (min - 1, max + 1);

            var pad = (max - min) * 0.12;
            return (min - pad, max + pad);
        }

        IEnumerable<CurveSeries> VisibleSeries() =>
            (SeriesSource ?? []).Where(s => s.IsVisible);

        /// <summary>The timeline span actually drawn: [0, Duration] widened to include any
        /// keyframe or the selected element's Begin/End that sits outside it, plus a little
        /// breathing room on whichever side got widened. Playback itself never sees anything out
        /// here - <see cref="CscElement.NormalizedWindow"/> clamps a negative time to 0 and one
        /// past Duration to Duration - so this is purely a "here's what's actually authored, even
        /// the part that doesn't do anything" view (see <see cref="DrawOutOfBoundsShading"/>).</summary>
        (double Min, double Max) TimeRange()
        {
            double min = 0, max = Math.Max(Duration, 0.001);
            foreach (var series in VisibleSeries())
                foreach (var key in series.Channel.Keyframes)
                {
                    min = Math.Min(min, key.Time);
                    max = Math.Max(max, key.Time);
                }
            min = Math.Min(min, ElementBegin);
            max = Math.Max(max, ElementEnd);

            var pad = Math.Max(Duration, 0.001) * 0.04;
            if (min < 0)
                min -= pad;
            if (max > Duration)
                max += pad;
            return (min, max);
        }

        double TimeToX(double time)
        {
            var (min, max) = TimeRange();
            var span = max - min;
            return span <= 0 ? 0 : (time - min) / span * ActualWidth;
        }

        double XToTime(double x)
        {
            var (min, max) = TimeRange();
            var span = max - min;
            if (ActualWidth <= 0 || span <= 0)
                return min;
            return min + Math.Clamp(x / ActualWidth, 0, 1) * span;
        }

        double ValueToY(double value, (double Min, double Max) range)
        {
            var height = ActualHeight - RulerHeight;
            var t = (value - range.Min) / (range.Max - range.Min);
            return RulerHeight + (1 - t) * height;
        }

        double YToValue(double y, (double Min, double Max) range)
        {
            var height = ActualHeight - RulerHeight;
            var t = 1 - (y - RulerHeight) / height;
            return range.Min + t * (range.Max - range.Min);
        }

        // ---------------------------------------------------------------------
        // Rendering
        // ---------------------------------------------------------------------

        protected override void OnRender(DrawingContext dc)
        {
            var background = new SolidColorBrush(Color.FromRgb(30, 30, 34));
            dc.DrawRectangle(background, null, new Rect(0, 0, ActualWidth, ActualHeight));
            if (ActualWidth < 10 || ActualHeight < RulerHeight + 10)
                return;

            var range = ValueRange();
            var timeRange = TimeRange();
            DrawGrid(dc, range, timeRange);
            DrawOutOfBoundsShading(dc, timeRange);
            DrawAliveWindowShading(dc);
            DrawRuler(dc, timeRange);

            foreach (var series in VisibleSeries())
                DrawSeries(dc, series, range, series == SelectedSeries);

            if (_selectedKey != null && _selectedKeySeries != null && _selectedKeySeries.IsVisible)
                DrawTangentHandles(dc, _selectedKeySeries, _selectedKey, range);

            // Timeline cursor.
            var cursorX = TimeToX(CurrentTime);
            dc.DrawLine(new Pen(Brushes.OrangeRed, 1.5), new Point(cursorX, 0), new Point(cursorX, ActualHeight));
        }

        void DrawGrid(DrawingContext dc, (double Min, double Max) range, (double Min, double Max) timeRange)
        {
            var gridPen = new Pen(new SolidColorBrush(Color.FromRgb(55, 55, 60)), 1);
            var textBrush = new SolidColorBrush(Color.FromRgb(140, 140, 150));

            var valueStep = NiceStep(range.Max - range.Min, 6);
            for (var v = Math.Ceiling(range.Min / valueStep) * valueStep; v <= range.Max; v += valueStep)
            {
                var y = ValueToY(v, range);
                dc.DrawLine(gridPen, new Point(0, y), new Point(ActualWidth, y));
                DrawText(dc, v.ToString("0.###"), textBrush, new Point(3, y - 14), 10);
            }

            var timeStep = NiceStep(timeRange.Max - timeRange.Min, 10);
            var startTime = Math.Floor(timeRange.Min / timeStep) * timeStep;
            for (var t = startTime; t <= timeRange.Max + 0.001; t += timeStep)
            {
                var x = TimeToX(t);
                dc.DrawLine(gridPen, new Point(x, RulerHeight), new Point(x, ActualHeight));
            }

            // Zero line, slightly stronger.
            if (range.Min < 0 && range.Max > 0)
            {
                var zeroPen = new Pen(new SolidColorBrush(Color.FromRgb(90, 90, 100)), 1);
                var y = ValueToY(0, range);
                dc.DrawLine(zeroPen, new Point(0, y), new Point(ActualWidth, y));
            }
        }

        /// <summary>Darkens the region(s) within [0, Duration] where the selected element isn't
        /// alive yet (before Begin) or not alive anymore (after End, unless TimingMode is
        /// "infinite") - anchored to <see cref="TimeToX"/> of 0/Duration rather than the control's
        /// own pixel edges, since <see cref="TimeRange"/> can now draw extra space on either side
        /// (see <see cref="DrawOutOfBoundsShading"/>, which covers that part instead).</summary>
        void DrawAliveWindowShading(DrawingContext dc)
        {
            if (string.IsNullOrEmpty(ElementTimingMode))
                return;

            var shade = new SolidColorBrush(Color.FromArgb(110, 0, 0, 0));
            var zeroX = TimeToX(0);
            var durationX = TimeToX(Duration);

            var beginX = TimeToX(Math.Clamp(ElementBegin, 0, Duration));
            if (beginX > zeroX)
                dc.DrawRectangle(shade, null, new Rect(zeroX, RulerHeight, beginX - zeroX, ActualHeight - RulerHeight));

            if (ElementTimingMode != "infinite")
            {
                var endX = TimeToX(Math.Clamp(ElementEnd, 0, Duration));
                if (endX < durationX)
                    dc.DrawRectangle(shade, null, new Rect(endX, RulerHeight, durationX - endX, ActualHeight - RulerHeight));
            }
        }

        /// <summary>Greys out the part of the drawn timeline before t=0 or after t=Duration -
        /// <see cref="TimeRange"/> widens the view to show out-of-bounds keyframes/Begin/End rather
        /// than hiding them, but nothing out here does anything at runtime (<see
        /// cref="CscElement.NormalizedWindow"/> clamps a negative time to 0 and one past Duration
        /// to Duration, no wraparound) - this marks at a glance which part of the drawn curve is
        /// actually part of the CSC's choreography. A thin line at the exact 0/Duration boundary
        /// backs up the shading in case the nearest grid line doesn't land exactly on it.</summary>
        void DrawOutOfBoundsShading(DrawingContext dc, (double Min, double Max) timeRange)
        {
            var shade = new SolidColorBrush(Color.FromArgb(150, 15, 15, 17));
            var boundaryPen = new Pen(new SolidColorBrush(Color.FromRgb(100, 100, 70)), 1);

            if (timeRange.Min < 0)
            {
                var zeroX = TimeToX(0);
                dc.DrawRectangle(shade, null, new Rect(0, RulerHeight, zeroX, ActualHeight - RulerHeight));
                dc.DrawLine(boundaryPen, new Point(zeroX, RulerHeight), new Point(zeroX, ActualHeight));
            }

            if (timeRange.Max > Duration)
            {
                var durationX = TimeToX(Duration);
                dc.DrawRectangle(shade, null, new Rect(durationX, RulerHeight, ActualWidth - durationX, ActualHeight - RulerHeight));
                dc.DrawLine(boundaryPen, new Point(durationX, RulerHeight), new Point(durationX, ActualHeight));
            }
        }

        void DrawRuler(DrawingContext dc, (double Min, double Max) timeRange)
        {
            var rulerBrush = new SolidColorBrush(Color.FromRgb(45, 45, 52));
            dc.DrawRectangle(rulerBrush, null, new Rect(0, 0, ActualWidth, RulerHeight));

            var textBrush = new SolidColorBrush(Color.FromRgb(170, 170, 180));
            var timeStep = NiceStep(timeRange.Max - timeRange.Min, 10);
            var startTime = Math.Floor(timeRange.Min / timeStep) * timeStep;
            for (var t = startTime; t <= timeRange.Max + 0.001; t += timeStep)
                DrawText(dc, t.ToString("0.##"), textBrush, new Point(TimeToX(t) + 2, 2), 10);
        }

        void DrawSeries(DrawingContext dc, CurveSeries series, (double Min, double Max) range, bool isSelected)
        {
            var colour = series.Colour;
            if (!isSelected)
                colour = Color.FromArgb(140, colour.R, colour.G, colour.B);
            var pen = new Pen(new SolidColorBrush(colour), isSelected ? 2 : 1.2);

            // Sample the channel across the width - cheap and handles every interpolation mode.
            var geometry = new StreamGeometry();
            using (var ctx = geometry.Open())
            {
                var started = false;
                for (double x = 0; x <= ActualWidth; x += 2)
                {
                    var value = series.Channel.Evaluate((float)XToTime(x));
                    var point = new Point(x, ValueToY(value, range));
                    if (!started)
                    {
                        ctx.BeginFigure(point, false, false);
                        started = true;
                    }
                    else
                    {
                        ctx.LineTo(point, true, false);
                    }
                }
            }
            geometry.Freeze();
            dc.DrawGeometry(null, pen, geometry);

            var keyBrush = new SolidColorBrush(series.Colour);
            foreach (var key in series.Channel.Keyframes)
            {
                var centre = new Point(TimeToX(key.Time), ValueToY(key.Value, range));
                var rect = new Rect(centre.X - KeySize / 2, centre.Y - KeySize / 2, KeySize, KeySize);
                dc.DrawRectangle(keyBrush, isSelected ? new Pen(Brushes.White, 1) : null, rect);
            }

            // Always shown - these mark whether the curve is authored to loop seamlessly, which is
            // useful information while editing regardless of whether scene playback is currently
            // set to loop (IsLooping was gated on Playback.Loop before, which defaults to off and
            // made the markers effectively invisible most of the time).
            DrawLoopWrapMarkers(dc, series, range, keyBrush);
        }

        /// <summary>Marks the left edge with the curve's end-of-timeline value and the right edge
        /// with its start-of-timeline value - the two values that must match for a seamless loop.
        /// Drawn oversized in the series' own colour with a black outline so they still read
        /// clearly against the background grid without losing which curve they belong to.</summary>
        void DrawLoopWrapMarkers(DrawingContext dc, CurveSeries series, (double Min, double Max) range, Brush brush)
        {
            var pen = new Pen(Brushes.Black, 1.5);

            // Anchored to TimeToX(0)/TimeToX(Duration), not the control's own pixel edges - those
            // only coincide when TimeRange isn't showing any out-of-bounds region.
            var zeroX = TimeToX(0);
            var durationX = TimeToX(Duration);

            var endValueY = ValueToY(series.Channel.Evaluate((float)Duration), range);
            dc.DrawGeometry(brush, pen, TriangleGeometry(new Point(zeroX, endValueY), pointsRight: true, sizeMultiplier: 1.8));

            var startValueY = ValueToY(series.Channel.Evaluate(0), range);
            dc.DrawGeometry(brush, pen, TriangleGeometry(new Point(durationX, startValueY), pointsRight: false, sizeMultiplier: 1.8));
        }

        /// <summary>Draws the selected keyframe's bezier tangent handles: TangentIn (affects the
        /// segment arriving from the previous key) and TangentOut (affects the segment leaving to
        /// the next key), each a small circle joined to the keyframe by a line, positioned at
        /// key.Time/Value + the tangent's (time, value) offset.</summary>
        void DrawTangentHandles(DrawingContext dc, CurveSeries series, CscKeyframe key, (double Min, double Max) range)
        {
            var centre = new Point(TimeToX(key.Time), ValueToY(key.Value, range));
            var linePen = new Pen(Brushes.White, 1);
            var handleBrush = new SolidColorBrush(Colors.White);
            var handleRadius = KeySize * 0.55;

            void DrawHandle(Coord2d tangent)
            {
                var handlePoint = new Point(TimeToX(key.Time + tangent.X), ValueToY(key.Value + tangent.Y, range));
                dc.DrawLine(linePen, centre, handlePoint);
                dc.DrawEllipse(handleBrush, new Pen(new SolidColorBrush(series.Colour), 1), handlePoint, handleRadius, handleRadius);
            }

            DrawHandle(key.TangentIn);
            DrawHandle(key.TangentOut);
        }

        /// <summary>Triangle with its apex at <paramref name="tip"/> and its base offset toward
        /// the CENTRE of the canvas - the control clips to bounds, so a base offset toward the
        /// edge (as a naive "arrow direction" reading of <paramref name="pointsRight"/> would
        /// suggest) puts almost the whole shape outside the clip region, leaving only a sliver at
        /// the tip visible. <paramref name="pointsRight"/> true means the tip sits at the LEFT
        /// edge (base extends rightward, into the canvas); false means the tip sits at the RIGHT
        /// edge (base extends leftward).</summary>
        static Geometry TriangleGeometry(Point tip, bool pointsRight, double sizeMultiplier = 1)
        {
            var size = KeySize * 0.9 * sizeMultiplier;
            var baseX = pointsRight ? tip.X + size : tip.X - size;
            var geometry = new StreamGeometry();
            using (var ctx = geometry.Open())
            {
                ctx.BeginFigure(tip, true, true);
                ctx.LineTo(new Point(baseX, tip.Y - size * 0.6), true, true);
                ctx.LineTo(new Point(baseX, tip.Y + size * 0.6), true, true);
            }
            geometry.Freeze();
            return geometry;
        }

        static void DrawText(DrawingContext dc, string text, Brush brush, Point origin, double size)
        {
            var formatted = new FormattedText(
                text, System.Globalization.CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                new Typeface("Segoe UI"), size, brush, 1.0);
            dc.DrawText(formatted, origin);
        }

        static double NiceStep(double span, int targetSteps)
        {
            if (span <= 0)
                return 1;
            var raw = span / targetSteps;
            var magnitude = Math.Pow(10, Math.Floor(Math.Log10(raw)));
            var normalized = raw / magnitude;
            var nice = normalized < 1.5 ? 1 : normalized < 3.5 ? 2 : normalized < 7.5 ? 5 : 10;
            return nice * magnitude;
        }

        // ---------------------------------------------------------------------
        // Interaction
        // ---------------------------------------------------------------------

        (CurveSeries Series, CscKeyframe Key)? HitTestKey(Point position)
        {
            var range = ValueRange();
            foreach (var series in VisibleSeries().Reverse())
            {
                foreach (var key in series.Channel.Keyframes)
                {
                    var centre = new Point(TimeToX(key.Time), ValueToY(key.Value, range));
                    if ((centre - position).Length <= HitRadius)
                        return (series, key);
                }
            }
            return null;
        }

        /// <summary>Hit-tests only the currently selected keyframe's two tangent handles (handles
        /// are only shown, and only draggable, while their keyframe is selected).</summary>
        bool? HitTestHandle(Point position)
        {
            if (_selectedKey == null || _selectedKeySeries == null || !_selectedKeySeries.IsVisible)
                return null;

            var range = ValueRange();
            var key = _selectedKey;
            var outPoint = new Point(TimeToX(key.Time + key.TangentOut.X), ValueToY(key.Value + key.TangentOut.Y, range));
            if ((outPoint - position).Length <= HitRadius)
                return true; // TangentOut

            var inPoint = new Point(TimeToX(key.Time + key.TangentIn.X), ValueToY(key.Value + key.TangentIn.Y, range));
            if ((inPoint - position).Length <= HitRadius)
                return false; // TangentIn

            return null;
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            Focus();
            var position = e.GetPosition(this);

            if (e.ClickCount == 2)
            {
                AddKeyAt(position);
                e.Handled = true;
                return;
            }

            if (position.Y <= RulerHeight)
            {
                _draggingTime = true;
                CurrentTime = Math.Clamp(XToTime(position.X), 0, Duration);
                CaptureMouse();
                e.Handled = true;
                return;
            }

            if (!IsReadOnlyStructure && HitTestHandle(position) is { } isOut)
            {
                StartEditGesture();
                _draggingHandleOut = isOut;
                _draggingHandleIn = !isOut;
                MarkHandleSegmentAsBezier(isOut);
                CaptureMouse();
                e.Handled = true;
                return;
            }

            if (HitTestKey(position) is { } hit)
            {
                StartEditGesture();
                _dragSeries = hit.Series;
                _dragKey = hit.Key;
                SelectedSeries = hit.Series;
                _selectedKey = hit.Key;
                _selectedKeySeries = hit.Series;
                CaptureMouse();
                e.Handled = true;
                return;
            }

            // Clicked empty canvas - hide the tangent handles.
            _selectedKey = null;
            _selectedKeySeries = null;
            InvalidateVisual();
        }

        /// <summary>
        /// Curve evaluation (<see cref="CscChannel.Evaluate"/>) only consults the earlier
        /// keyframe's ModeOut for a segment's shape, so a tangent drag only has a visible effect
        /// once that governing mode is "bezier_c" - flip it (and mirror the sibling ModeIn field
        /// for wire consistency) the moment the user grabs a handle to shape it.
        /// </summary>
        void MarkHandleSegmentAsBezier(bool isOut)
        {
            if (_selectedKey == null || _selectedKeySeries == null)
                return;

            var keys = _selectedKeySeries.Channel.Keyframes;
            var index = keys.IndexOf(_selectedKey);

            if (isOut)
            {
                _selectedKey.ModeOut = "bezier_c";
                if (index >= 0 && index + 1 < keys.Count)
                    keys[index + 1].ModeIn = "bezier_c";
            }
            else
            {
                _selectedKey.ModeIn = "bezier_c";
                if (index > 0)
                    keys[index - 1].ModeOut = "bezier_c";
            }

            CurvesModified?.Invoke();
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (_draggingTime)
            {
                CurrentTime = Math.Clamp(XToTime(e.GetPosition(this).X), 0, Duration);
                return;
            }

            if ((_draggingHandleOut || _draggingHandleIn) && _selectedKey != null)
            {
                var handlePos = e.GetPosition(this);
                handlePos.Y = Math.Clamp(handlePos.Y, RulerHeight, ActualHeight);
                var range = ValueRange();
                var offset = new Coord2d(
                    (float)(XToTime(handlePos.X) - _selectedKey.Time),
                    (float)(YToValue(handlePos.Y, range) - _selectedKey.Value));

                if (_draggingHandleOut)
                    _selectedKey.TangentOut = offset;
                else
                    _selectedKey.TangentIn = offset;

                CurvesModified?.Invoke();
                InvalidateVisual();
                return;
            }

            if (_dragKey == null || _dragSeries == null)
                return;

            var position = e.GetPosition(this);
            var dragRange = ValueRange();
            _dragKey.Time = (float)XToTime(position.X);
            _dragKey.Value = (float)YToValue(Math.Clamp(position.Y, RulerHeight, ActualHeight), dragRange);
            _dragSeries.Channel.SortKeyframes();
            CurvesModified?.Invoke();
            InvalidateVisual();
        }

        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e) => EndPointerInteraction();

        /// <summary>Capture can also be lost without a button-up (focus moves to another window,
        /// a dialog opens), which must still end the drag's undo step.</summary>
        protected override void OnLostMouseCapture(MouseEventArgs e)
        {
            EndPointerInteraction();
            base.OnLostMouseCapture(e);
        }

        void EndPointerInteraction()
        {
            _dragKey = null;
            _dragSeries = null;
            _draggingTime = false;
            _draggingHandleOut = false;
            _draggingHandleIn = false;
            if (IsMouseCaptured)
                ReleaseMouseCapture();
            CompleteEditGesture();
        }

        void StartEditGesture()
        {
            if (_editGestureActive)
                return;
            _editGestureActive = true;
            EditGestureStarted?.Invoke();
        }

        void CompleteEditGesture()
        {
            if (!_editGestureActive)
                return;
            _editGestureActive = false;
            EditGestureCompleted?.Invoke();
        }

        protected override void OnMouseRightButtonDown(MouseButtonEventArgs e)
        {
            if (IsReadOnlyStructure)
                return;

            if (HitTestKey(e.GetPosition(this)) is { } hit)
            {
                StartEditGesture();
                hit.Series.Channel.Keyframes.Remove(hit.Key);
                if (_selectedKey == hit.Key)
                {
                    _selectedKey = null;
                    _selectedKeySeries = null;
                }
                CurvesModified?.Invoke();
                InvalidateVisual();
                CompleteEditGesture();
                e.Handled = true;
            }
        }

        void AddKeyAt(Point position)
        {
            if (IsReadOnlyStructure)
                return;

            var series = SelectedSeries ?? VisibleSeries().FirstOrDefault();
            if (series == null)
                return;

            StartEditGesture();
            var range = ValueRange();
            var time = (float)XToTime(position.X);
            var value = position.Y <= RulerHeight
                ? series.Channel.Evaluate(time)
                : (float)YToValue(position.Y, range);

            var key = series.Channel.AddKeyframe(time, value);
            SelectedSeries = series;
            _selectedKey = key;
            _selectedKeySeries = series;
            CurvesModified?.Invoke();
            InvalidateVisual();
            CompleteEditGesture();
        }
    }
}
