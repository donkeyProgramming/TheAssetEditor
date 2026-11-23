using Shared.GameFormats.AnimationMeta.Parsing;

namespace Editors.AnimationMeta.SuperView.Timeline
{
    public enum MetaDataDocumentOwner
    {
        Persistent,
        Animation,
    }

    public enum MetaDataTimelineMarkerKind
    {
        Instant,
        Range,
        WholeAnimation,
    }

    public sealed record MetaDataInspectionSource(ParsedMetadataAttribute Source, MetaDataDocumentOwner Owner, bool AreFieldsValid = true);

    public sealed record MetaDataInspectionItem(
        ParsedMetadataAttribute Source,
        MetaDataDocumentOwner Owner,
        ParsedMetadataTimeRange? TimeRange,
        MetaDataTimelineMarkerKind? MarkerKind);

    /// <summary>
    /// The timing of every tag in the persistent and animation META files: whether it shows on the timeline, and as what.
    /// Ported from Szy-Cathay/TheAssetEditor-CN MetaDataInspectionIndex, timing only (master has none of the fork's
    /// preview diagnostics).
    /// Unlike the fork, negative and past-the-clip times still get a marker: vanilla has hundreds of tags authored a few
    /// milliseconds before 0, and the timeline draws those at the clip's edge.
    /// </summary>
    public sealed class MetaDataInspectionIndex
    {
        public IReadOnlyList<MetaDataInspectionItem> Items { get; }

        MetaDataInspectionIndex(IReadOnlyList<MetaDataInspectionItem> items) => Items = items;

        public static MetaDataInspectionIndex Create(IEnumerable<MetaDataInspectionSource> sources) =>
            new(sources.Select(CreateItem).ToArray());

        static MetaDataInspectionItem CreateItem(MetaDataInspectionSource source)
        {
            if (!ParsedMetadataTimeRange.TryCreate(source.Source, out var timeRange))
                return new(source.Source, source.Owner, null, null);

            return new(source.Source, source.Owner, timeRange, source.AreFieldsValid ? Classify(timeRange) : null);
        }

        static MetaDataTimelineMarkerKind? Classify(ParsedMetadataTimeRange timeRange)
        {
            if (!float.IsFinite(timeRange.StartTime) || !float.IsFinite(timeRange.EndTime))
                return null;
            if (timeRange.IsWholeAnimationRange)
                return MetaDataTimelineMarkerKind.WholeAnimation;
            // An end before the start (one vanilla SPLICE) has no span to draw, so it shows where it starts.
            if (timeRange.IsInstant || timeRange.EndTime < timeRange.StartTime)
                return MetaDataTimelineMarkerKind.Instant;
            return MetaDataTimelineMarkerKind.Range;
        }
    }
}
