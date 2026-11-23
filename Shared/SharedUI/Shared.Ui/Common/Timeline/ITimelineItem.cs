using System.Windows.Media;

namespace Shared.Ui.Common.Timeline
{
    /// <summary>
    /// One entry drawn by TimelineControl. End == null means an instant tick at Start.
    /// </summary>
    public interface ITimelineItem
    {
        double Start { get; }
        double? End { get; }
        int Lane { get; }
        Color Colour { get; }
        bool IsSelected { get; set; }
        string Tooltip { get; }
    }
}
