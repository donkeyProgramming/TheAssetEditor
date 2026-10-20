using System.Globalization;
using GameWorld.Core.Vfx;
using Microsoft.Xna.Framework;
using Shared.GameFormats.Vfx;

namespace GameWorld.Core.Test.Vfx
{
    public class ParticleSimulatorTests
    {
        static VfxCurve C(params float[] values) => VfxCurve.Constant(values);

        // From one value at t = 0 to another at t = 1, linear.
        static VfxCurve Ramp(float from, float to) => VfxCurve.Parse(string.Create(CultureInfo.InvariantCulture,
            $"x_min,0,0;0.0,{from},linear,linear,0,0,0,0,1;1.0,{to},linear,linear,0,0,0,0,1|x_max,0,0;0.0,{from},linear,linear,0,0,0,0,1;1.0,{to},linear,linear,0,0,0,0,1:x,0;0,1"));

        // A random point between two constants, drawn per particle.
        static VfxCurve Between(float min, float max) => VfxCurve.Parse(string.Create(CultureInfo.InvariantCulture,
            $"x_min,0,0;0.0,{min},linear,linear,0,0,0,0,1|x_max,0,0;0.0,{max},linear,linear,0,0,0,0,1:x,1;0,1"));

        // Ten particles a second that live 100 s and do not move, from an emitter that never stops.
        static EmitterSettings Steady(Action<EmitterSettings> change = null)
        {
            var s = new EmitterSettings { Name = "test", InfiniteLife = true, Rate = C(10), ParticleLife = C(100), Spread = (0, 0) };
            change?.Invoke(s);
            return s;
        }

        static ParticleSimulator Run(EmitterSettings s, float time, Func<float, Matrix> effectWorldAt = null, float spawnEnd = float.PositiveInfinity, int seed = 1, Action<string> log = null)
        {
            var simulator = new ParticleSimulator([s], seed, effectWorldAt ?? (_ => Matrix.Identity), spawnEnd, log);
            simulator.AdvanceTo(time);
            return simulator;
        }

        static int Count(ParticleSimulator simulator) => simulator.Emitters.Sum(x => x.Particles.Count);
        static List<ParticleState> States(ParticleSimulator simulator) => simulator.Emitters.SelectMany(ParticleSimulator.States).ToList();

        // A modder's rate past 2^25 particles a step made "accumulator -= 1" a no-op, so the spawn loop never ended. The cap
        // holds over an emitter's own MaximumParticles too.
        [Test]
        public void HugeEmissionRate_StopsAtTheCap_AndReturns()
        {
            var run = Task.Run(() => Run(Steady(x => { x.Rate = C(1e12f); x.MaxParticles = 1_000_000; }), 0.5f));
            Assert.That(run.Wait(TimeSpan.FromSeconds(20)), Is.True, "AdvanceTo returned");
            Assert.That(Count(run.Result), Is.EqualTo(ParticleSimulator.ParticleCap));
        }

        // A NaN lifetime bias (a malformed override attribute) passed "life <= 0", and the particle never died.
        [Test]
        public void NaNLifetimeBias_SpawnsNothing() => Assert.That(Count(Run(Steady(x => x.LifeBias = (float.NaN, float.NaN)), 1)), Is.Zero);

        // The bone turns a quarter turn about Y and walks 5 m along X in the first second.
        static Matrix MovingBone(float t) => Matrix.CreateRotationY(t * MathHelper.PiOver2) * Matrix.CreateTranslation(5 * t, 0, 0);

        [Test]
        public void SameSeed_SameParticles_OtherSeed_Others()
        {
            EmitterSettings Spray() => Steady(s => { s.SpawnAngle = new Vector2(0, 360); s.Spread = (10, 80); s.InitialSpeed = Between(1, 5); });
            var a = States(Run(Spray(), 2));
            Assert.That(a, Is.Not.Empty);
            Assert.That(States(Run(Spray(), 2)), Is.EqualTo(a));
            Assert.That(States(Run(Spray(), 2, seed: 2)), Is.Not.EqualTo(a));
        }

        [Test]
        public void SpawnedCount_IsTheIntegralOfTheRateCurve()
        {
            // 10 a second rising to 30 over the emitter's 1 s life: 20 in all.
            var s = Steady(x => { x.InfiniteLife = false; x.LifeTime = (1, 1); x.Rate = Ramp(10, 30); });
            Assert.That(Count(Run(s, 2)), Is.EqualTo(20).Within(1));
        }

        // A reference's spawn_rate_scale_factor multiplies the rate: 10 a second at 2.5 is 25 in a second.
        [Test]
        public void RateScale_MultipliesTheRate() => Assert.That(Count(Run(Steady(x => x.RateScale = (2.5f, 2.5f)), 1)), Is.EqualTo(25).Within(1));

        [Test]
        public void NegativeStartTime_PreWarms()
        {
            var s = Steady(x => x.StartTime = (-5, -5));
            Assert.That(Count(Run(s, -0.5f)), Is.Zero, "the effect has not started");
            Assert.That(Count(Run(s, 0)), Is.EqualTo(50).Within(1), "5 s of 10 a second, already there at the start");
        }

        [Test]
        public void PreWarm_IsCappedAt30Seconds()
        {
            var s = Steady(x => { x.StartTime = (-1000, -1000); x.ParticleLife = C(10000); });
            Assert.That(Count(Run(s, 0)), Is.EqualTo(300).Within(1));
        }

