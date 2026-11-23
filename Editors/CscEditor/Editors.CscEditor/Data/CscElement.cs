using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Shared.GameFormats.Csc;
using Shared.GameFormats.Esf;

namespace Editors.CscEditor.Data
{
    public enum CscElementKind
    {
        Locator,
        Model,
        VariantModel,
        Vfx,
        Sfx,
        Animation,
        AnimationNodeTransform,
        Camera,
        PointLight,
        SpotLight,
        Prefab,
        RootRef,
        SoundSphere,
        CameraShake,
        Unknown,
    }

    /// <summary>
    /// One scene element: the ELEMENT record plus its type-specific sibling records from a single
    /// labeled element group under ROOT, decoded into an editable form. Unrecognized fields are
    /// carried along as raw <see cref="EsfNode"/>s so saving preserves them byte-for-byte.
    /// </summary>
    public class CscElement
    {
        public int Id { get; set; }
        public string GroupLabel { get; set; } = "model";
        public CscElementKind Kind { get; set; } = CscElementKind.Unknown;

        // ---- ELEMENT record header fields ----
        public string HeaderString { get; set; } = "";
        public float Begin { get; set; }
        public float End { get; set; } = 10;
        public string TimingMode { get; set; } = "infinite";     // "duration" | "infinite" | "pulse"
        public string AnchorMode { get; set; } = "free";         // terrain anchoring mode
        public byte ElementVersion { get; set; } = 100;

        // ---- ELEMENT channel groups. On ELEMENT v5 (see IsStaticTransform) Position, Rotation
        // and Scale are static stand-ins for the record's fixed transform and Weight is null. ----
        public CscChannelGroup? Position { get; set; }
        public CscChannelGroup? Rotation { get; set; }
        public CscChannelGroup? Scale { get; set; }
        public CscChannelGroup? Weight { get; set; }

        /// <summary>
        /// ELEMENT v5 has no channel groups. It stores one fixed transform: Coord3d position,
        /// quaternion (x, y, z, w), F32 scale, then the base placement as Coord3d + quaternion.
        /// Every v5 ELEMENT in vanilla WH3 (4,056 of them) uses this 17-field layout, and all
        /// 8,112 quaternions are unit length with w last. Position/Rotation/Scale hold it as
        /// static channels so the details panel and gizmo work unchanged; they can't take keys.
        /// </summary>
        public bool IsStaticTransform { get; internal set; }

        // The stored quaternions are kept as read, so a save that didn't touch the rotation
        // writes back the exact original values instead of a round trip through Euler angles.
        internal Quaternion StaticRotation { get; set; } = Quaternion.Identity;
        internal Quaternion StaticBaseRotation { get; set; } = Quaternion.Identity;
        internal Vector3 StaticRotationEuler { get; set; }
        internal Vector3 StaticBaseRotationEuler { get; set; }

        /// <summary>CAMERA_ELEMENT v1/v2 (only seen with ELEMENT v5) store plain floats instead of
        /// channel groups: v1 is just the FOV, v2 is FOV, near, far, the unknown Bool and three
        /// more floats. Held as static channels for the same reason as the transform.</summary>
        internal CscChannel? StaticCameraFov { get; set; }
        internal CscChannel? StaticCameraNear { get; set; }
        internal CscChannel? StaticCameraFar { get; set; }

        // ---- ELEMENT trailing placement transform (in-game confirmed) ----
        public Vector3 BasePosition { get; set; }
        public Vector3 BaseRotation { get; set; }   // Euler radians

        /// <summary>Original ELEMENT fields after the second trailing Coord3d (a Bool on v7/v100), preserved verbatim.</summary>
        public List<EsfNode> ElementTrailingFields { get; set; } = [];

