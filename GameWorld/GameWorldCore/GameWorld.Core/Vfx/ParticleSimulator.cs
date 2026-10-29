using Microsoft.Xna.Framework;
using Shared.GameFormats.Vfx;

namespace GameWorld.Core.Vfx
{
    /// <summary>A live particle, in its emitter's simulation space (see Spawn and States).</summary>
    public struct Particle
    {
        public Vector3 Position;
        public Vector3 Velocity;
        public float Age;
        public float Life;
        public float Angle;
        public float Spin;
        public Vector2 Size;
        public float Alpha;
        public Vector4 Tint;
        // vs_cell_texture's random cell and vs_animated_texture's random row, from one draw, as CA's take one random_unary.
        public float RCell;
        // One draw per over-life property, taken at spawn.
        public float RScale, RDeceleration, RSpinDeceleration, RForce, RAlpha, RColour, RStretch;
        // Drawn only when the emitter collides, so an emitter that does not keeps its random sequence.
        public float Bounce, Damping;
        public bool FlipU, FlipV;
        // Drawn only for a rigid emitter: its model, and its turn and spin about X and Y.
        public int Model;
        public Vector2 TurnXy, SpinXy;
        // Its number in its emitter, in spawn order: what a particle event's effect follows it by.
        public int Id;
    }

    /// <summary>A particle as drawn now, in the effect's model space.</summary>
    public readonly record struct ParticleState(Vector3 Position, Vector3 Velocity, Vector2 Size, float Angle, Vector3 Colour, float Alpha, float Stretch, int Cell, bool FlipU = false, bool FlipV = false, int NextCell = 0, float CellBlend = 0, float Crush = 0, Vector2 TurnXy = default, int Model = 0, float Age = 0);

    public class EmitterRuntime
    {
        public EmitterRuntime(EmitterSettings settings, Random random)
        {
            Settings = settings;
            Random = random;
        }

        public EmitterSettings Settings { get; }
        public List<Particle> Particles { get; } = [];
        /// <summary>The emitter's transform at the last simulated time.</summary>
        public Matrix World { get; internal set; } = Matrix.Identity;

        internal Random Random { get; }
        internal float Start, Life, RateScale, BurstInterval, BurstLength, RRate, RTransform, Accumulator;
        internal bool CapLogged;
        // SpawnSpacing: where the emitter was at the last step, and the metres moved since the last particle.
        internal Vector3 LastPosition;
        internal float Travelled;

        /// <summary>A particle event's emitter: its particle's colour and alpha (track_colour, track_alpha). White otherwise.</summary>
        public Vector4 Tint { get; internal set; } = Vector4.One;
        // Particle events: the ids given out and the first not yet started, the particles that died this step, and the event
        // effects running.
        internal int NextId, Started;
        internal readonly List<Particle> Dead = [];
        internal readonly List<ChildEffect> Children = [];
        internal bool ChildCapLogged;
    }

    /// <summary>One particle's copy of a particle event's effect: a simulation of its own, placed at Frame in the parent's space.</summary>
    sealed class ChildEffect(ParticleEvent particleEvent, int parent, float born, Matrix frame)
    {
        public ParticleEvent Event { get; } = particleEvent;
        /// <summary>The particle's id; -1 once it has died, and for a death event.</summary>
        public int Parent { get; set; } = parent;
        /// <summary>The parent's time at which the child's clock reads 0.</summary>
        public float Born { get; } = born;
        public Matrix Frame { get; set; } = frame;
        public ParticleSimulator Simulator { get; set; } = null!;
    }

    /// <summary>
    /// One effect's particles. Deterministic: fixed 1/60 s steps, and a seed per emitter. Its clock is the
    /// effect's own (main animation time minus the tag's Start Time). Going back re-simulates from the
    /// start, so a scrub lands where a straight run would.
    /// </summary>
    public class ParticleSimulator
    {
        public const float Step = 1f / 60;
        public const int ParticleCap = 2000;
        const float Epsilon = 1e-5f;

        private readonly List<EmitterSettings> _settings;
        private readonly int _seed;
        private readonly Func<float, Matrix> _effectWorldAt;
        private float _spawnEnd;
        private readonly Action<string>? _log;
        private readonly List<EmitterRuntime> _emitters = [];
        private float _origin;
        private int _steps;