        [Test]
        public void ParticlesDie_AtTheirLifetime()
        {
            var simulator = Run(Steady(x => x.ParticleLife = C(0.5f)), 3);
            Assert.That(Count(simulator), Is.EqualTo(5).Within(1), "10 a second, living half a second");
            Assert.That(simulator.Emitters[0].Particles.All(x => x.Age < x.Life));
        }

        [Test]
        public void ZeroParticleLife_SpawnsNothing() => Assert.That(Count(Run(Steady(x => x.ParticleLife = C(0)), 1)), Is.Zero);

        // SkipParticlePositionUpdate (651 vanilla emitters, the nuln rifle's head among them): a particle stays where it
        // spawned and keeps its velocity, which still aligns and stretches it.
        [TestCase(false, true)]
        [TestCase(true, false)]
        public void SkipPositionUpdate_KeepsParticlesWhereTheySpawned(bool skip, bool moved)
        {
            var states = States(Run(Steady(x => { x.InitialSpeed = C(5); x.SkipPositionUpdate = skip; }), 1));
            Assert.That(states, Has.Count.GreaterThan(5));
            Assert.That(states.Any(x => x.Position.Length() > 1), Is.EqualTo(moved));
            Assert.That(states.All(x => x.Velocity.Length() > 4.9f), Is.True);
        }

        // SpawnSpacing: one particle per that many metres moved, laid evenly along the path, on top of the rate. At 150 m/s
        // a step moves 2.5 m, so particles bunched at each step's end would show as gaps of 2 and 3.
        [Test]
        public void SpawnSpacing_LaysOneParticlePerSpacingAlongThePath()
        {
            var states = States(Run(Steady(x => { x.Rate = C(0); x.SpawnSpacing = 1; }), 1, t => Matrix.CreateTranslation(150 * t, 0, 0)));
            var xs = states.Select(x => x.Position.X).OrderBy(x => x).ToList();
            Assert.That(xs.Count, Is.EqualTo(150).Within(2));
            var gaps = xs.Zip(xs.Skip(1), (a, b) => b - a).ToList();
            Assert.That(gaps.Max() - gaps.Min(), Is.LessThan(0.01f), string.Join(", ", gaps.Take(6)));
        }

        // A trail joins its particles in the order they come out, so that order is the order they were spawned in.
        [Test]
        public void SpawnSpacing_ParticlesComeOutOldestFirst()
        {
            var xs = States(Run(Steady(x => { x.Rate = C(0); x.SpawnSpacing = 1; }), 1, t => Matrix.CreateTranslation(150 * t, 0, 0))).Select(x => x.Position.X).ToList();
            Assert.That(xs, Is.Ordered.Ascending);
        }

        // 334 vanilla SpawnSpacing emitters run at rate 0, so the distance spawner meets the cap on its own.
        [Test]
        public void SpawnSpacing_ParticleCap_IsLoggedOnce()
        {
            var log = new List<string>();
            var simulator = Run(Steady(x => { x.Rate = C(0); x.SpawnSpacing = 0.01f; }), 0.5f, t => Matrix.CreateTranslation(150 * t, 0, 0), log: log.Add);
            Assert.That((Count(simulator), log.Count), Is.EqualTo((ParticleSimulator.ParticleCap, 1)));
        }

        // A particle laid back along the step is as old as the time since the emitter passed there, and has moved for it.
        [Test]
        public void SpawnSpacing_ParticlesHaveMovedForTheirAge()
        {
            var s = Steady(x => { x.Rate = C(0); x.SpawnSpacing = 1; x.InitialSpeed = C(2); x.Spread = (90, 90); });
            var particles = Run(s, 0.5f, t => Matrix.CreateTranslation(150 * t, 0, 0)).Emitters[0].Particles;
            Assert.That(particles.Max(p => MathF.Abs(p.Position.Y - 2 * p.Age)), Is.LessThan(1e-3f));
        }

        // SpawnInheritedSpeed hands a world-space particle that share of the emitter's velocity; a local-space one
        // already moves with its emitter.
        [TestCase(SimulationSpace.World, 5f)]
        [TestCase(SimulationSpace.LocalPositionOnly, 0f)]
        public void InheritedSpeed_IsAShareOfTheEmittersVelocity(SimulationSpace space, float expected)
        {
            var s = Steady(x => { x.Space = space; x.InheritedSpeed = 0.5f; });
            var velocities = Run(s, 1, t => Matrix.CreateTranslation(10 * t, 0, 0)).Emitters[0].Particles.Select(p => p.Velocity).ToList();
            Assert.That(velocities, Is.Not.Empty);
            Assert.That(velocities.Max(v => Vector3.Distance(v, new Vector3(expected, 0, 0))), Is.LessThan(1e-3f));
        }

        // SpawnInheritedDirectionBlend turns a particle's spawn direction that share of the way toward the way its emitter is
        // moving: straight up, half way toward +X, is 45 degrees, in every space. A still emitter changes nothing.
        [TestCase(SimulationSpace.World)]
        [TestCase(SimulationSpace.LocalPositionOnly)]
        [TestCase(SimulationSpace.LocalPositionRotation)]
        public void InheritedDirectionBlend_TurnsTheSpawnTowardTheEmittersMotion(SimulationSpace space)
        {
            var s = Steady(x => { x.Space = space; x.InitialSpeed = C(1); x.Spread = (90, 90); x.InheritedDirectionBlend = 0.5f; });
            var moving = Run(s, 1, t => Matrix.CreateTranslation(10 * t, 0, 0)).Emitters[0].Particles.Select(p => p.Velocity).ToList();
            var still = Run(s, 1).Emitters[0].Particles.Select(p => p.Velocity).ToList();
            Assert.That(moving, Is.Not.Empty);
            Assert.Multiple(() =>
            {
                Assert.That(moving.Max(v => Vector3.Distance(v, Vector3.Normalize(new Vector3(1, 1, 0)))), Is.LessThan(1e-3f), "half way to +X");
                Assert.That(still.Max(v => Vector3.Distance(v, Vector3.UnitY)), Is.LessThan(1e-3f), "still");
            });
        }

