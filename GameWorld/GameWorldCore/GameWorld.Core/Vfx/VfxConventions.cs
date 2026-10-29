using System.Text.RegularExpressions;
using Microsoft.Xna.Framework;

namespace GameWorld.Core.Vfx
{
    /// <summary>
    /// The readings of the vfx format that are guesses rather than measurements, so calibrating against
    /// Terry (CA's battle map editor) changes them here. This class holds some, and the
    /// rest, which live where they are used: the crush, gradient and light-pool readings in
    /// Shaders/Particle.fx, the vector field in ParticleSimulator.Field, the rigid draw in
    /// ParticleRenderItem.DrawRigid, the persistent clock in SuperView's VfxEffectInstance.EffectTime, and
    /// two in ParticleSimulator's structure: forces are accelerations in the particle's own space (the
    /// emitter's axes for LOCAL_SPACE_POSITION_ROTATION, world axes otherwise), and a quad's size is
    /// ParticleInitialScale x ParticleScale as its full width and height.
    /// </summary>
    public static class VfxConventions
    {
        /// <summary>ForceA/B/C are cm/s2 while speeds and sizes are metres: falling debris carries -981 (156 vanilla
        /// emitters), rocks -1471.5, sand -490.5 and -1962, against 101 uses of 9.81; debris launched at 5-8 m/s under
        /// -981 arcs and lands within its 1.5 s life. Measured 2026-10-11.</summary>
        public const float ForceToMetres = 0.01f;

        /// <summary>The longest pre-warm simulated, in seconds. Vanilla's StartTime goes down to -20.</summary>
        public const float MaxPreWarm = 30;

        /// <summary>A reference's transform: scale, then its rotation, then its position.</summary>
        public static Matrix ReferenceMatrix(float scale, Vector3 rotationDegrees, Vector3 position) =>
            Matrix.CreateScale(scale) * Rotation(rotationDegrees) * Matrix.CreateTranslation(position);

        /// <summary>Rotations (references, EmitterOrientation): about X, then Y, then Z, in degrees.</summary>
        public static Matrix Rotation(Vector3 degrees) =>
            Matrix.CreateRotationX(MathHelper.ToRadians(degrees.X)) * Matrix.CreateRotationY(MathHelper.ToRadians(degrees.Y)) * Matrix.CreateRotationZ(MathHelper.ToRadians(degrees.Z));

        /// <summary>Spawn direction: SpawnAngle is the azimuth about Y from +X, SpawnSpread the elevation above the XZ plane (90 is straight up).</summary>
        public static Vector3 Direction(float azimuthDegrees, float elevationDegrees)
        {
            var azimuth = MathHelper.ToRadians(azimuthDegrees);
            var elevation = MathHelper.ToRadians(elevationDegrees);
            return new Vector3(MathF.Cos(elevation) * MathF.Cos(azimuth), MathF.Sin(elevation), MathF.Cos(elevation) * MathF.Sin(azimuth));
        }

        /// <summary>An absent SpawnAngle is every azimuth. 8,908 of vanilla's 11,722 continuous_mesh emitters omit it, and
        /// where present it is an azimuth range (0..360 in 802, -180..180 in 366); measured 2026-10-11.</summary>
        public static readonly Vector2 EveryAzimuth = new(0, 360);

        /// <summary>SpawnArc is the spawn ring's start and end angle in degrees, in the XZ plane; 0,0 is the whole ring.</summary>
        public static (float From, float To) RingArc(Vector2 spawnArc) => spawnArc == Vector2.Zero ? (0, 360) : (spawnArc.X, spawnArc.Y);

        /// <summary>SpawnInheritedSpeed is a plain share of the emitter's velocity (332 of its 1,385 vanilla emitters carry 0.5,
        /// nearly all lie in 0..1), given to world-space particles only: a local-space particle already rides with its emitter.
        /// </summary>
        public static Vector3 Inherited(Vector3 emitterVelocity, float share) => emitterVelocity * share;

