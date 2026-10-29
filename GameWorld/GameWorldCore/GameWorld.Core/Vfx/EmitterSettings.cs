using Microsoft.Xna.Framework;
using Shared.GameFormats.Vfx;

namespace GameWorld.Core.Vfx
{
    public enum SimulationSpace { World, LocalPositionRotation, LocalPositionOnly }

    // The first six come from vs_translation_velocity_v2's orientation_mode; Upright, Flat and Object3D are vs_translation's
    // restriction 1, 2 and 3 (CA's "Vertical, Horizontal, 3D"). VelocityAligned is orientation_mode 5, "Camera-aligned with
    // stretching factor", and VelocityAxis its mode 2, "Camera-facing velocity-aligned".
    public enum QuadOrientation { CameraFacing, VelocityAligned, Horizontal, Vertical, LocalHorizontal, LocalVertical, Upright, Flat, Object3D, VelocityAxis }

    /// <summary>ps_lighting: how much sunlight small (Rayleigh) and large (Henyey-Greenstein) particles scatter, the large ones'
    /// directionality (g), how far the light is drawn toward emissive, and the average size the light crosses.</summary>
    public record ParticleLighting(float Small, float Large, float Directionality, float Emissive, float AverageSize);

    /// <summary>A particle event's effect, ready to run: a copy per particle, started at its birth or its death plus Offset seconds,
    /// following what of the particle the flags say (ParticleSimulator).</summary>
    public record ParticleEvent(bool OnDeath, IReadOnlyList<EmitterSettings> Emitters, bool TrackPosition = false, bool TrackScale = false,
        bool TrackColour = false, bool TrackAlpha = false, bool TrackLifetime = false, VfxEventTurn Turn = VfxEventTurn.None, float Offset = 0);

    /// <summary>
    /// One emitter, read into what ParticleSimulator and ParticleRenderItem use. Its setters let a test
    /// build one directly; From reads one out of a resolved emitter, and lists in Skipped what the preview
    /// does not simulate, for the log.
    /// </summary>
    public class EmitterSettings
    {
        public string Name { get; set; } = "";
        public bool Draws { get; set; } = true;
        public List<string> Skipped { get; } = [];

        // Placement and timing
        public Matrix ReferenceTransform { get; set; } = Matrix.Identity;
        public float StartOffset { get; set; }
        public (float Min, float Max) StartTime { get; set; }
        public (float Min, float Max) LifeTime { get; set; } = (1, 1);
        public (float Min, float Max) EmitterLifeBias { get; set; }
        public bool InfiniteLife { get; set; }
        public float SpeedMultiplier { get; set; } = 1;
        public SimulationSpace Space { get; set; }
        public VfxCurve? EmitterPosition { get; set; }
        public VfxCurve? EmitterOrientation { get; set; }
        public bool LoopPosition { get; set; }
        public bool LoopOrientation { get; set; }