        // ParticleRelativeSpawnDirection RELATIVE_TO_TERRAIN_NORMAL: the spawn direction is taken on the ground, the preview's flat
        // one, keeping only the emitter's heading, as RigidFrame stands a rigid model there; without it the direction turns with the
        // emitter. An emitter rolled 30 degrees and turned 90: up is (0, 0.866, 0.5) on it and straight up on the ground; +X is
        // (0, 0.5, -0.866) on it and -Z, its heading, on the ground.
        [TestCase(false, 90f, 0f, 0.866f, 0.5f)]
        [TestCase(true, 90f, 0f, 1f, 0f)]
        [TestCase(false, 0f, 0f, 0.5f, -0.866f)]
        [TestCase(true, 0f, 0f, 0f, -1f)]
        public void SpawnDirection_OnTerrain_KeepsOnlyTheEmittersHeading(bool onTerrain, float elevation, float x, float y, float z)
        {
            var s = Steady(e => { e.MaxParticles = 1; e.InitialSpeed = C(1); e.SpawnAngle = Vector2.Zero; e.Spread = (elevation, elevation); e.DirectionOnTerrain = onTerrain; });
            var tilted = Matrix.CreateRotationZ(MathHelper.ToRadians(30)) * Matrix.CreateRotationY(MathHelper.PiOver2);
            var velocity = Run(s, 1, _ => tilted).Emitters[0].Particles.Single().Velocity;
            Assert.That(Vector3.Distance(velocity, new Vector3(x, y, z)), Is.LessThan(1e-3f), velocity.ToString());
        }

        static List<EmitterRuntime> Children(ParticleSimulator simulator) => simulator.AllEmitters.Where(x => x.Settings.Name == "child").ToList();

        // OnParticleSpawn: each particle starts its own copy of the event's effect, which follows it (track_position): a still
        // child emitter's newest particle, a step old, is where its parent is now.
        [Test]
        public void SpawnEvent_StartsAChildEffectThatFollowsItsParticle()
        {
            var child = Steady(x => { x.Name = "child"; x.Rate = C(60); });
            var parent = Steady(x => { x.MaxParticles = 1; x.InitialSpeed = C(1); x.Spread = (90, 90); x.Events = [new ParticleEvent(false, [child], TrackPosition: true)]; });
            var simulator = Run(parent, 1);
            var children = Children(simulator);
            var at = ParticleSimulator.States(simulator.Emitters[0]).Single().Position.Y;
            Assert.That(children, Has.Count.EqualTo(1));
            Assert.That(at, Is.GreaterThan(0.5f), "the parent rose");
            Assert.That(ParticleSimulator.States(children[0]).Last().Position.Y, Is.EqualTo(at).Within(0.03));
        }

        // OnParticleDeath: a particle that dies starts the event's effect where it died, 1 m up after half a second at 2 m/s.
        [Test]
        public void DeathEvent_StartsTheChildEffectWhereTheParticleDied()
        {
            var child = Steady(x => x.Name = "child");
            var parent = Steady(x => { x.MaxParticles = 1; x.ParticleLife = C(0.5f); x.InitialSpeed = C(2); x.Spread = (90, 90); x.Events = [new ParticleEvent(true, [child])]; });
            var children = Children(Run(parent, 1, spawnEnd: 0.1f));
            Assert.That(children, Has.Count.EqualTo(1));
            Assert.That(ParticleSimulator.States(children[0]).Select(p => p.Position.Y), Is.Not.Empty.And.All.EqualTo(1).Within(0.05));
        }

        // track_lifetime: when its particle dies the child effect stops spawning and ends with its own particles; without it,
        // an infinite child goes on.
        [TestCase(true, 0)]
        [TestCase(false, 1)]
        public void TrackLifetime_EndsTheChildWithItsParticle(bool track, int left)
        {
            var child = Steady(x => { x.Name = "child"; x.ParticleLife = C(0.2f); });
            var parent = Steady(x => { x.MaxParticles = 1; x.ParticleLife = C(0.5f); x.Events = [new ParticleEvent(false, [child], TrackLifetime: track)]; });
            Assert.That(Children(Run(parent, 1, spawnEnd: 0.1f)).Count(x => x.Particles.Count > 0), Is.EqualTo(left));
        }

        // track_colour and track_alpha: the child's particles take their parent's colour and alpha.
        [TestCase(true, 0f, 0.5f)]
        [TestCase(false, 1f, 1f)]
        public void TrackColourAndAlpha_TintTheChildByItsParticle(bool track, float green, float alpha)
        {
            var child = Steady(x => x.Name = "child");
            var parent = Steady(x => { x.MaxParticles = 1; x.Colour = C(1, 0, 0); x.InitialAlpha = C(0.5f); x.Events = [new ParticleEvent(false, [child], TrackColour: track, TrackAlpha: track)]; });
            var state = ParticleSimulator.States(Children(Run(parent, 1)).Single()).Last();
            Assert.That((state.Colour.Y, state.Alpha), Is.EqualTo((green, alpha)));
        }