        public IReadOnlyList<EmitterRuntime> Emitters => _emitters;

        /// <summary>A particle event's effects running on one emitter at most; one per particle is a whole simulation.</summary>
        public const int ChildCap = 64;

        /// <summary>The emitters and, after them, every particle event effect's emitters, theirs included.</summary>
        public IEnumerable<EmitterRuntime> AllEmitters => _emitters.Concat(_emitters.SelectMany(e => e.Children.SelectMany(c => c.Simulator.AllEmitters)));

        /// <summary>Nothing alive and nothing more to come: every emitter past its life or its spawn end, with no events running.</summary>
        internal bool Spent => _emitters.All(e => e.Particles.Count == 0 && e.Children.Count == 0
            && (Time > _spawnEnd || !e.Settings.InfiniteLife && (Time - e.Start) * e.Settings.SpeedMultiplier > e.Life));

        /// <summary>Stops spawning at an effect time (track_lifetime: the particle a child effect follows has died).</summary>
        internal void EndSpawning(float time) => _spawnEnd = Math.Min(_spawnEnd, time);

        /// <summary>Effect time simulated so far; below 0 while an emitter pre-warms.</summary>
        public float Time => _origin + _steps * Step;

        /// <param name="effectWorldAt">the effect's frame at an effect time: the bone, then the tag's offset</param>
        /// <param name="spawnEnd">effect time at which spawning stops; live particles finish their lives</param>
        public ParticleSimulator(IEnumerable<EmitterSettings> settings, int seed, Func<float, Matrix> effectWorldAt, float spawnEnd = float.PositiveInfinity, Action<string>? log = null)
        {
            _settings = settings.Where(x => x.Draws).ToList();
            _seed = seed;
            _effectWorldAt = effectWorldAt;
            _spawnEnd = spawnEnd;
            _log = log;
            Reset();
        }

        /// <summary>Simulates up to an effect time. Before 0 the effect has not started, so nothing is alive.</summary>
        public void AdvanceTo(float effectTime)
        {
            if (effectTime < 0)
            {
                if (_steps != 0)
                    Reset();
                return;
            }

            // Whole steps, counted in double: minutes in, a float time cannot hold Epsilon, and a paused clock asked for the
            // same time again started over from nothing every frame.
            var steps = (int)Math.Floor(((double)effectTime + Epsilon - _origin) / Step);
            if (steps < _steps)
                Reset();
            while (_steps < steps)
                StepOnce();

            var effectWorld = _effectWorldAt(Time);
            foreach (var e in _emitters)
                e.World = EmitterWorld(e, Time, effectWorld);
        }

        void Reset()
        {
            _emitters.Clear();
            for (var i = 0; i < _settings.Count; i++)
            {
                var s = _settings[i];
                var random = new Random(_seed + i * 7919);
                float Draw((float Min, float Max) range) => MathHelper.Lerp(range.Min, range.Max, (float)random.NextDouble());
                var e = new EmitterRuntime(s, random)
                {
                    Start = Draw(s.StartTime) + s.StartOffset,
                    Life = Draw(s.LifeTime) + Draw(s.EmitterLifeBias),
                    RateScale = Draw(s.RateScale),
                    RRate = (float)random.NextDouble(),
                    RTransform = (float)random.NextDouble(),
                };
                if (s.BurstInterval is { } burst)
                {
                    e.BurstInterval = Draw(burst);
                    e.BurstLength = Draw(s.BurstLength);
                }
                _emitters.Add(e);
            }

            // A negative Start Time pre-warms: that emitter was already running when the effect began.
            _origin = Math.Max(-VfxConventions.MaxPreWarm, _emitters.Select(x => x.Start).Append(0).Min());
            _steps = 0;
            var effectWorld = _effectWorldAt(_origin);
            foreach (var e in _emitters)
                e.LastPosition = EmitterWorld(e, _origin, effectWorld).Translation;
        }