        /// <summary>SpawnInheritedDirectionBlend (146 vanilla emitters, 0.5 or 1 on 100 of them): a particle's spawn direction
        /// turned that share of the way toward the way its emitter is moving, keeping its length. Read as a blend, so held to
        /// 0..1 (three emitters say 200, one -0.1); a still emitter changes nothing. Not measured in game.</summary>
        public static Vector3 InheritDirection(Vector3 direction, Vector3 motion, float blend)
        {
            if (blend <= 0 || motion.LengthSquared() < 1e-12f)
                return direction;
            var turned = Vector3.Lerp(Vector3.Normalize(direction), Vector3.Normalize(motion), Math.Min(blend, 1));
            return turned.LengthSquared() < 1e-12f ? direction : Vector3.Normalize(turned) * direction.Length();
        }

        /// <summary>The turn that takes +Y, the way a vfx emitter fires (SpawnSpread 90 is straight up), to a direction: how
        /// orientation_by_velocity (442 vanilla particle events) is read. Not measured in game.</summary>
        public static Matrix UpTo(Vector3 direction)
        {
            var axis = Vector3.Cross(Vector3.Up, direction);
            var cos = Math.Clamp(Vector3.Dot(Vector3.Up, direction), -1, 1);
            if (axis.LengthSquared() < 1e-12f)
                return cos > 0 ? Matrix.Identity : Matrix.CreateRotationX(MathF.PI);
            return Matrix.CreateFromAxisAngle(Vector3.Normalize(axis), MathF.Acos(cos));
        }

        /// <summary>The preview's terrain is flat ground at y = 0 in the effect's space. ParticlesSpawnOnTerrain (1,503 vanilla
        /// emitters) drops the spawn point onto it. ParticlesFollowTerrain (1,073) is read as holding a particle's height above
        /// the ground as it moves, which on flat ground changes nothing; pinning it to the ground is the other reading.</summary>
        public static Vector3 OnGround(Vector3 position) => new(position.X, 0, position.Z);

        /// <summary>A particle falling into the ground leaves it with its fall scaled by CollisionBouncingFactor (0..1.5 in
        /// vanilla) and its slide cut by CollisionDampingFactor (0..2.2), read as the share lost and clamped to 0..1.</summary>
        public static Vector3 Bounce(Vector3 velocity, float bounce, float damping)
        {
            var keep = 1 - Math.Clamp(damping, 0, 1);
            return new Vector3(velocity.X * keep, -velocity.Y * bounce, velocity.Z * keep);
        }

        /// <summary>vs_flip_uv: flip_u and flip_v are each a particle's chance of a mirrored texture (0.5 in 4,217 and 3,413 of
        /// 4,736 vanilla chains; above 1 is always), and uv_orientation a quarter turn of it (28 chains), the turn first. Returns
        /// the texture coordinate, 0..1 with v down, at a quad corner given x right and y up, 0..1.</summary>
        public static (float U, float V) QuadUv(float x, float y, bool flipU, bool flipV, int turn)
        {
            var (u, v) = turn switch { 1 => (y, x), -1 => (1 - y, 1 - x), _ => (x, 1 - y) };
            return (flipU ? 1 - u : u, flipV ? 1 - v : v);
        }

        /// <summary>CullDistances and ParticleCullDistances, metres from the camera: alpha fades in from x to y and out from z to
        /// w, linearly; a band of no width is a hard edge. ParticleCullDistances sets a near band on 8,882 of 12,408 vanilla
        /// emitters, so a close camera sees particles thin out around it.</summary>
        public static float CullFade(float distance, Vector4 cull)
        {
            var near = cull.Y > cull.X ? Math.Clamp((distance - cull.X) / (cull.Y - cull.X), 0, 1) : distance >= cull.X ? 1 : 0;
            var far = cull.W > cull.Z ? Math.Clamp((cull.W - distance) / (cull.W - cull.Z), 0, 1) : distance <= cull.W ? 1 : 0;
            return near * far;
        }

        /// <summary>vs_rendersize_limiter (985 vanilla chains, nearly all small embers, sparks and droplets): pixelsize_limit is
        /// read as the fewest pixels a particle is drawn across, so a far spark stays visible, rather than a size below which it
        /// is culled. Returns what its size is multiplied by; pixelScale is the pixels a metre covers a metre from the camera.</summary>
        public static float MinPixelGrowth(float size, float distance, float pixelScale, float minPixels)
        {
            if (minPixels <= 0 || pixelScale <= 0 || size <= 0)
                return 1;
            return Math.Max(1, minPixels / (size / Math.Max(distance, 1e-3f) * pixelScale));
        }