        // A child effect per particle is a whole simulation: past 64 live on one emitter the rest do not start, and the preview
        // says so once.
        [Test]
        public void ChildEffects_AreCappedPerEmitter_AndSaySo()
        {
            var log = new List<string>();
            var child = Steady(x => x.Name = "child");
            var parent = Steady(x => { x.Rate = C(600); x.Events = [new ParticleEvent(false, [child])]; });
            var children = Children(Run(parent, 1, log: log.Add));
            Assert.That((children.Count, log.Count(x => x.Contains("64"))), Is.EqualTo((64, 1)));
        }

        // ParticlesSpawnOnTerrain: the preview's terrain is the flat ground, y = 0 in the effect's space, whatever space
        // the particle lives in.
        [TestCase(SimulationSpace.World)]
        [TestCase(SimulationSpace.LocalPositionOnly)]
        [TestCase(SimulationSpace.LocalPositionRotation)]
        public void SpawnOnTerrain_PutsParticlesOnTheGround(SimulationSpace space)
        {
            var s = Steady(x => { x.Space = space; x.SpawnOnTerrain = true; });
            var states = States(Run(s, 1, _ => Matrix.CreateRotationZ(0.3f) * Matrix.CreateTranslation(2, 5, 0)));
            Assert.That(states, Is.Not.Empty);
            Assert.That(states.Max(p => Math.Abs(p.Position.Y)), Is.LessThan(1e-3f));
        }

        // EnableCollisions: a particle that reaches the ground leaves it with its fall scaled by the bounce and its
        // slide by one minus the damping, wherever it lives.
        [TestCase(SimulationSpace.World)]
        [TestCase(SimulationSpace.LocalPositionOnly)]
        [TestCase(SimulationSpace.LocalPositionRotation)]
        public void Collisions_BounceOffTheGround(SimulationSpace space)
        {
            var s = Steady(x =>
            {
                x.Space = space;
                x.SpawnAngle = Vector2.Zero;
                x.Spread = (-45, -45);
                x.InitialSpeed = C(10);
                x.Collisions = true;
                x.Bounce = (0.5f, 0.5f);
                x.Damping = (0.5f, 0.5f);
            });
            var states = States(Run(s, 0.5f, _ => Matrix.CreateTranslation(0, 1, 0)));
            var c = 10 / MathF.Sqrt(2);
            var falling = new Vector3(c, -c, 0);
            var bounced = new Vector3(c / 2, c / 2, 0);
            Assert.That(states.Min(p => p.Position.Y), Is.GreaterThan(-1e-3f));
            Assert.That(states.Count(p => Vector3.Distance(p.Velocity, bounced) < 1e-3f), Is.GreaterThan(0));
            Assert.That(states.All(p => Vector3.Distance(p.Velocity, bounced) < 1e-3f || Vector3.Distance(p.Velocity, falling) < 1e-3f), Is.True);
        }

        // vs_flip_uv: each particle's texture is mirrored with the chain's chance, per axis.
        [Test]
        public void FlipUv_MirrorsParticlesByChance()
        {
            var states = States(Run(Steady(x => { x.FlipU = 1; x.FlipV = 0.5f; }), 10));
            Assert.That(states.All(p => p.FlipU), Is.True);
            Assert.That(states.Count(p => p.FlipV), Is.InRange(states.Count / 4, states.Count * 3 / 4));
        }

        // A rigid particle turns on every axis: X and Y drawn in their ranges and spun on by their own signs; with two models,
        // each particle picks one.
        [Test]
        public void RigidParticles_TurnOnEveryAxis_AndPickAModel()
        {
            var rigid = Steady(x =>
            {
                x.Rigid = [new("a.wsmodel", new Vector3(0.5f), null), new("b.wsmodel", new Vector3(0.5f), null)];
                x.TurnXy = (new Vector2(10, 20), new Vector2(10, 20));
                x.SpinXy = (new Vector2(90, 0), new Vector2(90, 0));
                x.SpinSignXy = new Vector2(-1, 1);
            });
            var simulator = Run(rigid, 1);
            var pairs = simulator.Emitters[0].Particles.Zip(States(simulator)).ToList();
            Assert.Multiple(() =>
            {
                Assert.That(pairs.Select(x => x.Second.TurnXy.X - MathHelper.ToRadians(10 - 90 * x.First.Age)), Is.All.EqualTo(0).Within(1e-4));
                Assert.That(pairs.Select(x => x.Second.TurnXy.Y), Is.All.EqualTo(MathHelper.ToRadians(20)).Within(1e-5));
                Assert.That(pairs.Select(x => x.Second.Model).Distinct().Order(), Is.EqualTo(new[] { 0, 1 }));
            });
        }

        // vs_translation's restriction 3 turns a quad as a 3D object, so like a rigid particle it draws its X and Y turn.
        [Test]
        public void Object3DQuads_TurnOnEveryAxis()
        {
            var swipe = Steady(x => { x.Orientation = QuadOrientation.Object3D; x.TurnXy = (new Vector2(90, 20), new Vector2(90, 20)); });
            var turned = new Vector2(MathHelper.ToRadians(90), MathHelper.ToRadians(20));
            Assert.That(States(Run(swipe, 1)).Select(x => Vector2.Distance(x.TurnXy, turned)), Is.Not.Empty.And.All.LessThan(1e-5f));
        }