        void StepOnce()
        {
            var end = Time + Step;
            var effectWorld = _effectWorldAt(end);
            foreach (var e in _emitters)
            {
                var s = e.Settings;
                var dt = Step * s.SpeedMultiplier;
                var world = EmitterWorld(e, end, effectWorld);
                var age = (end - e.Start) * s.SpeedMultiplier;
                Move(e, dt, world, age);

                var alive = age > 0 && (s.InfiniteLife || age <= e.Life);
                var from = e.LastPosition;
                e.LastPosition = world.Translation;
                if (!alive || end > _spawnEnd + Epsilon || !VfxConventions.InBurst(age, e.BurstInterval, e.BurstLength))
                    continue;

                var limit = Math.Min(s.MaxParticles, ParticleCap);
                var moving = dt > 0 ? (world.Translation - from) / dt : Vector3.Zero;
                var carried = s.Space == SimulationSpace.World ? VfxConventions.Inherited(moving, s.InheritedSpeed) : Vector3.Zero;
                if (s.SpawnSpacing > 0)
                    SpawnAlong(e, age, world, from, dt, limit, carried, moving);

                var rate = s.Rate?.Evaluate(0, VfxConventions.EmitterTime(age, e.Life, s.LoopRate), e.RRate) ?? 0;
                e.Accumulator += Math.Max(0, rate) * e.RateScale * dt;
                if (e.Accumulator < 1)
                    continue;

                // The step's whole particles are taken in one go: "-= 1" in a loop stops changing a float past
                // 2^25, which a modder's rate reaches, and the loop never ended. At most `limit` tries a step.
                var due = MathF.Floor(e.Accumulator);
                e.Accumulator -= due;
                var tries = (int)Math.Min(due, limit);
                var tried = 0;
                for (; tried < tries && e.Particles.Count < limit; tried++)
                    Spawn(e, age, world, carried, moving);
                LogCap(e, due, tried, limit);
            }
            foreach (var e in _emitters.Where(x => x.Settings.Events.Count > 0))
                StepEvents(e, end, EmitterWorld(e, end, effectWorld));
            _steps++;
        }

        // Particle events: an effect started for each particle born or died this step, then each running one moved to its particle
        // and run to now. One whose particle has gone and that has nothing left to draw is dropped.
        void StepEvents(EmitterRuntime e, float end, Matrix world)
        {
            var events = e.Settings.Events;
            foreach (var p in e.Particles.Where(x => x.Id >= e.Started))
                foreach (var spawn in events.Where(x => !x.OnDeath))
                    Start(e, spawn, p.Id, end - p.Age, State(e, p, world));
            e.Started = e.NextId;
            foreach (var p in e.Dead)
            {
                foreach (var child in e.Children.Where(x => x.Parent == p.Id))
                {
                    child.Parent = -1;
                    if (child.Event.TrackLifetime)
                        child.Simulator.EndSpawning(end - child.Born);
                }
                foreach (var death in events.Where(x => x.OnDeath))
                    Start(e, death, -1, end, State(e, p, world));
            }
            e.Dead.Clear();

            foreach (var child in e.Children)
            {
                var ev = child.Event;
                if (child.Parent >= 0 && (ev.TrackPosition || ev.TrackColour || ev.TrackAlpha) && Find(e.Particles, child.Parent) is { } p)
                {
                    var state = State(e, p, world);
                    if (ev.TrackPosition)
                        child.Frame = Frame(ev, state);
                    Tint(child, state);
                }
                child.Simulator.AdvanceTo(end - child.Born);
            }
            e.Children.RemoveAll(x => x.Parent < 0 && x.Simulator.Spent);
        }

        void Start(EmitterRuntime e, ParticleEvent particleEvent, int parent, float at, ParticleState p)
        {
            if (e.Children.Count >= ChildCap)
            {
                if (!e.ChildCapLogged)
                    _log?.Invoke($"emitter {e.Settings.Name}: {ChildCap} particle event effects running, the rest are not started");
                e.ChildCapLogged = true;
                return;
            }
            var child = new ChildEffect(particleEvent, parent, at + particleEvent.Offset, Frame(particleEvent, p));
            child.Simulator = new ParticleSimulator(particleEvent.Emitters, e.Random.Next(), _ => child.Frame, log: _log);
            Tint(child, p);
            e.Children.Add(child);
        }