        /// <summary>True when <see cref="ElementTrailingFields"/> holds the v7/v100 trailing Bool
        /// (absent on v6 and earlier ELEMENT shapes).
        /// Meaning unconfirmed, but real files show it varying rather than sitting at one
        /// constant value, so it's exposed for inspection/editing instead of only round-tripped
        /// verbatim.</summary>
        public bool HasElementTrailingBool => ElementTrailingFields is [{ Kind: EsfNodeKind.Bool }, ..];

        public bool ElementTrailingBool
        {
            get => HasElementTrailingBool && ElementTrailingFields[0].Value is true;
            set
            {
                if (HasElementTrailingBool)
                    ElementTrailingFields[0] = EsfNode.Leaf(EsfNodeKind.Bool, value);
            }
        }

        /// <summary>True when the ELEMENT record's channel-group layout was not recognized (e.g.
        /// legacy v5). The whole element group is then preserved verbatim and is read-only.</summary>
        public bool IsLegacyRaw { get; set; }

        /// <summary>All records of the element group in encounter order (ELEMENT first, usually).
        /// Used for verbatim preservation and as the patch target on save.</summary>
        public List<EsfNode> Records { get; set; } = [];
        public EsfNode? ElementRecord { get; set; }
        public EsfNode? TypeRecord { get; set; }

        /// <summary>Channel-group runs detected inside <see cref="TypeRecord"/> at load time
        /// (camera parameters, light colour/intensity/... groups), edited in place.</summary>
        public List<CscChannelGroup> TypeGroups { get; } = [];

        /// <summary>Primary asset reference: model path, VFX name, .anim path, .bmd prefab path,
        /// nested .csc path or the Wwise start event, depending on <see cref="Kind"/>.</summary>
        public string AssetPath { get; set; } = "";

        /// <summary>Second Wwise event (the stop event) for SFX elements - paired with <see cref="AssetPath"/>
        /// (event 1's start event).</summary>
        public string SfxStopEvent { get; set; } = "";

        /// <summary>SFX_ELEMENT can carry two more Ascii fields after the (start, stop) pair -
        /// confirmed (corpus-wide, versions with 4+ leading Ascii fields) to be a second,
        /// independent Wwise (start, stop) event pair: real instances show completely unrelated
        /// event names between the two pairs in the same record (e.g. one Battle_IND_Magic_...
        /// event and one unrelated Cam_Env_Sea_Lane_Teleport_... event together). Usually empty in
        /// any given file simply because most SFX elements only use one event pair, not because the
        /// slot is unused in general.</summary>
        public bool SfxHasSecondEventPair { get; set; }
        public string SfxEvent2Start { get; set; } = "";
        public string SfxEvent2Stop { get; set; } = "";

        // ---- ELEMENT_PERIOD (SFX/animation bundles): (flag, speed multiplier, time offset) ----
        public EsfNode? PeriodRecord { get; set; }
        public bool PeriodFlag { get; set; }
        public float PeriodSpeedMultiplier { get; set; } = 1;
        public float PeriodTimeOffset { get; set; }

        // ---- ANIMATION_SPLICE_ELEMENT: a sibling record alongside ANIMATION_ELEMENT within the
        // same element group (not nested inside it) - splices this .anim onto a specific bone and
        // its descendants rather than the whole skeleton. Field 0 = bone id (fairly confident
        // guess); fields 1/2 = depth/scope values whose exact meaning isn't confirmed. Everything
        // after that varies by record version (bools plus floats/an Ascii name) and is preserved
        // verbatim, shown read-only.
        public EsfNode? SpliceRecord { get; set; }
        public int SpliceBoneId { get; set; }
        public int SpliceDepthA { get; set; }
        public int SpliceDepthB { get; set; }
        public string SpliceRemainingFieldsDisplay { get; set; } = "";

        // ---- ANIMATION_NODE_TRANSFORM_ELEMENT (its own element kind, "Node Transform" in the
        // tree): this element's own Position/Rotation/Scale/Weight keyframe curves drive the bone
        // named here rather than placing the element itself - e.g. animating a jaw bone open while
        // the rest of the body plays a different/no clip. Field 1 is corpus-wide almost always the
        // -1 "no reference" sentinel; shown but not asserted to mean anything else. ----
        public int NodeTransformBoneId { get; set; } = -1;
        public int NodeTransformSecondValue { get; set; } = -1;