        // Minutes into a clock a float time cannot hold a 1e-5 s tolerance. Asked for the same time twice, the simulator must
        // not start again from nothing: a paused persistent effect did, every frame.
        [Test]
        public void TheSameTimeTwice_DoesNotStartAgain_MinutesIn()
        {
            var simulator = new ParticleSimulator([Steady(x => x.Rate = C(0))], 1, _ => Matrix.Identity);
            var restarts = 0;
            for (var t = 250f; t < 400f; t += 0.0173f)
            {
                simulator.AdvanceTo(t);
                var emitter = simulator.Emitters[0];
                simulator.AdvanceTo(t);
                restarts += ReferenceEquals(emitter, simulator.Emitters[0]) ? 0 : 1;
            }
            Assert.That(restarts, Is.Zero);
        }

        // A vector field volume: x along a row, y down the rows, z through the slices, sampled between cell centres and
        // repeating outside itself.
        [Test]
        public void VectorField_SamplesBetweenCellCentres_AndRepeats()
        {
            var field = VfxVectorField.Read(VfxTestFiles.Field(2, 2, 2, (x, y, z) => new Vector3(x, y * 2, z * 4)))!;
            Assert.Multiple(() =>
            {
                Assert.That(field.Sample(new Vector3(0.75f, 0.25f, 0.75f)), Is.EqualTo(new Vector3(1, 0, 4)), "cell (1, 0, 1)");
                Assert.That(field.Sample(new Vector3(0.5f, 0.75f, 0.25f)), Is.EqualTo(new Vector3(0.5f, 2, 0)), "half way along x");
                Assert.That(field.Sample(new Vector3(1.75f, -0.25f, 0.75f)), Is.EqualTo(new Vector3(1, 2, 4)), "a volume over, either way");
                Assert.That(VfxVectorField.Read(VfxTestFiles.Xml()), Is.Null, "not a field");
            });
        }

        // VectorField*: where a particle is, the field times Strength pushes it (m/s2), turned by Orientation as an emitter
        // is; Tightness pulls its velocity to that same vector.
        [Test]
        public void VectorField_PushesParticles_AndTightnessPullsTheirVelocity()
        {
            var field = VfxVectorField.Read(VfxTestFiles.Field(2, 2, 2, (_, _, _) => Vector3.UnitX))!;
            EmitterSettings Fielded(float tightness) => Steady(x =>
                (x.Field, x.FieldSize, x.FieldStrength, x.FieldTightness, x.FieldOrientation) = (field, C(4), C(2), C(tightness), C(0, 90, 0)));
            var turned = Vector3.TransformNormal(Vector3.UnitX, VfxConventions.Rotation(new Vector3(0, 90, 0)));
            var loose = Run(Fielded(0), 1).Emitters[0].Particles;
            var tight = Run(Fielded(1), 1).Emitters[0].Particles;
            Assert.Multiple(() =>
            {
                Assert.That(loose.Select(p => Vector3.Distance(p.Velocity, turned * 2 * p.Age)), Is.All.LessThan(1e-3));
                Assert.That(tight.Where(p => p.Age > 0).Select(p => Vector3.Distance(p.Velocity, turned * 2)), Is.All.LessThan(1e-4));
            });
        }

        // The field rides its emitter: Position moves it in the emitter's frame, so an emitter carried 11 m still finds its
        // particles in the field's second half. Neither the offset nor the ride alone puts them there.
        [Test]
        public void VectorField_IsPlacedInTheEmittersFrame()
        {
            var field = VfxVectorField.Read(VfxTestFiles.Field(4, 1, 1, (x, _, _) => new Vector3(x >= 2 ? 1 : 0, 0, 0)))!;
            var s = Steady(x => (x.Field, x.FieldSize, x.FieldStrength, x.FieldPosition) = (field, C(4), C(1), C(-0.5f, 0, 0)));
            var particles = Run(s, 1, _ => Matrix.CreateTranslation(11, 0, 0)).Emitters[0].Particles;
            Assert.That(particles.Select(p => p.Velocity.X - p.Age), Is.All.EqualTo(0).Within(1e-3));
        }

        // Vectorfield{Size,Position,Orientation}LoopAnim wrap their curves over the emitter's life, as EmitterPositionLoopAnim and
        // EmitterOrientationLoopAnim wrap the emitter's own; unset, they hold at the end. An emitter living 1 s spawns on, and the
        // particles born after it are pushed up only while the curve is wrapped back to where it pushes: Size 4 to 0 (none at the
        // end), Position -1 to 1 m across a field pushing only in its upper half (out of it at the end), Orientation 0 to 90
        // degrees about X (pushing sideways at the end).
        [TestCase("size", false)]
        [TestCase("size", true)]
        [TestCase("position", false)]
        [TestCase("position", true)]
        [TestCase("orientation", false)]
        [TestCase("orientation", true)]
        public void VectorFieldCurves_LoopOverTheEmittersLife(string curve, bool loop)
        {
            var uniform = VfxVectorField.Read(VfxTestFiles.Field(2, 2, 2, (_, _, _) => Vector3.UnitY))!;
            var half = VfxVectorField.Read(VfxTestFiles.Field(4, 1, 1, (x, _, _) => new Vector3(0, x >= 2 ? 1 : 0, 0)))!;
            var s = Steady(x =>
            {
                (x.LifeTime, x.Field, x.FieldStrength, x.FieldSize) = ((1, 1), curve == "position" ? half : uniform, C(1), C(4));
                if (curve == "size")
                    (x.FieldSize, x.LoopFieldSize) = (Ramp(4, 0), loop);
                if (curve == "position")
                    (x.FieldPosition, x.LoopFieldPosition) = (Ramp(-1, 1), loop);
                if (curve == "orientation")
                    (x.FieldOrientation, x.LoopFieldOrientation) = (Ramp(0, 90), loop);
            });
            var late = Run(s, 1.9f).Emitters[0].Particles.Where(p => p.Age is > 0.1f and < 0.8f).ToList();
            Assert.That(late, Is.Not.Empty);
            Assert.That(late.Max(p => p.Velocity.Y / p.Age), loop ? Is.GreaterThan(0.05f) : Is.LessThan(0.01f));
        }