        // Where a child effect sits in its parent's space: at its particle, scaled by its size (track_scale), turned by its
        // heading or its own turn (track_orientation_mode).
        static Matrix Frame(ParticleEvent particleEvent, ParticleState p)
        {
            var turn = particleEvent.Turn switch
            {
                VfxEventTurn.Velocity when p.Velocity.LengthSquared() > 1e-12f => VfxConventions.UpTo(Vector3.Normalize(p.Velocity)),
                VfxEventTurn.Particle => Matrix.CreateRotationX(p.TurnXy.X) * Matrix.CreateRotationY(p.TurnXy.Y) * Matrix.CreateRotationZ(p.Angle),
                _ => Matrix.Identity,
            };
            return (particleEvent.TrackScale ? Matrix.CreateScale(p.Size.X) : Matrix.Identity) * turn * Matrix.CreateTranslation(p.Position);
        }

        static void Tint(ChildEffect child, ParticleState p)
        {
            var tint = new Vector4(child.Event.TrackColour ? p.Colour : Vector3.One, child.Event.TrackAlpha ? p.Alpha : 1);
            foreach (var e in child.Simulator.Emitters)
                e.Tint = tint;
        }

        // Particles stay in spawn order, so in id order.
        static Particle? Find(List<Particle> particles, int id)
        {
            var (low, high) = (0, particles.Count - 1);
            while (low <= high)
            {
                var middle = (low + high) / 2;
                var at = particles[middle].Id;
                if (at == id)
                    return particles[middle];
                (low, high) = at < id ? (middle + 1, high) : (low, middle - 1);
            }
            return null;
        }

        void LogCap(EmitterRuntime e, float due, int spawned, int limit)
        {
            if (due <= spawned || e.Particles.Count < limit || limit != ParticleCap || e.CapLogged)
                return;
            e.CapLogged = true;
            _log?.Invoke($"emitter {e.Settings.Name}: {ParticleCap} particles reached, the rest are not spawned");
        }

        // SpawnSpacing: one particle per that many metres the emitter moved this step, laid back along the step's path
        // and aged and moved by the part of the step since it passed there, so a fast emitter leaves them evenly spaced.
        void SpawnAlong(EmitterRuntime e, float age, Matrix world, Vector3 from, float dt, int limit, Vector3 carried, Vector3 moving)
        {
            var spacing = e.Settings.SpawnSpacing;
            var to = world.Translation;
            var travelled = Vector3.Distance(from, to);
            e.Travelled += travelled;
            var due = MathF.Floor(e.Travelled / spacing);
            if (due < 1)
                return;
            e.Travelled -= due * spacing;
            // Oldest first, so the particle list stays in spawn order, the order a trail joins them in.
            var spawned = 0;
            for (var k = (int)Math.Min(due, limit) - 1; k >= 0 && e.Particles.Count < limit; k--, spawned++)
            {
                var back = travelled > 0 ? Math.Clamp((e.Travelled + k * spacing) / travelled, 0, 1) : 0;
                var at = world;
                at.Translation = Vector3.Lerp(to, from, back);
                var count = e.Particles.Count;
                Spawn(e, age, at, carried, moving);
                if (e.Particles.Count <= count)
                    continue;
                var p = e.Particles[^1];
                p.Age = back * dt;
                if (!e.Settings.SkipPositionUpdate)
                    p.Position += p.Velocity * p.Age;
                e.Particles[^1] = p;
            }
            LogCap(e, due, spawned, limit);
        }

        static Matrix EmitterWorld(EmitterRuntime e, float time, Matrix effectWorld)
        {
            var s = e.Settings;
            var age = (time - e.Start) * s.SpeedMultiplier;
            var position = Vector(s.EmitterPosition, VfxConventions.EmitterTime(age, e.Life, s.LoopPosition), e.RTransform);
            var orientation = Vector(s.EmitterOrientation, VfxConventions.EmitterTime(age, e.Life, s.LoopOrientation), e.RTransform);
            return VfxConventions.Rotation(orientation) * Matrix.CreateTranslation(position) * s.ReferenceTransform * effectWorld;
        }