        // Spawning
        public VfxCurve? Rate { get; set; }
        public bool LoopRate { get; set; }
        public (float Min, float Max) RateScale { get; set; } = (1, 1);
        public (float Min, float Max)? BurstInterval { get; set; }
        public (float Min, float Max) BurstLength { get; set; }
        public int MaxParticles { get; set; } = ParticleSimulator.ParticleCap;
        public VfxCurve? SpawnBox { get; set; }
        public VfxCurve? SpawnRing { get; set; }
        public bool LoopArea { get; set; }
        public Vector2 SpawnAngle { get; set; }
        public Vector2 SpawnArc { get; set; }
        public (float Min, float Max) Spread { get; set; } = (90, 90);
        /// <summary>Metres the emitter moves per extra particle, on top of the rate; 0 is none.</summary>
        public float SpawnSpacing { get; set; }
        /// <summary>MaximumVelocity: the most a particle's speed reaches, in m/s; 0 for no cap.</summary>
        public float MaxSpeed { get; set; }
        /// <summary>SkipParticlePositionUpdate: particles stay where they spawned; their velocity still aligns and stretches them.</summary>
        public bool SkipPositionUpdate { get; set; }
        /// <summary>SpawnInheritedSpeed: the share of the emitter's own velocity a particle starts with.</summary>
        public float InheritedSpeed { get; set; }
        /// <summary>SpawnInheritedDirectionBlend: VfxConventions.InheritDirection.</summary>
        public float InheritedDirectionBlend { get; set; }
        /// <summary>OnParticleSpawn and OnParticleDeath: the effects each particle starts at its birth and its death.</summary>
        public IReadOnlyList<ParticleEvent> Events { get; set; } = [];
        /// <summary>ParticlesSpawnOnTerrain: particles start on the ground.</summary>
        public bool SpawnOnTerrain { get; set; }
        /// <summary>EnableCollisions, with CollisionBouncingFactor and CollisionDampingFactor drawn per particle.</summary>
        public bool Collisions { get; set; }
        public (float Min, float Max) Bounce { get; set; }
        public (float Min, float Max) Damping { get; set; }
        /// <summary>vs_flip_uv: the chance a particle's texture is mirrored left to right, and top to bottom.</summary>
        public float FlipU { get; set; }
        public float FlipV { get; set; }
        /// <summary>vs_flip_uv uv_orientation: a quarter turn of the texture, 1 counter-clockwise, -1 clockwise.</summary>
        public int UvTurn { get; set; }
        /// <summary>CullDistances and ParticleCullDistances, metres from the camera: fade in over x..y, out over z..w.</summary>
        public Vector4 EmitterCull { get; set; } = NoCull;
        public Vector4 ParticleCull { get; set; } = NoCull;
        /// <summary>vs_rendersize_limiter pixelsize_limit: the fewest pixels a particle is drawn across.</summary>
        public float MinPixels { get; set; }
        /// <summary>ps_soft_edge soft_factor, metres (0: a hard edge), and scale_strength.</summary>
        public float SoftEdge { get; set; }
        public float SoftStrength { get; set; } = 1;
        static readonly Vector4 NoCull = new(0, 0, float.MaxValue, float.MaxValue);

        // A particle's start
        public VfxCurve? InitialSpeed { get; set; }
        public VfxCurve? InitialScale { get; set; }
        public VfxCurve? InitialAlpha { get; set; }
        public VfxCurve? ParticleLife { get; set; }
        public bool LoopSpeed { get; set; }
        public bool LoopInitial { get; set; }
        public bool LoopLife { get; set; }
        public (float Min, float Max) LifeBias { get; set; }
        public Vector4 TintMin { get; set; } = Vector4.One;
        public Vector4 TintMax { get; set; } = Vector4.One;
        public (float Min, float Max) Angle { get; set; }
        public (float Min, float Max) Spin { get; set; }
        public float SpinSign { get; set; } = 1;

        // Over a particle's life
        public VfxCurve? Scale { get; set; }
        public VfxCurve? Deceleration { get; set; }
        public VfxCurve? SpinDeceleration { get; set; }
        public VfxCurve?[] Forces { get; set; } = [];
        public VfxCurve? Alpha { get; set; }
        /// <summary>vs_vertex_alpha's alpha_START_offset and alpha_END_offset (VfxConventions.AlphaLife); zero without them.</summary>
        public Vector2 AlphaStartOffset { get; set; }
        public Vector2 AlphaEndOffset { get; set; }
        public VfxCurve? Colour { get; set; }
        /// <summary>ps_emissive's emissive_mul; 1 without one.</summary>
        public float EmissiveMul { get; set; } = 1;
        public VfxCurve? Stretch { get; set; }

