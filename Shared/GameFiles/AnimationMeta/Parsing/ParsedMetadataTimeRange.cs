using Shared.GameFormats.AnimationMeta.Definitions;

namespace Shared.GameFormats.AnimationMeta.Parsing
{
    public enum ParsedMetadataZeroRangeBehavior
    {
        Instant,
        WholeAnimation,
    }

    /// <summary>
    /// The authored Start/End time of a META tag. A 0/0 range on the tags in <see cref="WholeAnimationZeroRangeTags"/>
    /// means the tag applies to the whole animation; on any other tag it is an instant at 0.
    /// Ported from Szy-Cathay/TheAssetEditor-CN ParsedMetadataTimeRange.
    /// </summary>
    public readonly record struct ParsedMetadataTimeRange(float StartTime, float EndTime, ParsedMetadataZeroRangeBehavior ZeroRangeBehavior)
    {
        const float BoundaryToleranceSeconds = 0.000001f;

        // Every one of these appears at 0/0 in vanilla WH3 .meta files, mostly so (2026-11-23 scan of 68,625 files).
        // The meaning (whole animation rather than an instant at 0) is the fork's reading, and matches how the
        // SuperView preview treats DOCK_EQPT_* (active from Start to the end of the clip). CAMERA_SHAKE_SCALE,
        // IMPACT_POS and TARGET_POS are added here: the fork left them out, but they are mostly 0/0 in vanilla too.
        public static readonly IReadOnlySet<string> WholeAnimationZeroRangeTags = new HashSet<string>
        {
            "ALLOWED_DELTA_SCALE", "ALPHA", "ANIMATED_PROP", "BEARING", "BLEND_OVERRIDE", "BOUNDING_VOLUME_OVERRIDE",
            "CAMERA_SHAKE_SCALE", "CANNOT_DISMEMBER", "CREW_LOCATION", "DISABLE_FACIAL", "DISABLE_HEAD_TRACKING", "DISABLE_MODEL",
            "DISABLE_PERSISTENT", "DISABLE_PERSISTENT_ID", "DISABLE_PERSISTENT_VFX", "DISTANCE", "DOCK_EQPT_BACK",
            "DOCK_EQPT_LHAND", "DOCK_EQPT_LHAND_2", "DOCK_EQPT_LWAIST", "DOCK_EQPT_RHAND", "DOCK_EQPT_RHAND_2",
            "DOCK_EQPT_RWAIST", "FACE_POSE", "FULL_BODY", "IGNORE_FOOT_SLIDING", "IMPACT_POS", "IMPACT_SPEED", "LHAND_POSE",
            "MAX_TARGET_SIZE", "MIN_TARGET_SIZE", "NOT_BUILDING", "PARENT_CONSTRAINT", "POSITION", "PROP", "RESCALE",
            "RHAND_POSE", "RIDER_IDLE_SPEED_SCALE", "SC_HEIGHT", "SC_RADIUS", "SC_RATIO", "SHADER_PARAMETER", "SPLICE",
            "SPLICE_OVERRIDE", "TARGET_POS", "TRANSFORM", "USE_BASE_METADATA", "WEAPON_HIP", "WEAPON_LHAND", "WEAPON_ON",
            "WEAPON_RHAND", "WOUNDED_POSE",
        };

        public bool IsZeroRange => Math.Abs(StartTime) <= BoundaryToleranceSeconds && Math.Abs(EndTime) <= BoundaryToleranceSeconds;

        public bool IsWholeAnimationRange => IsZeroRange && ZeroRangeBehavior == ParsedMetadataZeroRangeBehavior.WholeAnimation;

        public bool IsInstant => !IsWholeAnimationRange && Math.Abs(EndTime - StartTime) <= BoundaryToleranceSeconds;

        public static bool TryCreate(ParsedMetadataAttribute attribute, out ParsedMetadataTimeRange timeRange)
        {
            switch (attribute)
            {
                case Effect_v1 effect:
                    timeRange = new(effect.EffectStartTime, effect.EffectEndTime, ParsedMetadataZeroRangeBehavior.Instant);
                    return true;
                case DecodedMetaEntryBase_v2 version2:
                    timeRange = new(version2.StartTime, version2.EndTime, GetZeroRangeBehavior(attribute));
                    return true;
                case DecodedMetaEntryBase version10:
                    timeRange = new(version10.StartTime, version10.EndTime, GetZeroRangeBehavior(attribute));
                    return true;
                default:
                    timeRange = default;
                    return false;
            }
        }

        static ParsedMetadataZeroRangeBehavior GetZeroRangeBehavior(ParsedMetadataAttribute attribute)
        {
            var tagName = attribute.GetType()
                .GetCustomAttributes(typeof(MetaDataAttribute), true)
                .OfType<MetaDataAttribute>()
                .FirstOrDefault()?.Name ?? attribute.Name;
            return tagName != null && WholeAnimationZeroRangeTags.Contains(tagName)
                ? ParsedMetadataZeroRangeBehavior.WholeAnimation
                : ParsedMetadataZeroRangeBehavior.Instant;
        }
    }
}