        /// <summary>A particle's colour is HDR. curve_colour's fourth output, brightness, and ps_emissive's emissive_mul (556 vanilla
        /// chains, 1 to 21 and past) are read as plain multipliers on the unlit colour; a negative brightness is 0. Brightness keys
        /// are 1 in 10,338 places, 0 in 597 (286 of 13,353 curves fade to dark), and run past 50 to 5,000. How far the game's
        /// exposure brings those values down is not measured: the preview draws them saturated and blooms them as
        /// <see cref="BloomThreshold"/> says.</summary>
        public static float Hdr(float brightness, float emissiveMul) => Math.Max(0, brightness) * emissiveMul;

        /// <summary>CA's bloom bright pass, fx_hdr_tone_mapper's ps_high_pass_luminance_cutoff: a pixel feeds the bloom by its
        /// colour times saturate(luminance - g_bloom_threshold), times g_bloom_particle_strength where particles are. The glow pass
        /// weighs what each particle draws so: 4 is bloom_threshold in 83 of the 126 lighting blocks of vanilla's
        /// weather/battle environments (4.5 in 34, 1 to 4.7 in the rest), 0.5 bloom_particle_strength in all 126. CA weighs the
        /// whole scene, so particles over 4 only where they overlap glow there and not here.</summary>
        public const float BloomThreshold = 4f;
        public const float BloomParticleStrength = 0.5f;

        /// <summary>ps_soft_edge (6,893 vanilla chains), as CA's ps_main reads it: a particle fades in, linearly, over
        /// <see cref="SoftDistance"/> in front of what lies behind it. Here against the preview's ground, world y = 0: the gap is
        /// how far the ray from the camera through a quad corner runs on before it reaches the ground, and a corner under the ground
        /// is hidden, as terrain hides it. Against the solid scene, Shaders/Particle fades each pixel by its view depth in front of
        /// the depth the engine draws first. size: the particle's smaller side, metres.</summary>
        public static float SoftEdge(Vector3 corner, Vector3 camera, float softFactor, float strength, float size)
        {
            if (softFactor <= 0)
                return 1;
            if (corner.Y < 0)
                return 0;
            var ray = corner - camera;
            var down = ray.LengthSquared() > 1e-12f ? -Vector3.Normalize(ray).Y : 0;
            return down <= 0 ? 1 : Math.Clamp(corner.Y / down / SoftDistance(softFactor, strength, size), 0, 1);
        }

        /// <summary>How far ps_soft_edge fades over, metres: soft_factor (1.5 in 1,684 vanilla chains, 1 in 1,489, 0.2 to 5), shrunk
        /// for a particle under a metre across by scale_strength (1 in 6,610), soft_factor x lerp(1, its smaller side, strength),
        /// and a millimetre more, as CA's ps_main has it.</summary>
        public static float SoftDistance(float softFactor, float strength, float size) =>
            softFactor * MathHelper.Lerp(1, Math.Clamp(size, 0, 1), strength) + 0.001f;

        /// <summary>DEFERRED_DECAL (568 vanilla emitters), DEFERRED_DECAL_EMISSIVE (399) and SEA_OVERLAY (11) project onto what lies
        /// under them, and none carries an orientation_mode. The preview lays each quad flat on its ground, world y = 0, under the
        /// particle and a centimetre clear of the grid, turned by the particle's angle.</summary>
        public static Vector3 Decal(Vector3 worldPosition) => new(worldPosition.X, 0.01f, worldPosition.Z);

        /// <summary>DISTORTION (685 vanilla emitters) draws no colour: its ps_texture names only a normal map, and its alpha curve
        /// fades it. CA's ps_distortion_alphablend writes the map's x (alpha) and y (green) into a buffer, and fx_hdr_to_screen moves
        /// the finished scene by this share of the view per unit of it, in screen space, at any distance. One particle of alpha a
        /// and tilt t moves it DistortionShare x (a t)^2 x t: the particle writes a t as the buffer's cover, and the screen pass
        /// weighs the shift by the cover. That weighing is read off both of CA's blend modes agreeing, not off the blend state.</summary>
        public const float DistortionShare = 0.02f;

