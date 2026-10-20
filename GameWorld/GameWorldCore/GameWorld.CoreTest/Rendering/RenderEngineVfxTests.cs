using System.IO;
using GameWorld.Core.Components.Rendering;
using GameWorld.Core.Rendering;
using GameWorld.Core.Services;
using GameWorld.Core.Vfx;
using GameWorld.Core.WpfWindow;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Shared.GameFormats.Vfx;
using Test.TestingUtility.Shared;

namespace GameWorld.Core.Test.Rendering
{
    // What the render engine hands the VFX preview's particles each frame, and what a screenshot leaves out.
    [Apartment(ApartmentState.STA)]
    public class RenderEngineVfxTests
    {
        AssetEditorTestRunner _runner;
        IServiceScope _scope;
        RenderEngineComponent _engine;
        GraphicsDevice _device;

        [SetUp]
        public void Setup()
        {
            _runner = new AssetEditorTestRunner();
            _scope = _runner.ServiceProvider.CreateScope();
            var game = _scope.ServiceProvider.GetRequiredService<IWpfGame>();
            game.AddComponent(_scope.ServiceProvider.GetRequiredService<ArcBallCamera>());
            _engine = game.AddComponent(_scope.ServiceProvider.GetRequiredService<RenderEngineComponent>());
            _device = game.GraphicsDevice;
        }

        [TearDown]
        public void TearDown() => _scope.Dispose();

        // One frame onto a 64 x 64 target, of the items `add` puts in the engine's buckets.
        void Frame(Action<RenderEngineComponent> add)
        {
            using var target = new RenderTarget2D(_device, 64, 64, false, SurfaceFormat.Color, DepthFormat.Depth24);
            _device.SetRenderTarget(target);
            add(_engine);
            _engine.Draw(new GameTime());
            _engine.Update(new GameTime());
            _device.SetRenderTarget(null);
        }

        // Soft particles fade against the solid scene, so a frame with particles first draws the scene's depth for them.
        [Test]
        public void A_frame_with_particles_hands_them_the_scenes_depth()
        {
            var (particles, solid) = (new List<CommonShaderParameters>(), new List<CommonShaderParameters>());
            Frame(e => e.AddRenderItem(RenderBuckedId.Transparent, new Witness(particles)));
            Frame(e => e.AddRenderItem(RenderBuckedId.Normal, new Witness(solid)));
            Assert.That(particles.Last().SceneDepth, Is.Not.Null);
            Assert.That(solid.Last().SceneDepth, Is.Null, "a frame with no particles draws no depth");
        }

        // The solid scene is handed every light particle alive, not only the first.
        [Test]
        public void The_solid_scene_takes_every_light_particle()
        {
            var lights = new EmitterSettings
            {
                Name = "light", InfiniteLife = true, Rate = VfxCurve.Constant(60), MaxParticles = 3, ParticleLife = VfxCurve.Constant(100), Light = true, Decal = true,
            };
            var simulator = new ParticleSimulator([lights], 1, _ => Matrix.Identity);
            simulator.AdvanceTo(0.5f);
            using var textures = new VfxTextureCache((_, _) => null);
            var shader = _runner.ServiceProvider.GetRequiredService<ResourceLibrary>();
            using var item = new ParticleRenderItem(simulator, textures, () => shader.GetStaticEffect(ShaderTypes.Particle));
            var seen = new List<CommonShaderParameters>();
            Frame(e =>
            {
                e.AddRenderItem(RenderBuckedId.Transparent, item);
                e.AddRenderItem(RenderBuckedId.Normal, new Witness(seen));
            });
            Assert.That(seen.Last().PointLights, Has.Count.EqualTo(3));
        }

        // A screenshot without lines leaves the grid out and keeps the wireframe, as PhotoStudio's always has; one without
        // overlays too, as the VFX editor takes, leaves both out. The next frame draws both again.
        [TestCase(true)]
        [TestCase(false)]
        public void A_screenshot_without_lines_draws_no_grid(bool overlays)
        {
            var folder = Path.Combine(TestContext.CurrentContext.WorkDirectory, "screenshot_guides");
            var (grid, wireframe) = (new List<RenderingTechnique>(), new List<RenderingTechnique>());
            void Add(RenderEngineComponent e)
            {
                e.AddRenderItem(RenderBuckedId.Grid, new Recorder(grid));
                e.AddRenderItem(RenderBuckedId.Wireframe, new Recorder(wireframe));
            }
            try
            {
                _engine.SaveNextFrame(new SaveRenderImageSettings("frame", false, DrawLines: false, 1, folder, DrawOverlays: overlays));
                Frame(Add);
                var inScreenshot = (grid.Count, wireframe.Count);
                Frame(Add);
                var plain = (grid.Count - inScreenshot.Item1, wireframe.Count - inScreenshot.Item2);
                Assert.That(plain.Item1 > 0 && plain.Item2 > 0, $"a plain frame draws both: {plain}");
                Assert.That(inScreenshot, Is.EqualTo((0, overlays ? plain.Item2 : 0)));
            }
            finally
            {
                if (Directory.Exists(folder))
                    Directory.Delete(folder, true);
            }
        }

        sealed class Witness(List<CommonShaderParameters> seen) : IRenderItem
        {
            public void Draw(GraphicsDevice device, CommonShaderParameters parameters, RenderingTechnique technique) => seen.Add(parameters);
        }

        sealed class Recorder(List<RenderingTechnique> drawn) : IRenderItem
        {
            public void Draw(GraphicsDevice device, CommonShaderParameters parameters, RenderingTechnique technique) => drawn.Add(technique);
        }
    }
}