        // Look
        public string? Texture { get; set; }
        public bool Premultiplied { get; set; } = true;
        /// <summary>ps_texture's overlay_colour: the colour is overlaid on the texture instead of multiplied in.</summary>
        public bool Overlay { get; set; }
        public float AdditiveMul { get; set; }
        /// <summary>ps_normal: the ps_texture normal map that shapes a lit chain's light; null without ps_normal.</summary>
        public string? NormalMap { get; set; }
        /// <summary>ps_lighting's settings; null without one.</summary>
        public ParticleLighting? Lighting { get; set; }
        /// <summary>Drawn lit: ps_lighting on a chain blended by alpha that lies over the scene. An additive chain stays unlit, as the
        /// preview's additive reading (VfxConventions.Blend) is not CA's, and a decal paints what lies under it.</summary>
        public bool Lit => Lighting != null && AdditiveMul == 0 && !Decal && !Light && !Distortion;
        public Vector2 Pivot { get; set; } = new(0.5f, 0.5f);
        public QuadOrientation Orientation { get; set; }
        /// <summary>enable_trail_mode on the render stack: the particles drawn as one strip, not a quad each.</summary>
        public bool Trail { get; set; }
        public float StretchCap { get; set; }
        public int CellsX { get; set; } = 1;
        public int CellsY { get; set; } = 1;
        public bool Animated { get; set; }
        public bool FlipbookLoop { get; set; } = true;
        public Vector2 Loops { get; set; } = Vector2.One;
        public bool FixedSpeed { get; set; }
        public bool CellBlend { get; set; }
        /// <summary>vs_animated_texture's row_restricted: the strip is one row of the sheet, row_restriction.</summary>
        public bool RowRestricted { get; set; }
        public int Row { get; set; }
        /// <summary>vs_animated_texture's randomize_row: each particle starts on a random row.</summary>
        public bool RandomRow { get; set; }
        /// <summary>The stack draws onto what lies under it (DEFERRED_DECAL, DEFERRED_DECAL_EMISSIVE, SEA_OVERLAY).</summary>
        public bool Decal { get; set; }
        /// <summary>The stack is DISTORTION: the chain's normal map shifts the scene behind it.</summary>
        public bool Distortion { get; set; }
        public bool Light { get; set; }
        public VfxVectorField? Field { get; set; }
        public VfxCurve? FieldSize { get; set; }
        public VfxCurve? FieldStrength { get; set; }
        public VfxCurve? FieldTightness { get; set; }
        public VfxCurve? FieldPosition { get; set; }
        public VfxCurve? FieldOrientation { get; set; }
        /// <summary>Vectorfield{Size,Position,Orientation}LoopAnim: those curves wrap over the emitter's life, as EmitterPositionLoopAnim's does.</summary>
        public bool LoopFieldSize { get; set; }
        public bool LoopFieldPosition { get; set; }
        public bool LoopFieldOrientation { get; set; }
        /// <summary>continuous_rigid: the models a particle may be, and its orientation and spin about X and Y (Angle and Spin are Z).</summary>
        public IReadOnlyList<VfxRigidEntry> Rigid { get; set; } = [];
        /// <summary>ParticleRelativeSpawnOrientation RELATIVE_TO_TERRAIN_NORMAL: the model, or an Object3D quad, stands on the ground (VfxConventions.RigidFrame).</summary>
        public bool RigidOnTerrain { get; set; }
        /// <summary>ParticleRelativeSpawnDirection RELATIVE_TO_TERRAIN_NORMAL: the spawn direction is taken on the ground, keeping the emitter's heading.</summary>
        public bool DirectionOnTerrain { get; set; }
        public (Vector2 Min, Vector2 Max) TurnXy { get; set; }
        public (Vector2 Min, Vector2 Max) SpinXy { get; set; }
        public Vector2 SpinSignXy { get; set; } = Vector2.One;
        /// <summary>ps_alpha_crush: a particle's crush level over its life, and whether it cuts sharp.</summary>
        public VfxCurve? AlphaCrush { get; set; }
        public bool CrushSharp { get; set; }
        /// <summary>ps_gradient: the colour and alpha strips a texel's value picks from, and alpha_mapping.</summary>
        public VfxCurve? Gradient { get; set; }
        public VfxCurve? GradientAlpha { get; set; }
        public bool GradientMapsAlpha { get; set; }
        public int Cell { get; set; }
        public bool RandomCell { get; set; }
        public int AtlasX { get; set; } = 1;
        public int AtlasY { get; set; } = 1;
        public int AtlasCell { get; set; }