        // carried: the share of the emitter's own velocity a world-space particle starts with (SpawnInheritedSpeed). moving: that
        // velocity, in the effect's space.
        static void Spawn(EmitterRuntime e, float age, Matrix world, Vector3 carried, Vector3 moving)
        {
            var s = e.Settings;
            var random = e.Random;
            float Next() => (float)random.NextDouble();
            float Draw((float Min, float Max) range) => MathHelper.Lerp(range.Min, range.Max, Next());

            var uInitial = VfxConventions.EmitterTime(age, e.Life, s.LoopInitial);
            var life = VfxConventions.Biased(s.ParticleLife?.Evaluate(0, VfxConventions.EmitterTime(age, e.Life, s.LoopLife), Next(), 1) ?? 1, Draw(s.LifeBias));
            if (!(life > 0))   // NaN too (a malformed lifetime bias): it would never die
                return;

            // Where, in the emitter's space: the box, then the ring.
            var uArea = VfxConventions.EmitterTime(age, e.Life, s.LoopArea);
            var offset = Vector3.Zero;
            if (s.SpawnBox != null)
                offset += VfxConventions.BoxOffset(Vector(s.SpawnBox, uArea, Next()), new Vector3(Next(), Next(), Next()));
            if (s.SpawnRing != null)
            {
                var ring = Next();
                var radius = MathHelper.Lerp(s.SpawnRing.Evaluate(0, uArea, ring), s.SpawnRing.Evaluate(1, uArea, ring), Next());
                var (from, to) = VfxConventions.RingArc(s.SpawnArc);
                var angle = MathHelper.ToRadians(MathHelper.Lerp(from, to, Next()));
                offset += new Vector3(MathF.Cos(angle), 0, MathF.Sin(angle)) * radius;
            }

            // Which way, and how fast.
            var direction = VfxConventions.Direction(MathHelper.Lerp(s.SpawnAngle.X, s.SpawnAngle.Y, Next()), Draw(s.Spread));
            // On the ground, keeping the emitter's heading: turned into the emitter's space here, as the world turns it back below.
            if (s.DirectionOnTerrain)
                direction = Vector3.TransformNormal(direction, VfxConventions.RigidFrame(world, true) * Matrix.Invert(VfxConventions.RigidFrame(world, false)));
            if (s.InheritedDirectionBlend > 0 && moving != Vector3.Zero)
                direction = VfxConventions.InheritDirection(direction, Vector3.TransformNormal(moving, Matrix.Invert(world)), s.InheritedDirectionBlend);
            var velocity = direction * (s.InitialSpeed?.Evaluate(0, VfxConventions.EmitterTime(age, e.Life, s.LoopSpeed), Next()) ?? 0);

            var scale = Next();
            var p = new Particle
            {
                Life = life,
                Size = s.InitialScale == null ? Vector2.One : new Vector2(s.InitialScale.Evaluate(0, uInitial, scale, 1), s.InitialScale.Evaluate(1, uInitial, scale, 1)),
                Alpha = s.InitialAlpha?.Evaluate(0, uInitial, Next(), 1) ?? 1,
                Angle = MathHelper.ToRadians(Draw(s.Angle)),
                Spin = s.SpinSign * MathHelper.ToRadians(Draw(s.Spin)),
                Tint = Vector4.Lerp(s.TintMin, s.TintMax, Next()),
                RCell = (float)random.NextDouble(),
                RScale = Next(),
                RDeceleration = Next(),
                RSpinDeceleration = Next(),
                RForce = Next(),
                RAlpha = Next(),
                RColour = Next(),
                RStretch = Next(),
            };
            (p.Position, p.Velocity) = s.Space switch
            {
                SimulationSpace.LocalPositionRotation => (offset, velocity),
                SimulationSpace.LocalPositionOnly => (Vector3.TransformNormal(offset, world), Vector3.TransformNormal(velocity, world)),
                _ => (Vector3.Transform(offset, world), Vector3.TransformNormal(velocity, world) + carried),
            };
            if (s.SpawnOnTerrain)
            {
                var (onEffect, _) = ToEffect(s.Space, world, p.Position, p.Velocity);
                (p.Position, _) = FromEffect(s.Space, world, VfxConventions.OnGround(onEffect), p.Velocity);
            }
            if (s.Collisions)
            {
                p.Bounce = Draw(s.Bounce);
                p.Damping = Draw(s.Damping);
            }
            if (s.FlipU > 0)
                p.FlipU = Next() < s.FlipU;
            if (s.FlipV > 0)
                p.FlipV = Next() < s.FlipV;
            // A rigid model, or a quad turned as a 3D object, turns about X and Y as well as Z.
            if (s.Rigid.Count > 0 || s.Orientation == QuadOrientation.Object3D)
            {
                Vector2 Draw2((Vector2 Min, Vector2 Max) range) => new(MathHelper.Lerp(range.Min.X, range.Max.X, Next()), MathHelper.Lerp(range.Min.Y, range.Max.Y, Next()));
                p.TurnXy = Draw2(s.TurnXy) * (MathF.PI / 180);
                p.SpinXy = s.SpinSignXy * Draw2(s.SpinXy) * (MathF.PI / 180);
                if (s.Rigid.Count > 0)
                    p.Model = Math.Min((int)(Next() * s.Rigid.Count), s.Rigid.Count - 1);
            }
            p.Id = e.NextId++;
            e.Particles.Add(p);
        }