        // ---- Hierarchy (ROOT's attach tree) ----
        public CscElement? Parent { get; set; }
        public List<CscElement> Children { get; } = [];

        /// <summary>The scene this element belongs to - the main <see cref="CscScene"/> for a
        /// normal element, or a root-ref'd sub-scene's own <see cref="CscScene"/> instance for one
        /// of its elements. Stamped by <see cref="CscScene.Load"/>/<see cref="CscScene.AddElement"/>;
        /// used only to resolve <see cref="NormalizedWindow"/>'s wrap boundary, never serialized.</summary>
        public CscScene? Scene { get; set; }

        /// <summary>ATTACH_TO_PARAMETERS value: bone index within the parent model, -1 = model origin/animroot.</summary>
        public int AttachBoneIndex { get; set; } = -1;
        public EsfNode? AttachRecord { get; set; }

        /// <summary>True for elements nested inside another element's type record (e.g. an idle
        /// animation carried inside a VARIANT_MODEL_ELEMENT). Nested elements are not part of
        /// ROOT's attach tree - their records are written back in place inside the carrier - so
        /// they can be edited but not re-parented or deleted individually.</summary>
        public bool IsNested { get; set; }

        /// <summary>True for elements of a .csc loaded through a ROOT_REF_ELEMENT. They are
        /// display-only: shown in the 3D view and the tree, but never part of the host scene's
        /// data, so they cannot be edited, re-parented or deleted.</summary>
        public bool IsExternal { get; set; }

        /// <summary>This element's COMPOSITE_SCENE manifest group fields (raw slice), captured at
        /// load so channel names/flags survive a save even though keyframe counts are re-derived.</summary>
        public List<EsfNode>? ManifestGroupFields { get; set; }

        public string DisplayName
        {
            get
            {
                var asset = AssetPath;
                if (!string.IsNullOrEmpty(asset))
                {
                    var slash = asset.Replace('/', '\\').LastIndexOf('\\');
                    if (slash >= 0 && slash < asset.Length - 1)
                        asset = asset[(slash + 1)..];
                }

                if (string.IsNullOrEmpty(asset))
                    asset = string.IsNullOrEmpty(HeaderString) ? GroupLabel : HeaderString;

                return $"[{Id}] {asset}";
            }
        }

        // ---------------------------------------------------------------------
        // Channel access helpers
        // ---------------------------------------------------------------------

        public Vector3 EvaluatePosition(float t) => EvaluateVector(Position, t, Vector3.Zero);
        public Vector3 EvaluateRotation(float t) => EvaluateVector(Rotation, t, Vector3.Zero);
        public float EvaluateScale(float t) => Scale?.Channels.Count > 0 ? Scale.Channels[0].Evaluate(t) : 1;
        public float EvaluateWeight(float t) => Weight?.Channels.Count > 0 ? Weight.Channels[0].Evaluate(t) : 1;

        static Vector3 EvaluateVector(CscChannelGroup? group, float t, Vector3 fallback)
        {
            if (group == null || group.Channels.Count < 3)
                return fallback;
            return new Vector3(group.Channels[0].Evaluate(t), group.Channels[1].Evaluate(t), group.Channels[2].Evaluate(t));
        }