        // The 18 fragments vanilla uses.
        static readonly HashSet<string> s_knownFragments =
        [
            "vs_vertex_alpha", "vs_colour", "ps_texture", "ps_soft_edge", "ps_lighting", "vs_translation", "vs_cell_texture",
            "vs_translation_velocity_v2", "vs_animated_texture", "vs_flip_uv", "ps_normal", "vs_rendersize_limiter", "ps_alpha_crush",
            "ps_gradient", "ps_emissive", "ps_custom_shader", "ps_metal_roughness", "vs_translation_screen_space_wrap",
        ];

        public static EmitterSettings From(ResolvedEmitter resolved)
        {
            var properties = resolved.Emitter.Properties;
            var o = resolved.Override;
            // The file is in the name, so a log line says where a broken property lives.
            var s = new EmitterSettings { Name = resolved.File + "/" + resolved.Emitter.Id };

            VfxProperty? P(string name) => properties.GetValueOrDefault(name);
            bool Flag(string name) => P(name)?.IsTrue ?? false;
            float Number(string name, float fallback) => P(name)?.Number("value", fallback) ?? fallback;
            (float, float) Range(string name, (float, float) fallback) => P(name) is { } p ? (p.Number("min"), p.Number("max")) : fallback;
            Vector2 Xy(string name) => P(name) is { } p ? new Vector2(p.Number("x"), p.Number("y")) : Vector2.Zero;
            float Z(string name) => P(name)?.Number("z") ?? 0;

            s.ReferenceTransform = resolved.ReferenceTransform;
            s.Trail = resolved.TrailMode;
            s.StartOffset = resolved.StartOffset;
            s.SpeedMultiplier = resolved.SpeedMultiplier * Number("SimulationSpeedMultiplier", 1);
            s.StartTime = Range("StartTime", (0, 0));
            s.LifeTime = Range("LifeTime", (1, 1));
            s.EmitterLifeBias = (o.EmitterLifetimeBiasMin, o.EmitterLifetimeBiasMax);
            s.InfiniteLife = resolved.InfiniteLife && Flag("InfiniteLife");
            s.Space = P("ParticleSimulationSpace")?.Value switch
            {
                "LOCAL_SPACE_POSITION_ROTATION" => SimulationSpace.LocalPositionRotation,
                "LOCAL_SPACE_POSITION_ONLY" => SimulationSpace.LocalPositionOnly,
                _ => SimulationSpace.World,
            };
            s.EmitterPosition = ParseCurve(s, P("EmitterPosition"));
            s.EmitterOrientation = ParseCurve(s, P("EmitterOrientation"));
            s.LoopPosition = Flag("EmitterPositionLoopAnim");
            s.LoopOrientation = Flag("EmitterOrientationLoopAnim");

            s.Rate = ParseCurve(s, P("EmissionRateCurve"));
            s.LoopRate = Flag("EmissionLoopAnim");
            s.RateScale = (o.SpawnRateScaleMin, o.SpawnRateScaleMax);
            if (P("BurstInterval") != null)
            {
                s.BurstInterval = Range("BurstInterval", (0, 0));
                s.BurstLength = Range("BurstIntervalLength", (0, 0));
            }
            var maximum = (int)Number("MaximumParticles", ParticleSimulator.ParticleCap);
            s.MaxParticles = maximum > 0 ? Math.Min(maximum, ParticleSimulator.ParticleCap) : ParticleSimulator.ParticleCap;
            s.SpawnBox = ParseCurve(s, P("SpawnAreaBox"));
            s.SpawnRing = ParseCurve(s, P("SpawnAreaCurve"));
            s.LoopArea = Flag("SpawnAreaLoopAnim");
            s.SpawnAngle = P("SpawnAngle") == null ? VfxConventions.EveryAzimuth : Xy("SpawnAngle");
            s.SpawnArc = Xy("SpawnArc");
            s.Spread = Range("SpawnSpread", (90, 90));
            s.SkipPositionUpdate = Flag("SkipParticlePositionUpdate");
            s.SpawnSpacing = Math.Max(0, Number("SpawnSpacing", 0));
            // CA ships no source for it; read as its name says, on 31 vanilla emitters whose particles start still.
            s.MaxSpeed = Math.Max(0, Number("MaximumVelocity", 0));

            s.InitialSpeed = ParseCurve(s, P("ParticleInitialSpeed"));
            s.InitialScale = ParseCurve(s, P("ParticleInitialScale"));
            s.InitialAlpha = ParseCurve(s, P("ParticleInitialAlpha"));
            s.ParticleLife = ParseCurve(s, P("ParticleLifeTime"));
            s.LoopSpeed = Flag("ParticleInitialSpeedLoopAnim");
            s.LoopInitial = Flag("ParticleInitialParamLoopAnim");
            s.LoopLife = Flag("ParticleLifeTimeLoopAnim");
            s.LifeBias = (o.ParticleLifetimeBiasMin, o.ParticleLifetimeBiasMax);
            s.TintMin = o.TintMin;
            s.TintMax = o.TintMax;
            s.Angle = (Z("ParticleOrientationMin"), Z("ParticleOrientationMax"));
            s.Spin = (Z("ParticleSpin_Min"), Z("ParticleSpin_Max"));
            s.SpinSign = VfxConventions.SpinSign(P("ParticleSpinDirectionZ")?.Value);
            s.TurnXy = (Xy("ParticleOrientationMin"), Xy("ParticleOrientationMax"));
            s.SpinXy = (Xy("ParticleSpin_Min"), Xy("ParticleSpin_Max"));
            s.SpinSignXy = new Vector2(VfxConventions.SpinSign(P("ParticleSpinDirectionX")?.Value), VfxConventions.SpinSign(P("ParticleSpinDirectionY")?.Value));

            s.Scale = ParseCurve(s, P("ParticleScale"));
            s.Deceleration = ParseCurve(s, P("ParticleDeceleration"));
            s.SpinDeceleration = ParseCurve(s, P("ParticleSpinDeceleration"));
            s.Forces = [ParseCurve(s, P("ForceA")), ParseCurve(s, P("ForceB")), ParseCurve(s, P("ForceC"))];

            s.RigidOnTerrain = P("ParticleRelativeSpawnOrientation")?.Value == "RELATIVE_TO_TERRAIN_NORMAL";
            // CA ships no source for it; read as its orientation sibling is, on 249 vanilla emitters.
            s.DirectionOnTerrain = P("ParticleRelativeSpawnDirection")?.Value == "RELATIVE_TO_TERRAIN_NORMAL";
            if (resolved.Emitter.Type == "continuous_rigid")
            {
                s.Rigid = resolved.Emitter.Rigid ?? [];
                if (s.Rigid.Count == 0)
                    NotDrawn(s, "its RigidData names no model, so there is nothing to draw");
            }
            else if (resolved.Emitter.Type != "continuous_mesh")
                NotDrawn(s, $"emitter type {resolved.Emitter.Type} is not drawn");
            if (resolved.SceneViewType is "DEFERRED_DECAL" or "DEFERRED_DECAL_EMISSIVE" or "SEA_OVERLAY")
                s.Decal = true;
            else if (resolved.SceneViewType == "DISTORTION")
                s.Distortion = true;
            else if (resolved.SceneViewType == "DEFERRED_DYNAMIC_LIGHT")
                (s.Decal, s.Light) = (true, true);
            else if (resolved.SceneViewType is not (null or "FULLRES_DEFERRED_ALPHA" or "HALFRES_DEFERRED_ALPHA"))
                NotDrawn(s, $"scene_view_type {resolved.SceneViewType} is not drawn");
            if (Flag("AllowEmitFromMesh"))
                s.Skipped.Add("emit from mesh is not simulated");
            if (P("VectorFieldStrId")?.Value is { Length: > 0 } field)
            {
                s.Field = resolved.VectorField;
                if (s.Field == null)
                    s.Skipped.Add($"vector field {field} is not in the loaded packs or is not a half-float volume, so it is not simulated");
                s.FieldSize = ParseCurve(s, P("VectorFieldSize"));
                s.FieldStrength = ParseCurve(s, P("VectorFieldStrength"));
                s.FieldTightness = ParseCurve(s, P("VectorFieldTightness"));
                s.FieldPosition = ParseCurve(s, P("VectorFieldPosition"));
                s.FieldOrientation = ParseCurve(s, P("VectorFieldOrientation"));
                s.LoopFieldSize = Flag("VectorfieldSizeLoopAnim");
                s.LoopFieldPosition = Flag("VectorfieldPositionLoopAnim");
                s.LoopFieldOrientation = Flag("VectorfieldOrientationLoopAnim");
            }
            if (Number("EffectedByWindFactor", 0) != 0 || Number("WindVarianceFactor", 0) != 0)
                s.Skipped.Add("wind is not simulated");
            s.InheritedSpeed = Number("SpawnInheritedSpeed", 0);
            s.InheritedDirectionBlend = Number("SpawnInheritedDirectionBlend", 0);
            s.Events = (resolved.Events ?? []).Where(x => x.Emitters.Count > 0)
                .Select(x => new ParticleEvent(x.Event.OnDeath, x.Emitters.Select(From).ToList(), x.Event.TrackPosition, x.Event.TrackScale,
                    x.Event.TrackColour, x.Event.TrackAlpha, x.Event.TrackLifetime, x.Event.Turn, x.Event.Offset))
                .ToList();
            if (resolved.RefSpawnSpeed is { } refSpawnSpeed)
                s.Skipped.Add(FormattableString.Invariant($"its file's ref_spawn_speed {refSpawnSpeed} is not read"));
            s.SpawnOnTerrain = Flag("ParticlesSpawnOnTerrain");
            s.Collisions = Flag("EnableCollisions");
            s.Bounce = Range("CollisionBouncingFactor", (0, 0));
            s.Damping = Range("CollisionDampingFactor", (0, 0));
            Vector4 Cull(string name, Vector4 fallback) => P(name) is { } p ? new Vector4(p.Number("x"), p.Number("y"), p.Number("z"), p.Number("w")) : fallback;
            s.EmitterCull = Cull("CullDistances", s.EmitterCull);
            s.ParticleCull = Cull("ParticleCullDistances", s.ParticleCull);
            if (Range("SpreadArc", (0, 0)) != (0, 0))
                s.Skipped.Add("SpreadArc is not read");

            ReadChain(s, resolved.Chain, resolved.MaterialTexture);
            // A decal paints the ground's colour, not light, so its texture is laid over by its alpha whatever the chain says:
            // wh3_dlc20_kho_crack_01, flagged premultiplied, bleeds brown into its clear texels.
            if (resolved.SceneViewType == "DEFERRED_DECAL")
                s.Premultiplied = false;
            return s;
        }