        // MaximumVelocity caps a particle's speed: all 31 vanilla uses are 1 m/s on particles launched still and carried by a
        // force or a vector field, which would otherwise speed them up for as long as they live. A push of 5 m/s2 for a second
        // stops at the cap, along its way; one of 0.5 m/s2 never reaches it and is left alone.
        [TestCase(500f, 1f, true)]
        [TestCase(50f, 1f, false)]
        [TestCase(500f, 0f, false)]
        public void MaximumVelocity_CapsTheSpeed(float force, float cap, bool capped)
        {
            var s = Steady(x => { x.MaxParticles = 1; x.Forces = [C(0.6f * force, 0.8f * force, 0)]; x.MaxSpeed = cap; });
            var p = Run(s, 1).Emitters[0].Particles.Single();
            Assert.That(p.Velocity.Length(), Is.EqualTo(capped ? cap : force * VfxConventions.ForceToMetres * p.Age).Within(1e-3));
        }

        // ps_alpha_crush: each particle carries its crush level, off the curve over its life.
        [Test]
        public void AlphaCrush_IsCarriedByEachParticle()
        {
            var states = States(Run(Steady(x => x.AlphaCrush = C(0.3f)), 1));
            Assert.That(states.Select(p => p.Crush).Distinct(), Is.EqualTo(new[] { 0.3f }));
        }

        // curve_colour's fourth output, brightness, and ps_emissive's emissive_mul both multiply the colour, past 1.
        [Test]
        public void Brightness_AndEmissive_MultiplyTheColour()
        {
            var curve = VfxCurve.Parse("half,1,1;0.0,0.5,linear,linear,0,0,0,0,0|four,1,1;0.0,4.0,linear,linear,0,0,0,0,0:r,0;0,0|g,0;0,0|b,0;0,0|brightness,0;1,1");
            var states = States(Run(Steady(x => { x.Colour = curve; x.EmissiveMul = 3; }), 1));
            Assert.That(states.Select(p => p.Colour).Distinct(), Is.EqualTo(new[] { new Vector3(6) }));
        }

        [Test]
        public void SpawnSpacing_StandingStill_SpawnsOnlyByRate()
        {
            Assert.That(Count(Run(Steady(x => { x.Rate = C(0); x.SpawnSpacing = 1; }), 1)), Is.Zero);
            Assert.That(Count(Run(Steady(x => x.SpawnSpacing = 1), 1)), Is.EqualTo(10).Within(1));
        }

        [Test]
        public void ZeroEmitterLifeTime_GivesFiniteStates()
        {
            var states = States(Run(Steady(x => { x.LifeTime = (0, 0); x.LoopRate = true; x.InitialSpeed = C(1); }), 1));
            Assert.That(states, Is.Not.Empty);
            Assert.That(states.All(x => float.IsFinite(x.Position.X + x.Position.Y + x.Position.Z + x.Alpha + x.Size.X)));
        }

        // Forces are cm/s2: vanilla's falling debris carries -981 (156 emitters), rocks -1471.5, sand -490.5 and
        // -1962, against 101 uses of 9.81. Read as m/s2, Belegar's rising embers (+98.1..196.2) shot up at 20-40 m/s.
        [Test]
        public void ConstantForce_IsInCentimetresPerSecondSquared_AndGivesTheAnalyticPath()
        {
            // One particle, 2 m/s along X, under one g downward: -981 cm/s2, 9.81 m/s2.
            var s = Steady(x => { x.MaxParticles = 1; x.InitialSpeed = C(2); x.SpawnAngle = Vector2.Zero; x.Forces = [C(0, -981, 0)]; });
            var p = Run(s, 1).Emitters[0].Particles.Single();
            Assert.That(Vector3.Distance(p.Position, new Vector3(2 * p.Age, -4.905f * p.Age * p.Age, 0)), Is.LessThan(1e-3f));
        }

        [TestCase(SimulationSpace.World, 0f, 0f, 1f, Description = "stays where it spawned")]
        [TestCase(SimulationSpace.LocalPositionRotation, 6f, 0f, 0f, Description = "moves and turns with the bone")]
        [TestCase(SimulationSpace.LocalPositionOnly, 5f, 0f, 1f, Description = "moves with the bone, does not turn")]
        public void SimulationSpace_DecidesWhatTheBoneCarries(SimulationSpace space, float x, float y, float z)
        {
            // One still particle spawned 1 m along the emitter's Z: a ring of radius 1, at 90 degrees.
            var s = Steady(e => { e.Space = space; e.MaxParticles = 1; e.Rate = C(600); e.SpawnRing = C(1, 1); e.SpawnArc = new Vector2(90, 90); });
            var state = States(Run(s, 1, MovingBone)).Single();
            Assert.That(Vector3.Distance(state.Position, new Vector3(x, y, z)), Is.LessThan(0.15f));
        }