        /// <summary>
        /// <see cref="Begin"/>/<see cref="End"/> clamped into [0, Scene.Duration]. Real files (and
        /// hand edits) can carry a negative Begin/End or one past the scene's own duration - a
        /// negative time just reads as 0, a time past the scene's own duration reads as the
        /// duration (no wraparound). The raw <see cref="Begin"/>/<see cref="End"/> properties are
        /// left untouched - they're the authored/editable values shown as-is in the UI (and in the
        /// curve editor, which draws the true out-of-bounds region rather than hiding it) - every
        /// runtime consumer of the active window should read this instead. Falls back to just a
        /// lower clamp at 0 when there's no <see cref="Scene"/> to size the upper bound against
        /// (e.g. a bare element in a unit test) or its duration is non-positive.
        /// </summary>
        public (float Begin, float End) NormalizedWindow
        {
            get
            {
                var duration = Scene?.Duration ?? 0f;
                var begin = MathF.Max(Begin, 0f);
                var end = MathF.Max(End, 0f);
                if (duration > 0)
                {
                    begin = MathF.Min(begin, duration);
                    end = MathF.Min(end, duration);
                }
                return (begin, end);
            }
        }

        /// <summary>Convenience accessors for <see cref="NormalizedWindow"/>'s two components -
        /// every runtime (non-UI) consumer of the active window should read these, not the raw
        /// <see cref="Begin"/>/<see cref="End"/>.</summary>
        public float NormalizedBegin => NormalizedWindow.Begin;
        public float NormalizedEnd => NormalizedWindow.End;

        /// <summary>
        /// Clamps <paramref name="t"/> into the element's own (normalized) active window before it
        /// reaches the keyframe curves, so begin/end timing is authoritative over what the curves
        /// would otherwise keep producing - an element frozen or force-shown (e.g. while selected)
        /// outside its window holds its boundary pose instead of continuing to animate past it.
        /// </summary>
        float ClampToActiveWindow(float t)
        {
            var (begin, end) = NormalizedWindow;

            if (TimingMode == "infinite")
                return MathF.Min(t, end);
            if (end < begin) // malformed data guard
                return MathF.Max(t, begin);
            return Math.Clamp(t, begin, end);
        }

        /// <summary>
        /// Local transform at scene time <paramref name="t"/>. The trailing base placement is
        /// applied before the channel transform - in-game testing showed the base position offsets
        /// the mesh away from the channel rotation's pivot while the spin continues around that
        /// pivot, which is exactly this composition order (row-vector convention: base first).
        /// </summary>
        public Matrix LocalTransform(float t)
        {
            var channelTime = ClampToActiveWindow(t);

            var pos = EvaluatePosition(channelTime);
            var rot = EvaluateRotation(channelTime);
            var scale = EvaluateScale(channelTime);

            var channelRotation = IsStaticTransform ? Matrix.CreateFromQuaternion(CurrentStaticRotation()) : EulerMatrix(rot);
            var baseRotation = IsStaticTransform ? Matrix.CreateFromQuaternion(CurrentStaticBaseRotation()) : EulerMatrix(BaseRotation);
            var channelMatrix = Matrix.CreateScale(scale) * channelRotation * Matrix.CreateTranslation(pos);
            var baseMatrix = baseRotation * Matrix.CreateTranslation(BasePosition);
            return baseMatrix * channelMatrix;
        }

        /// <summary>The v5 rotation as a quaternion: the stored one while the Euler stand-in is
        /// unchanged, otherwise rebuilt from the edited Euler angles.</summary>
        internal Quaternion CurrentStaticRotation() => EvaluateRotation(0) == StaticRotationEuler
            ? StaticRotation
            : Quaternion.CreateFromRotationMatrix(EulerMatrix(EvaluateRotation(0)));

        internal Quaternion CurrentStaticBaseRotation() => BaseRotation == StaticBaseRotationEuler
            ? StaticBaseRotation
            : Quaternion.CreateFromRotationMatrix(EulerMatrix(BaseRotation));

        /// <summary>
        /// Extracts (x, y, z) Euler angles from a rotation matrix under the Rx*Ry*Rz row-vector
        /// composition this editor uses for CSC rotations (<see cref="EulerMatrix"/>).
        /// </summary>
        internal static Vector3 ToEulerXyz(Matrix m)
        {
            var y = MathF.Asin(Math.Clamp(-m.M13, -1f, 1f));
            return MathF.Abs(m.M13) < 0.9999f
                ? new Vector3(MathF.Atan2(m.M23, m.M33), y, MathF.Atan2(m.M12, m.M11))
                : new Vector3(MathF.Atan2(-m.M32, m.M22), y, 0);
        }

