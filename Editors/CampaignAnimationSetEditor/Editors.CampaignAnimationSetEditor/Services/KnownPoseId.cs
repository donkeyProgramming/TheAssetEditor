using System.Linq;

namespace Editors.CampaignAnimationSetEditor.Services
{
    /// <summary>PersistentMeta_Pose.PoseId is its own small numbering scheme, not an index into the
    /// battle animation slot list (which puts HAND_POSE_RELAX in the 400s; PoseId never exceeds 19).
    /// Confirmed by cross-referencing each of 994 Poses rows against its own animation filename
    /// across 661 vanilla files - every PoseId=0 row points at "..._hand_pose_relax...", every
    /// PoseId=10 at "..._face_pose_relax...", and so on for all observed ids (0-8, 10-19). Two
    /// outliers (2057, 2222) look like authoring mistakes and are excluded.</summary>
    public enum KnownPoseId
    {
        HAND_POSE_RELAX = 0,
        HAND_POSE_FLAT = 1,
        HAND_POSE_CLENCH = 2,
        HAND_POSE_GRIP = 3,
        HAND_POSE_HALF_OPEN = 4,
        HAND_POSE_THUMB_GRIP = 5,
        HAND_POSE_CUSTOM_1 = 6,
        HAND_POSE_CUSTOM_2 = 7,
        HAND_POSE_CUSTOM_3 = 8,

        /// <summary>Never observed - inferred from the otherwise gap-free sequence.</summary>
        HAND_POSE_CUSTOM_4 = 9,

        FACE_POSE_RELAX = 10,
        FACE_POSE_RELAX_BLINK = 11,
        FACE_POSE_ANGRY = 12,
        FACE_POSE_ANGRY_BLINK = 13,
        FACE_POSE_ANGRY_SCREAM = 14,
        FACE_POSE_DEAD = 15,
        FACE_POSE_DEAD2 = 16,
        FACE_POSE_DEATH_SCREAM = 17,
        FACE_POSE_WORRIED = 18,
        FACE_POSE_WORRIED_BLINK = 19,
    }

    /// <summary>(Name, Value) pairs for ComboBox ItemsSource/SelectedValuePath binding, since
    /// PersistentMeta_Pose.PoseId is a plain int rather than this enum type.</summary>
    public static class KnownPoseIdOptions
    {
        public record Option(string Name, int Value);

        public static readonly Option[] All = System.Enum.GetValues<KnownPoseId>()
            .Select(v => new Option(v.ToString(), (int)v))
            .ToArray();
    }
}
