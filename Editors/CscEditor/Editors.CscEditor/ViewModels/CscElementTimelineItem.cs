using System.Globalization;
using System.Windows.Media;
using Editors.CscEditor.Data;
using Shared.Core.Misc;
using Shared.Ui.Common.Timeline;

namespace Editors.CscEditor.ViewModels
{
    /// <summary>One element's active window (Begin to End, or to the scene's end for "infinite") in the
    /// element rows above the curve editor. Reads the element live; <see cref="Refresh"/> reports changes.</summary>
    public sealed class CscElementTimelineItem : NotifyPropertyChangedImpl, ITimelineItem
    {
        readonly Func<double> _sceneDuration;
        double _start;
        double? _end;
        string _tooltip = "";

        public CscElementViewModel Element { get; }

        public double Start => _start;
        public double? End => _end;
        public int Lane => 0;
        public Color Colour { get; }
        public string Tooltip => _tooltip;

        bool _isSelected;
        public bool IsSelected { get => _isSelected; set => SetAndNotify(ref _isSelected, value); }

        public CscElementTimelineItem(CscElementViewModel element, Func<double> sceneDuration)
        {
            Element = element;
            _sceneDuration = sceneDuration;
            Colour = KindColour(element.Element.Kind);
            Read();
        }

        public void Refresh()
        {
            var (start, end, tooltip) = (_start, _end, _tooltip);
            Read();
            if (start != _start)
                NotifyPropertyChanged(nameof(Start));
            if (end != _end)
                NotifyPropertyChanged(nameof(End));
            if (tooltip != _tooltip)
                NotifyPropertyChanged(nameof(Tooltip));
        }

        void Read()
        {
            var element = Element.Element;
            _start = element.Begin;
            var isInfinite = element.TimingMode == "infinite";
            var end = isInfinite ? Math.Max(_sceneDuration(), element.Begin) : element.End;
            // An end before the begin has no span to draw, so it shows where it begins.
            _end = end < element.Begin ? null : end;

            var endText = isInfinite ? "∞" : element.End.ToString("0.###", CultureInfo.InvariantCulture);
            _tooltip = $"[{Element.Id}] {Element.DisplayName} ({Element.KindName})\n" +
                       $"{element.Begin.ToString("0.###", CultureInfo.InvariantCulture)} - {endText} s, {element.TimingMode}";
        }

        static Color KindColour(CscElementKind kind) => kind switch
        {
            CscElementKind.Model or CscElementKind.VariantModel or CscElementKind.Prefab => Color.FromRgb(110, 160, 220),
            CscElementKind.Animation or CscElementKind.AnimationNodeTransform => Color.FromRgb(120, 200, 140),
            CscElementKind.Vfx => Color.FromRgb(230, 150, 80),
            CscElementKind.Sfx or CscElementKind.SoundSphere => Color.FromRgb(200, 130, 210),
            CscElementKind.Camera or CscElementKind.CameraShake => Color.FromRgb(230, 210, 100),
            CscElementKind.PointLight or CscElementKind.SpotLight => Color.FromRgb(240, 240, 190),
            CscElementKind.RootRef => Color.FromRgb(90, 200, 210),
            _ => Color.FromRgb(150, 150, 160),
        };
    }
}
