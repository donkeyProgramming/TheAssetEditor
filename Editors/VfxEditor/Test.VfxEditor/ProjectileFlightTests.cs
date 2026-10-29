using Editors.VfxEditor.Preview;
using GameWorld.Core.Vfx;
using Microsoft.Xna.Framework;
using Shared.GameFormats.Vfx;

namespace Test.VfxEditor
{
    class ProjectileFlightTests
    {
        [Test]
        public void The_flight_crosses_the_origin_halfway_through()
        {
            var flight = ProjectileFlight.WorldAt(Vector3.UnitZ, 60, 1);
            Assert.That(new[] { 0f, 0.5f, 1f }.Select(t => flight(t).Translation),
                Is.EqualTo(new[] { new Vector3(0, 0, -30), Vector3.Zero, new Vector3(0, 0, 30) }));
        }

        // Ten still particles a second for one second of a 60 m/s flight: world space leaves them from x = -54 to 0 (the
        // trail), standing still they sit at the origin, and local space carries them along.
        [TestCase(true, SimulationSpace.World, 54f)]
        [TestCase(false, SimulationSpace.World, 0f)]
        [TestCase(true, SimulationSpace.LocalPositionOnly, 0f)]
        public void A_world_space_emitter_leaves_its_particles_along_the_flight(bool flying, SimulationSpace space, float spread)
        {
            var settings = new EmitterSettings { Name = "trail", InfiniteLife = true, Rate = VfxCurve.Constant(10), ParticleLife = VfxCurve.Constant(100), Spread = (0, 0), Space = space };
            var simulator = new ParticleSimulator([settings], 1, flying ? ProjectileFlight.WorldAt(Vector3.UnitX, 60, 2) : _ => Matrix.Identity);
            simulator.AdvanceTo(1);
            var xs = simulator.Emitters.SelectMany(ParticleSimulator.States).Select(p => p.Position.X).ToList();
            Assert.That(xs, Has.Count.GreaterThan(5));
            Assert.That(xs.Max() - xs.Min(), Is.EqualTo(spread).Within(2));
        }
    }
}