        // world: the emitter's matrix at the end of the step, which a collision is tested against and a vector field rides.
        static void Move(EmitterRuntime e, float dt, Matrix world, float age)
        {
            var s = e.Settings;
            var field = Field(e, world, age);
            for (var i = e.Particles.Count - 1; i >= 0; i--)
            {
                var p = e.Particles[i];
                p.Age += dt;
                if (p.Age >= p.Life)
                {
                    if (s.Events.Count > 0)
                        e.Dead.Add(p);
                    e.Particles.RemoveAt(i);
                    continue;
                }

                var v = p.Age / p.Life;
                var force = Vector3.Zero;
                foreach (var curve in s.Forces)
                    force += Vector(curve, v, p.RForce) * VfxConventions.ForceToMetres;
                var push = Vector3.Zero;
                if (field is var (volume, toParticles, toField, size))
                {
                    push = Vector3.TransformNormal(volume.Sample(Vector3.Transform(p.Position, toField) / size + new Vector3(0.5f)), toParticles)
                        * (s.FieldStrength?.Evaluate(0, v, p.RForce) ?? 0);
                    force += push;
                }
                // Exact for a constant force, so a path under one is the analytic path.
                if (!s.SkipPositionUpdate)
                    p.Position += p.Velocity * dt + force * (dt * dt / 2);
                p.Velocity = (p.Velocity + force * dt) * VfxConventions.Drag(s.Deceleration?.Evaluate(0, v, p.RDeceleration) ?? 0, dt);
                if (field != null && s.FieldTightness?.Evaluate(0, v, p.RForce) is > 0 and var tightness)
                    p.Velocity = Vector3.Lerp(p.Velocity, push, Math.Min(tightness, 1));
                if (s.MaxSpeed > 0 && p.Velocity.LengthSquared() > s.MaxSpeed * s.MaxSpeed)
                    p.Velocity = Vector3.Normalize(p.Velocity) * s.MaxSpeed;
                p.Angle += p.Spin * dt;
                p.TurnXy += p.SpinXy * dt;
                var spinDrag = VfxConventions.Drag(s.SpinDeceleration?.Evaluate(0, v, p.RSpinDeceleration) ?? 0, dt);
                p.Spin *= spinDrag;
                p.SpinXy *= spinDrag;
                if (s.Collisions && !s.SkipPositionUpdate)
                    Collide(s.Space, world, ref p);
                e.Particles[i] = p;
            }
        }

        // VectorField*: read as Unreal's vector fields, whose .fga files CA's came from (one still carries the extension). The
        // volume fills a cube of edge Size, centred at Position and turned by Orientation in the emitter's frame, all three
        // along the emitter's life; it repeats outside itself. Where a particle is, the field times Strength pushes it (m/s2),
        // and Tightness pulls its velocity to that same vector, both along the particle's life. Null when there is none, or
        // Size is 0 (100 vanilla emitters). toParticles: the field's frame in the particles' space; toField: its inverse.
        static (VfxVectorField Volume, Matrix ToParticles, Matrix ToField, float Size)? Field(EmitterRuntime e, Matrix world, float age)
        {
            var s = e.Settings;
            if (s.Field == null)
                return null;
            float U(bool loop) => VfxConventions.EmitterTime(age, e.Life, loop);
            var size = s.FieldSize?.Evaluate(0, U(s.LoopFieldSize), e.RTransform) ?? 0;
            if (!(size > 0))
                return null;
            var frame = VfxConventions.Rotation(Vector(s.FieldOrientation, U(s.LoopFieldOrientation), e.RTransform))
                * Matrix.CreateTranslation(Vector(s.FieldPosition, U(s.LoopFieldPosition), e.RTransform));
            var toParticles = s.Space switch
            {
                SimulationSpace.LocalPositionRotation => frame,
                SimulationSpace.LocalPositionOnly => frame * world * Matrix.CreateTranslation(-world.Translation),
                _ => frame * world,
            };
            return (s.Field, toParticles, Matrix.Invert(toParticles), size);
        }