        [Test]
        public void ScrubBack_ThenForward_EqualsAStraightRun()
        {
            EmitterSettings Spray() => Steady(s => { s.SpawnAngle = new Vector2(0, 360); s.InitialSpeed = Between(1, 5); s.ParticleLife = Between(0.5f, 2); });
            var straight = Run(Spray(), 2, MovingBone);
            var scrubbed = Run(Spray(), 3, MovingBone);
            scrubbed.AdvanceTo(2);
            Assert.That(States(scrubbed), Is.EqualTo(States(straight)));
        }

        [Test]
        public void TagEnd_StopsSpawning_AndTheLiveOnesLiveOn() => Assert.That(Count(Run(Steady(), 2, spawnEnd: 0.5f)), Is.EqualTo(5).Within(1));

        [Test]
        public void ParticleCap_Holds_AndIsLoggedOnce()
        {
            var log = new List<string>();
            var simulator = Run(Steady(x => x.Rate = C(100000)), 0.5f, log: log.Add);
            Assert.That(Count(simulator), Is.EqualTo(ParticleSimulator.ParticleCap));
            Assert.That(log, Has.Count.EqualTo(1));
        }

        [Test]
        public void OverLife_SizeAndAlphaFollowTheirCurves()
        {
            // Spawned at size 2 and alpha 1, living 2 s; ParticleScale goes 1 to 3 and curve_alpha 1 to 0 over that life.
            var s = Steady(x => { x.MaxParticles = 1; x.ParticleLife = C(2); x.InitialScale = C(2, 2, 2); x.Scale = Ramp(1, 3); x.Alpha = Ramp(1, 0); });
            var simulator = Run(s, 1);
            var p = simulator.Emitters[0].Particles.Single();
            var state = States(simulator).Single();
            var life = p.Age / p.Life;
            Assert.That(state.Size.X, Is.EqualTo(2 * (1 + 2 * life)).Within(1e-3));
            Assert.That(state.Alpha, Is.EqualTo(1 - life).Within(1e-3));
        }

        // vs_vertex_alpha's alpha offsets squeeze curve_alpha into the window from the start offset to 1 less the end offset, each
        // lerped by the particle's alpha random, as CA's map_range has it, and hold its ends outside: frost_wyrm's end offset of
        // 0.8 plays the whole curve in the first fifth of the life; ostankya's start of -0.3 stretches it past the birth.
        [TestCase(0.1f, 0f, 0f, 0.8f, 0.8f, 0f, 0.5f)]
        [TestCase(0.5f, 0f, 0f, 0.8f, 0.8f, 0f, 1f)]
        [TestCase(0.5f, 0.3f, 0.3f, 0f, 0f, 0f, 0.2857f)]
        [TestCase(0.2f, 0.3f, 0.3f, 0f, 0f, 0f, 0f)]
        [TestCase(0.35f, -0.3f, -0.3f, 0f, 0f, 0f, 0.5f)]
        [TestCase(0.6f, 0f, 0.4f, 0f, 0f, 0.5f, 0.5f)]
        [TestCase(0.3f, 0f, 0f, 0.2f, 0.6f, 0.5f, 0.5f)]
        public void AlphaLife_SqueezesTheCurveIntoItsWindow(float life, float startMin, float startMax, float endMin, float endMax, float random, float expected) =>
            Assert.That(VfxConventions.AlphaLife(life, new Vector2(startMin, startMax), new Vector2(endMin, endMax), random), Is.EqualTo(expected).Within(1e-3));

        // ...on the particle: curve_alpha 1 to 0 with an end offset of 0.5 is gone by half its life.
        [Test]
        public void AlphaOffsets_ReachTheParticle()
        {
            var s = Steady(x => { x.MaxParticles = 1; x.ParticleLife = C(2); x.Alpha = Ramp(1, 0); x.AlphaEndOffset = new Vector2(0.5f, 0.5f); });
            var simulator = Run(s, 0.5f);
            var p = simulator.Emitters[0].Particles.Single();
            Assert.That(States(simulator).Single().Alpha, Is.EqualTo(Math.Max(0, 1 - p.Age / p.Life / 0.5f)).Within(1e-3));
        }

        [TestCase(0.5f, 0f, 1f, false, true, 32)]
        [TestCase(0.5f, 0f, 0.5f, false, true, 16)]
        [TestCase(0f, 0.25f, 2f, true, true, 32, Description = "fixed speed: loops per second")]
        [TestCase(0.75f, 0f, 2f, false, false, 63, Description = "no loop: holds the last cell")]
        [TestCase(0.75f, 0f, 2f, false, true, 32, Description = "loop: wraps")]
        public void FlipbookCell_OfSixtyFour(float lifeFraction, float age, float loops, bool fixedSpeed, bool loop, int expected) =>
            Assert.That(VfxConventions.FlipbookFrame(64, lifeFraction, age, loops, fixedSpeed, loop).Cell, Is.EqualTo(expected));