        public static Matrix EulerMatrix(Vector3 euler) =>
            Matrix.CreateRotationX(euler.X) * Matrix.CreateRotationY(euler.Y) * Matrix.CreateRotationZ(euler.Z);

        /// <summary>World transform at time t, composed through the attach-tree parent chain.</summary>
        public Matrix WorldTransform(float t)
        {
            var m = LocalTransform(t);
            var p = Parent;
            while (p != null)
            {
                m *= p.LocalTransform(t);
                p = p.Parent;
            }
            return m;
        }

        /// <summary>Whether the element is showing at scene time <paramref name="t"/>: inside its
        /// own normalized Begin/End window (see <see cref="NormalizedWindow"/>) and its Weight
        /// channel isn't zeroed out there. "infinite" mode has no upper cutoff - alive forever once
        /// past Begin.</summary>
        public bool IsAliveAt(float t)
        {
            var (begin, end) = NormalizedWindow;

            if (t < begin)
                return false;
            if (TimingMode != "infinite" && t > end)
                return false;
            return EvaluateWeight(ClampToActiveWindow(t)) > 0;
        }

        // ---------------------------------------------------------------------
        // Type-specific channel-group aliases (indexes into TypeGroups)
        // ---------------------------------------------------------------------

        public CscChannel? TypeChannel(int groupIndex, int channelIndex = 0)
        {
            if (groupIndex < 0 || groupIndex >= TypeGroups.Count)
                return null;
            var channels = TypeGroups[groupIndex].Channels;
            return channelIndex < channels.Count ? channels[channelIndex] : null;
        }

        // Lights: group 0 = colour (R/G/B, 3 channels).
        public Vector3 LightColour(float t)
        {
            if (TypeGroups.Count == 0 || TypeGroups[0].Channels.Count < 3)
                return Vector3.One;
            var g = TypeGroups[0];
            return new Vector3(g.Channels[0].Evaluate(t), g.Channels[1].Evaluate(t), g.Channels[2].Evaluate(t));
        }

        // Point light group order: colour(3), intensity, range, scatter_diameter (CA's own manifest names).
        public CscChannel? PointLightIntensity => Kind == CscElementKind.PointLight ? TypeChannel(1) : null;
        public CscChannel? PointLightRange => Kind == CscElementKind.PointLight ? TypeChannel(2) : null;

        // Spot light group order: colour(3), intensity, length, inner_angle, outer_angle, scatter_diameter.
        public CscChannel? SpotLightIntensity => Kind == CscElementKind.SpotLight ? TypeChannel(1) : null;
        public CscChannel? SpotLightLength => Kind == CscElementKind.SpotLight ? TypeChannel(2) : null;
        public CscChannel? SpotLightInnerAngle => Kind == CscElementKind.SpotLight ? TypeChannel(3) : null;
        public CscChannel? SpotLightOuterAngle => Kind == CscElementKind.SpotLight ? TypeChannel(4) : null;

        // Camera group order (group indexes; interleaved Bools don't count) - corpus-confirmed,
        // same for every CAMERA_ELEMENT version: fov(deg), roll(radians), near, far, focal depth,
        // focal width, focal falloff near/far, kernal radius fg/bg, pivot distance. Every group
        // after far sits at its default value in every file checked so far, so their meaning
        // (beyond the pre-existing manifest channel names below) is unconfirmed.
        static int CameraGroupIndex(string name) => name switch
        {
            "fov" => 0, "roll" => 1, "near" => 2, "far" => 3, "focal_depth" => 4, _ => -1
        };