        // EnableCollisions: the preview's one collider is the ground, y = 0 in the effect's space.
        static void Collide(SimulationSpace space, Matrix world, ref Particle p)
        {
            var (position, velocity) = ToEffect(space, world, p.Position, p.Velocity);
            if (position.Y >= 0)
                return;
            position.Y = 0;
            if (velocity.Y < 0)
                velocity = VfxConventions.Bounce(velocity, p.Bounce, p.Damping);
            (p.Position, p.Velocity) = FromEffect(space, world, position, velocity);
        }

        // A particle between the space it lives in and the effect's, where it is drawn and where the ground is.
        static (Vector3 Position, Vector3 Velocity) ToEffect(SimulationSpace space, Matrix world, Vector3 position, Vector3 velocity) => space switch
        {
            SimulationSpace.LocalPositionRotation => (Vector3.Transform(position, world), Vector3.TransformNormal(velocity, world)),
            SimulationSpace.LocalPositionOnly => (position + world.Translation, velocity),
            _ => (position, velocity),
        };

        static (Vector3 Position, Vector3 Velocity) FromEffect(SimulationSpace space, Matrix world, Vector3 position, Vector3 velocity)
        {
            if (space != SimulationSpace.LocalPositionRotation)
                return space == SimulationSpace.LocalPositionOnly ? (position - world.Translation, velocity) : (position, velocity);
            var inverse = Matrix.Invert(world);
            return (Vector3.Transform(position, inverse), Vector3.TransformNormal(velocity, inverse));
        }

        /// <summary>An emitter's live particles as drawn now, in the effect's model space.</summary>
        public static IEnumerable<ParticleState> States(EmitterRuntime e) => e.Particles.Select(p => State(e, p, e.World));

        // One particle as drawn, given its emitter's matrix; a particle event's emitter takes its particle's tint on top.
        static ParticleState State(EmitterRuntime e, Particle p, Matrix world)
        {
            var s = e.Settings;
            var scale = new Vector3(world.M11, world.M12, world.M13).Length();
            var v = p.Age / p.Life;
            var (position, velocity) = ToEffect(s.Space, world, p.Position, p.Velocity);
            var size = p.Size * scale * (s.Scale == null ? Vector2.One : new Vector2(s.Scale.Evaluate(0, v, p.RScale, 1), s.Scale.Evaluate(1, v, p.RScale, 1)));
            var colour = Vector(s.Colour, v, p.RColour, 1) * new Vector3(p.Tint.X, p.Tint.Y, p.Tint.Z) * new Vector3(e.Tint.X, e.Tint.Y, e.Tint.Z)
                * VfxConventions.Hdr(s.Colour?.Evaluate(3, v, p.RColour, 1) ?? 1, s.EmissiveMul);
            var alpha = p.Alpha * (s.Alpha?.Evaluate(0, VfxConventions.AlphaLife(v, s.AlphaStartOffset, s.AlphaEndOffset, p.RAlpha), p.RAlpha, 1) ?? 1) * p.Tint.W * e.Tint.W;
            var cells = s.CellsX * s.CellsY;
            var (cell, next, blend) = s.Animated ? VfxConventions.FlipbookFrame(s, v, p.Age, MathHelper.Lerp(s.Loops.X, s.Loops.Y, v), p.RCell)
                : (s.RandomCell ? Math.Min((int)(p.RCell * cells), cells - 1) : s.Cell, 0, 0f);
            return new ParticleState(position, velocity, size, p.Angle, colour, alpha, s.Stretch?.Evaluate(0, v, p.RStretch) ?? 0, cell, p.FlipU, p.FlipV, next, s.CellBlend ? blend : 0,
                s.AlphaCrush?.Evaluate(0, v, p.RAlpha) ?? 0, p.TurnXy, p.Model, p.Age);
        }

        static Vector3 Vector(VfxCurve? curve, float t, float random, float fallback = 0) => curve == null
            ? new Vector3(fallback)
            : new Vector3(curve.Evaluate(0, t, random, fallback), curve.Evaluate(1, t, random, fallback), curve.Evaluate(2, t, random, fallback));
    }
}