        /// <summary>continuous_rigid (2,925 vanilla emitters): each particle is a model, a wsmodel in 2,745 of 2,936 RigidData
        /// entries. PivotPoint is read as a share of the model's box (0.5, 0.5, 0.5 in 2,701 entries: its middle) put on the
        /// particle; the particle's width scales it evenly, and it turns about X, then Y, then Z (radians), as references do,
        /// then by its emitter's frame (RigidFrame).</summary>
        public static Matrix RigidMatrix(BoundingBox box, Vector3 pivot, float scale, Vector3 turn, Matrix frame, Vector3 position) =>
            Matrix.CreateTranslation(-(box.Min + pivot * (box.Max - box.Min))) * Matrix.CreateScale(scale)
            * Matrix.CreateRotationX(turn.X) * Matrix.CreateRotationY(turn.Y) * Matrix.CreateRotationZ(turn.Z) * frame * Matrix.CreateTranslation(position);

        static readonly Regex s_standardShader = new(@"^shaders/(system/)?(weighted\d*|rigid)_(character|default|standard)(_\d+)?(_alpha|_emissive)?\.xml\.shader$",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        /// <summary>A material's shader that the preview's PBR materials do not stand in for: any but CA's character, default and
        /// standard ones. Most of vanilla's rigid VFX models name one (the dissolves, panners and unlit vortices, Tidecall's water),
        /// and the preview cannot run it.</summary>
        public static bool IsCustomShader(string shader) => shader.Length > 0 && !s_standardShader.IsMatch(shader.Replace('\\', '/'));

        /// <summary>How far through its AnimFile a rigid particle is: played at AnimSpeed from its birth, looping, since most of
        /// vanilla's 118 animated entries are run cycles, idles and tentacles. Read as a playback rate; not measured in game.</summary>
        public static float AnimShare(float age, float speed, float length)
        {
            if (length <= 0)
                return 0;
            var u = age * speed / length;
            return u - MathF.Floor(u);
        }

        /// <summary>The turn a rigid particle takes from its emitter's matrix, without its place or scale: ParticleRelativeSpawnOrientation
        /// is RELATIVE_TO_VFX_UP_AXIS or unset on 2,853 of vanilla's 2,924 rigid emitters. RELATIVE_TO_TERRAIN_NORMAL (71) stands the
        /// model on the ground, the preview's flat one, so it keeps only the emitter's heading.</summary>
        public static Matrix RigidFrame(Matrix frame, bool onTerrain)
        {
            var forward = onTerrain ? new Vector3(frame.Forward.X, 0, frame.Forward.Z) : frame.Forward;
            if (forward.LengthSquared() < 1e-12f)
                return Matrix.Identity;
            return Matrix.CreateWorld(Vector3.Zero, Vector3.Normalize(forward), onTerrain ? Vector3.Up : Vector3.Normalize(frame.Up));
        }

        /// <summary>SpawnAreaBox is the box's full size, centred on the emitter.</summary>
        public static Vector3 BoxOffset(Vector3 size, Vector3 random01) => (random01 - new Vector3(0.5f)) * size;

        /// <summary>A curve over the emitter's life: age over LifeTime, wrapped when its LoopAnim flag is set, held at the end otherwise.</summary>
        public static float EmitterTime(float age, float lifeTime, bool loop)
        {
            if (lifeTime <= 0)
                return 0;
            var u = age / lifeTime;
            return loop ? u - MathF.Floor(u) : Math.Min(u, 1);
        }

        /// <summary>Bursts: every BurstInterval seconds, the emitter spawns for the first BurstIntervalLength of them.</summary>
        public static bool InBurst(float age, float interval, float length) => interval <= 0 || age % interval < length;

        /// <summary>ParticleDeceleration and ParticleSpinDeceleration as drag: the speed loses that fraction per second, exponentially.</summary>
        public static float Drag(float deceleration, float dt) => MathF.Exp(-Math.Max(0, deceleration) * dt);

        /// <summary>A reference's lifetime biases add seconds.</summary>
        public static float Biased(float value, float bias) => value + bias;

        /// <summary>ParticleSpinDirectionZ: CW turns the quad clockwise as the camera sees it.</summary>
        public static float SpinSign(string? direction) => direction == "CW" ? -1 : 1;

        /// <summary>additive_blend_mul: 0 blends by alpha, 1 adds, between mixes the two; above 1 it also brightens.</summary>
        public static (float AlphaWeight, float Brightness) Blend(float additiveMul) => (1 - Math.Clamp(additiveMul, 0, 1), Math.Max(1, additiveMul));

        /// <summary>A DEFERRED_DYNAMIC_LIGHT particle's share of the models' sun per unit of colour times alpha: CA's
        /// ps_main_deferred_dynamic_light lights with colour x alpha x 2500 where the models' sun is get_sun_colour() x
        /// unchartedSunFactor, 20000 x 3, both then taking sin^2(0.5 deg). Read off the shader, not assumed. Times the battle's
        /// dynamic_light_scale, which no shader reads, so the game puts it on the lights themselves: 0.1 in 93 of the 126 lighting
        /// blocks of vanilla's weather/battle environments, between 0.03 and 0.3 in the rest.</summary>
        public const float LightShare = 2500f / 60000f * 0.1f;

        /// <summary>The ground a light's pool lies on, which the preview does not draw: a mid grey of albedo 0.18. CA's light lights
        /// the terrain's own albedo out of its gbuffer.</summary>
        public const float GroundAlbedo = 0.18f;

        /// <summary>velocity_stretch_mul lengthens a velocity-aligned quad by speed x multiplier, up to velocity_stretch_cap times.</summary>
        public static float Stretch(float speed, float multiplier, float cap) => Math.Clamp(1 + speed * multiplier, 1, cap > 0 ? cap : float.MaxValue);

        /// <summary>vs_animated_texture: no_loops is how many times the strip plays over a particle's life
        /// (using_particle_lifetime) or per second (fixed_animation_speed), running from its start to its end over the life, as
        /// CA's lerps it by the life fraction; without loop it holds the last cell. cell_blend (4,408
        /// of 4,935 vanilla flipbooks) mixes each cell into the next by how far the particle is between them; the next after the
        /// last is the first when the strip loops, the last again when it holds.</summary>
        /// <summary>vs_animated_texture's rows, as CA's has them: row_restricted plays the cells of row row_restriction;
        /// randomize_row starts on the row the particle's random fraction picks and, without row_restricted, plays the whole sheet
        /// from there. CA's cell then runs off the sheet; it is taken round to the first, as a repeating texture would.
        /// randomize_cell starts the particle that far along its strip, by the same random fraction.</summary>
        /// <summary>vs_vertex_alpha's alpha_START_offset and alpha_END_offset, as CA's map_range has them: curve_alpha is read at the life
        /// fraction squeezed into the window from the start offset to 1 less the end offset, each lerped by the particle's alpha random,
        /// and holds its ends outside it.</summary>
        public static float AlphaLife(float life, Vector2 start, Vector2 end, float random)
        {
            var (from, to) = (MathHelper.Lerp(start.X, start.Y, random), 1 - MathHelper.Lerp(end.X, end.Y, random));
            return Math.Clamp((life - from) / Math.Max(to - from, 1e-6f), 0, 1);
        }

        public static (int Cell, int Next, float Blend) FlipbookFrame(EmitterSettings s, float lifeFraction, float ageSeconds, float loops, float random)
        {
            var cells = s.CellsX * s.CellsY;
            var (cell, next, blend) = FlipbookFrame(s.RowRestricted ? s.CellsX : cells, lifeFraction, ageSeconds, loops, s.FixedSpeed, s.FlipbookLoop, s.RandomCell ? random : 0);
            var start = (s.RandomRow ? Math.Min((int)(random * s.CellsY), s.CellsY - 1) : s.RowRestricted ? s.Row : 0) * s.CellsX;
            return ((start + cell) % cells, (start + next) % cells, blend);
        }

        public static (int Cell, int Next, float Blend) FlipbookFrame(int cells, float lifeFraction, float ageSeconds, float loops, bool fixedSpeed, bool loop, float offset = 0)
        {
            var f = (fixedSpeed ? ageSeconds : lifeFraction) * loops + offset;
            f = loop ? f - MathF.Floor(f) : Math.Clamp(f, 0, 0.9999f);
            var position = f * cells;
            var cell = Math.Min((int)position, cells - 1);
            return (cell, loop ? (cell + 1) % cells : Math.Min(cell + 1, cells - 1), Math.Clamp(position - cell, 0, 1));
        }
    }
}