        /// <summary>The real near/far render clip planes (not depth-of-field - camera_focal_depth,
        /// group index 4, is always some default value in every file checked and isn't used for
        /// anything; see CameraGroupNames for the full group list).</summary>
        public CscChannel? CameraFov => Kind == CscElementKind.Camera ? StaticCameraFov ?? TypeChannel(CameraGroupIndex("fov")) : null;
        public CscChannel? CameraRoll => Kind == CscElementKind.Camera ? TypeChannel(CameraGroupIndex("roll")) : null;
        public CscChannel? CameraNear => Kind == CscElementKind.Camera ? StaticCameraNear ?? TypeChannel(CameraGroupIndex("near")) : null;
        public CscChannel? CameraFar => Kind == CscElementKind.Camera ? StaticCameraFar ?? TypeChannel(CameraGroupIndex("far")) : null;

        /// <summary>Identity of the original Bool node sitting between the roll and near groups of
        /// every CAMERA_ELEMENT (any version) - not part of the Group/Channel/Curve grammar (see
        /// CurveDetector), so it isn't a <see cref="TypeGroups"/> entry. Never reassigned after
        /// decode: <see cref="CscSceneWriter"/> matches on this exact reference to splice
        /// <see cref="CameraUnknownFlag"/>'s edited value back into the right raw field on save.
        /// Meaning unconfirmed.</summary>
        public EsfNode? CameraUnknownFlagNode { get; set; }

        public bool HasCameraUnknownFlag => CameraUnknownFlagNode != null;

        public bool CameraUnknownFlag { get; set; }

        public static readonly string[] CameraGroupNames =
        [
            "camera_fov", "camera_roll", "camera_near_distance", "camera_far_distance", "camera_focal_depth",
            "camera_focal_width", "camera_focal_falloff_near", "camera_focal_falloff_far",
            "camera_kernal_radius_foreground", "camera_kernal_radius_background", "camera_pivot_distance",
        ];

        public static readonly string[] PointLightGroupNames =
            ["point_light_colour", "point_light_intensity", "point_light_range", "point_light_scatter_diameter"];

        public static readonly string[] SpotLightGroupNames =
        [
            "spot_light_colour", "spot_light_intensity", "spot_light_length",
            "spot_light_inner_angle", "spot_light_outer_angle", "spot_light_scatter_diameter",
        ];

        public string TypeGroupName(int groupIndex)
        {
            var names = Kind switch
            {
                CscElementKind.Camera => CameraGroupNames,
                CscElementKind.PointLight => PointLightGroupNames,
                CscElementKind.SpotLight => SpotLightGroupNames,
                CscElementKind.RootRef => (string[])["game_time_multiplier", "scene_time_multiplier"],
                _ => [],
            };
            return groupIndex < names.Length ? names[groupIndex] : $"param {groupIndex}";
        }

        /// <summary>
        /// Keyframe counts of every channel, ELEMENT groups first (position, rotation, scale,
        /// weight) then the type record's groups - the exact order COMPOSITE_SCENE's manifest
        /// restates them in.
        /// </summary>
        public List<int> AllChannelKeyframeCounts()
        {
            var counts = new List<int>();
            foreach (var group in ElementGroups().Concat(TypeGroups))
                foreach (var channel in group.Channels)
                    counts.Add(channel.Keyframes.Count);
            return counts;
        }

        /// <summary>The ELEMENT record's channel groups as the file stores them. None for v5's
        /// static transform, whose stand-in groups aren't channel data.</summary>
        public IEnumerable<CscChannelGroup> ElementGroups()
        {
            if (IsStaticTransform) yield break;
            if (Position != null) yield return Position;
            if (Rotation != null) yield return Rotation;
            if (Scale != null) yield return Scale;
            if (Weight != null) yield return Weight;
        }

        public bool IsInSubtreeOf(CscElement other)
        {
            var current = this;
            while (current != null)
            {
                if (current == other)
                    return true;
                current = current.Parent;
            }
            return false;
        }
    }
}