        static void ReadChain(EmitterSettings s, VfxBehaviourChain? chain, string? materialTexture)
        {
            // A rigid particle draws its model, chain or no chain.
            if (chain == null)
            {
                if (s.Rigid.Count == 0)
                    NotDrawn(s, "no behaviour chain block at this quality tier, so nothing to draw");
                return;
            }
            foreach (var modifier in chain.Modifiers.Where(x => !s_knownFragments.Contains(x.Fragment)))
                s.Skipped.Add($"fragment {modifier.Fragment} is not one of the 18 vanilla uses, so it is ignored");

            float Number(VfxModifier? m, string name, float fallback) => m?.Properties.GetValueOrDefault(name)?.Number("value", fallback) ?? fallback;
            bool Flag(VfxModifier? m, string name) => m?.Properties.GetValueOrDefault(name)?.IsTrue ?? false;

            var texture = chain.Find("ps_texture");
            // A light draws no texture, only the pool it casts.
            s.Texture = !s.Light && texture?.Textures.GetValueOrDefault("diffuse") is { Length: > 0 } path ? path : null;
            if (s.Distortion)
            {
                s.Texture = texture?.Textures.GetValueOrDefault("normal") is { Length: > 0 } normal ? normal : null;
                if (s.Texture == null)
                    NotDrawn(s, "its DISTORTION chain names no normal map, so it has nothing to shift the scene by");
            }
            else if (texture == null && chain.Find("ps_custom_shader") is { } custom)
            {
                var material = custom.Properties.GetValueOrDefault("material")?.Attributes.GetValueOrDefault("path");
                s.Texture = materialTexture;
                if (materialTexture == null)
                    NotDrawn(s, $"fragment ps_custom_shader draws through {material}, which names no colour texture in the loaded packs");
                else
                    s.Skipped.Add($"fragment ps_custom_shader: {material} is drawn with its colour texture {materialTexture}, not its own shader");
            }
            else if (texture == null && s.Draws && s.Rigid.Count == 0 && !s.Light)
                NotDrawn(s, "the chain has no ps_texture, so nothing is drawn: in vanilla only sound carriers lack one");
            s.Premultiplied = texture?.Properties.GetValueOrDefault("pre_multiplied_alpha")?.IsTrue ?? true;
            s.Overlay = texture?.Properties.GetValueOrDefault("overlay_colour")?.IsTrue ?? false;
            if (!s.Distortion && chain.Find("ps_normal") != null && texture?.Textures.GetValueOrDefault("normal") is { Length: > 0 } normalMap)
                s.NormalMap = normalMap;
            var lighting = chain.Find("ps_lighting");
            s.AdditiveMul = Number(lighting, "additive_blend_mul", 0);
            // Missing settings take the defaults fx/particle_vfx_library.hlsl gives them.
            if (lighting != null)
                s.Lighting = new ParticleLighting(Number(lighting, "small_particles", 0.1f), Number(lighting, "large_particles", 0.5f),
                    Number(lighting, "directionality", 0.4f), Number(lighting, "emissive", 0), Number(lighting, "average_size", 0.6f));
            s.MinPixels = Number(chain.Find("vs_rendersize_limiter"), "pixelsize_limit", 0);
            s.EmissiveMul = Number(chain.Find("ps_emissive"), "emissive_mul", 1);
            var crush = chain.Find("ps_alpha_crush");
            s.AlphaCrush = ParseCurve(s, crush?.Properties.GetValueOrDefault("curve_alpha_crush"));
            s.CrushSharp = Flag(crush, "sharp_transition");
            var gradient = chain.Find("ps_gradient");
            s.Gradient = ParseCurve(s, gradient?.Properties.GetValueOrDefault("curve_colour"));
            s.GradientAlpha = ParseCurve(s, gradient?.Properties.GetValueOrDefault("curve_alpha"));
            s.GradientMapsAlpha = Flag(gradient, "alpha_mapping");
            var soft = chain.Find("ps_soft_edge");
            s.SoftEdge = Number(soft, "soft_factor", 0);
            s.SoftStrength = Number(soft, "scale_strength", 1);
            var flip = chain.Find("vs_flip_uv");
            s.FlipU = Number(flip, "flip_u", 0);
            s.FlipV = Number(flip, "flip_v", 0);
            s.UvTurn = flip?.Properties.GetValueOrDefault("uv_orientation")?.Value switch { "90_degrees_ccw" => 1, "90_degrees_cw" => -1, _ => 0 };
            var vertexAlpha = chain.Find("vs_vertex_alpha");
            s.Alpha = ParseCurve(s, vertexAlpha?.Properties.GetValueOrDefault("curve_alpha"));
            s.AlphaStartOffset = vertexAlpha?.Properties.GetValueOrDefault("alpha_START_offset")?.Float2(Vector2.Zero) ?? Vector2.Zero;
            s.AlphaEndOffset = vertexAlpha?.Properties.GetValueOrDefault("alpha_END_offset")?.Float2(Vector2.Zero) ?? Vector2.Zero;
            s.Colour = ParseCurve(s, chain.Find("vs_colour")?.Properties.GetValueOrDefault("curve_colour"));

            var velocity = chain.Find("vs_translation_velocity_v2");
            var translation = velocity ?? chain.Find("vs_translation");
            s.Pivot = new Vector2(Number(translation, "pivot_x", 0.5f), Number(translation, "pivot_y", 0.5f));
            s.Stretch = ParseCurve(s, velocity?.Properties.GetValueOrDefault("velocity_stretch_mul"));
            s.StretchCap = Number(velocity, "velocity_stretch_cap", 0);
            var mode = velocity?.Properties.GetValueOrDefault("orientation_mode")?.Value;
            (s.Orientation, var known) = mode switch
            {
                null => (QuadOrientation.CameraFacing, true),
                "Camera-aligned with stretching factor" => (QuadOrientation.VelocityAligned, true),
                "Camera-facing velocity-aligned" => (QuadOrientation.VelocityAxis, true),
                "Horizontally-oriented" => (QuadOrientation.Horizontal, true),
                "Vertically-oriented" => (QuadOrientation.Vertical, true),
                "Particle only rotating around local Y (horizontal)" => (QuadOrientation.LocalHorizontal, true),
                "Particle only rotating around local Y (vertical)" => (QuadOrientation.LocalVertical, true),
                _ => (QuadOrientation.CameraFacing, false),
            };
            if (!known)
                s.Skipped.Add($"orientation_mode \"{mode}\" is drawn camera-facing");
            // Without a velocity modifier the quad turns by vs_translation's restriction: CA's particle_vfx_library.hlsl names
            // 0 to 3 "CameraFacing, Vertical, Horizontal, 3D".
            if (velocity == null)
                s.Orientation = (int)Number(translation, "restriction", 0) switch
                {
                    1 => QuadOrientation.Upright,
                    2 => QuadOrientation.Flat,
                    3 => QuadOrientation.Object3D,
                    _ => QuadOrientation.CameraFacing,
                };

            var animated = chain.Find("vs_animated_texture");
            var cells = chain.Find("vs_cell_texture");
            var atlas = animated ?? cells;
            s.AtlasX = Math.Max(1, (int)Number(atlas, "atlas_number_of_cells_x", 1));
            s.AtlasY = Math.Max(1, (int)Number(atlas, "atlas_number_of_cells_y", 1));
            s.AtlasCell = (int)Number(atlas, "atlas_cell", 0);
            if (animated != null)
            {
                var count = animated.Properties.GetValueOrDefault("no_cells_xy")?.Float2(Vector2.One) ?? Vector2.One;
                s.CellsX = Math.Max(1, (int)count.X);
                s.CellsY = Math.Max(1, (int)count.Y);
                s.Animated = true;
                s.FlipbookLoop = Flag(animated, "loop");
                s.Loops = animated.Properties.GetValueOrDefault("no_loops")?.Float2(Vector2.One) ?? Vector2.One;
                s.FixedSpeed = animated.Properties.GetValueOrDefault("time_mode")?.Value == "fixed_animation_speed";
                s.CellBlend = Flag(animated, "cell_blend");
                s.RowRestricted = Flag(animated, "row_restricted");
                s.Row = (int)Number(animated, "row_restriction", 0);
                s.RandomRow = Flag(animated, "randomize_row");
                s.RandomCell = Flag(animated, "randomize_cell");
            }
            else if (cells != null)
            {
                s.CellsX = Math.Max(1, (int)Number(cells, "number_of_cells_x", 1));
                s.CellsY = Math.Max(1, (int)Number(cells, "number_of_cells_y", 1));
                s.Cell = (int)Number(cells, "cell", 0);
                s.RandomCell = Flag(cells, "randomize_cell");
            }
        }

        static VfxCurve? ParseCurve(EmitterSettings s, VfxProperty? property)
        {
            if (property == null)
                return null;
            var curve = VfxCurve.Parse(property.Text);
            if (curve == null)
                s.Skipped.Add($"{property.Name} did not parse, so its default is used");
            return curve;
        }

        static void NotDrawn(EmitterSettings s, string why)
        {
            s.Draws = false;
            s.Skipped.Add(why);
        }
    }
}