        // no_loops runs from its start to its end over the particle's life, as CA's lerps it by the life fraction, the same for
        // every particle: by life fraction u the strip has played u x lerp(start, end, u) times, or age x lerp(start, end, u) at a
        // fixed speed. 1 to 3 over a 2 s life, read off 15 particles of different ages.
        [TestCase(false)]
        [TestCase(true)]
        public void Flipbook_LoopsRunFromStartToEndOverLife(bool fixedSpeed)
        {
            var s = Steady(x => { x.ParticleLife = C(2); x.Animated = true; x.CellsX = 16; x.Loops = new Vector2(1, 3); x.FixedSpeed = fixedSpeed; });
            var emitter = Run(s, 1.5f).Emitters[0];
            var expected = emitter.Particles.Select(p => p.Age / p.Life).Select(v => (int)(Fraction((fixedSpeed ? v * 2 : v) * (1 + 2 * v)) * 16));
            Assert.That(ParticleSimulator.States(emitter).Select(x => x.Cell), Is.EqualTo(expected));
            static float Fraction(float x) => x - MathF.Floor(x);
        }

        // cell_blend: a quarter of the way through cell 2 of 4 is a quarter of the way to cell 3; the last cell runs into the
        // first when the strip loops and into itself when it holds.
        [TestCase(0.5625f, true, 2, 3, 0.25f)]
        [TestCase(0.875f, true, 3, 0, 0.5f)]
        [TestCase(0.875f, false, 3, 3, 0.5f)]
        public void FlipbookFrame_BlendsIntoTheNextCell(float lifeFraction, bool loop, int cell, int next, float blend)
        {
            var frame = VfxConventions.FlipbookFrame(4, lifeFraction, 0, 1, false, loop);
            Assert.That((frame.Cell, frame.Next), Is.EqualTo((cell, next)));
            Assert.That(frame.Blend, Is.EqualTo(blend).Within(1e-4));
        }

        // vs_animated_texture's rows on a 4 x 4 sheet, as CA's has them: row_restricted plays the 4 cells of row_restriction;
        // randomize_row starts on the particle's random row, 0.6 of 4 rows being row 2, and without row_restricted plays the
        // whole sheet from there, past the last cell round to the first.
        [TestCase(false, 0, false, 0.6f, 0.5f, 8, 9, Description = "the whole sheet")]
        [TestCase(true, 2, false, 0.6f, 0.5f, 10, 11, Description = "row 2 only")]
        [TestCase(true, 2, false, 0.6f, 0.875f, 11, 8, Description = "row 2 loops inside the row")]
        [TestCase(false, 0, true, 0.6f, 0.25f, 12, 13, Description = "from random row 2")]
        [TestCase(true, 1, true, 0.6f, 0.5f, 10, 11, Description = "the random row overrides row_restriction")]
        [TestCase(false, 0, true, 0.9f, 0.5f, 4, 5, Description = "from row 3, round past the last cell")]
        public void FlipbookFrame_PlaysItsRow(bool restricted, int row, bool randomRow, float random, float lifeFraction, int cell, int next)
        {
            var s = new EmitterSettings { Animated = true, CellsX = 4, CellsY = 4, RowRestricted = restricted, Row = row, RandomRow = randomRow };
            var frame = VfxConventions.FlipbookFrame(s, lifeFraction, 0, 1, random);
            Assert.That((frame.Cell, frame.Next), Is.EqualTo((cell, next)));
        }

        // randomize_cell on a flipbook starts each particle that far along its strip, as CA's offset is its random_unary, before
        // the strip wraps or holds: 0.5 of life and 0.3 is 0.8 of 16 cells; 0.75 and 0.5 wraps to 0.25 when looping and holds the
        // last cell when not; with randomize_row too the one random picks the row and the start in it (0.6: row 2, 2.4 of 4).
        // Two loops play the strip twice as fast from the same start: 0.25 of life is 0.5 of the strip, and 0.3 on is 0.8 again.
        [TestCase(true, false, 0.5f, 1f, 0.3f, 12, 13, 0.8f)]
        [TestCase(true, false, 0.75f, 1f, 0.5f, 4, 5, 0f)]
        [TestCase(false, false, 0.75f, 1f, 0.5f, 15, 15, 1f)]
        [TestCase(true, true, 0f, 1f, 0.6f, 10, 11, 0.4f)]
        [TestCase(true, false, 0.25f, 2f, 0.3f, 12, 13, 0.8f)]
        public void FlipbookFrame_StartsAtTheParticlesRandomCell(bool loop, bool randomRow, float lifeFraction, float loops, float random, int cell, int next, float blend)
        {
            var s = new EmitterSettings { Animated = true, CellsX = 4, CellsY = 4, RandomCell = true, FlipbookLoop = loop, RowRestricted = randomRow, RandomRow = randomRow };
            var frame = VfxConventions.FlipbookFrame(s, lifeFraction, 0, loops, random);
            Assert.That((frame.Cell, frame.Next), Is.EqualTo((cell, next)));
            Assert.That(frame.Blend, Is.EqualTo(blend).Within(2e-3));
        }

        // ...and each particle draws its own row: one cell per row, one row each, so the cells are the rows.
        [Test]
        public void FlipbookRows_ARandomRowPerParticle()
        {
            var s = Steady(x => { x.MaxParticles = 40; x.Animated = true; x.CellsX = 1; x.CellsY = 8; x.RowRestricted = true; x.RandomRow = true; });
            Assert.That(States(Run(s, 2)).Select(x => x.Cell).Distinct().Count(), Is.GreaterThan(4));
        }
    }
}
