using System;
using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace Shared.Ui.Common.Timeline
{
    /// <summary>
    /// Time to pixel mapping, ruler and playhead drawing shared by the timeline and the CSC curve editor.
    /// Lifted from CurveEditorControl so both controls draw the same axis.
    /// </summary>
    public readonly struct TimeAxis
    {
        public const double RulerHeight = 18;

        public double Min { get; }
        public double Max { get; }
        public double Width { get; }

        public TimeAxis(double min, double max, double width)
        {
            Min = min;
            Max = max;
            Width = width;
        }

        public double Span => Max - Min;

        public double TimeToX(double time) => Span <= 0 ? 0 : (time - Min) / Span * Width;

        public double XToTime(double x)
        {
            if (Width <= 0 || Span <= 0)
                return Min;
            return Min + Math.Clamp(x / Width, 0, 1) * Span;
        }

        public void DrawRuler(DrawingContext dc)
        {
            var rulerBrush = new SolidColorBrush(Color.FromRgb(45, 45, 52));
            dc.DrawRectangle(rulerBrush, null, new Rect(0, 0, Width, RulerHeight));

            var textBrush = new SolidColorBrush(Color.FromRgb(170, 170, 180));
            var timeStep = NiceStep(Span, 10);
            var startTime = Math.Floor(Min / timeStep) * timeStep;
            for (var t = startTime; t <= Max + 0.001; t += timeStep)
                DrawText(dc, t.ToString("0.##", CultureInfo.InvariantCulture), textBrush, new Point(TimeToX(t) + 2, 2), 10);
        }

        public void DrawPlayhead(DrawingContext dc, double time, double height, Brush brush)
        {
            var x = TimeToX(time);
            dc.DrawLine(new Pen(brush, 1.5), new Point(x, 0), new Point(x, height));
        }

        public static void DrawText(DrawingContext dc, string text, Brush brush, Point origin, double size)
        {
            var formatted = new FormattedText(
                text, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                new Typeface("Segoe UI"), size, brush, 1.0);
            dc.DrawText(formatted, origin);
        }

        public static double NiceStep(double span, int targetSteps)
        {
            if (span <= 0)
                return 1;
            var raw = span / targetSteps;
            var magnitude = Math.Pow(10, Math.Floor(Math.Log10(raw)));
            var normalized = raw / magnitude;
            var nice = normalized < 1.5 ? 1 : normalized < 3.5 ? 2 : normalized < 7.5 ? 5 : 10;
            return nice * magnitude;
        }
    }
}
