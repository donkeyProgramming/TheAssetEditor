using System.Diagnostics;
using System.Globalization;
using GameWorld.Core.Components.Rendering;
using GameWorld.Core.Rendering;
using GameWorld.Core.Vfx;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Test.TestingUtility.Shared;

namespace GameWorld.Core.Test.Vfx
{
    // What a frame of effects costs, split as the app spends it: stepping the simulators on, the clip's loop (a jump back,
    // which starts every effect over), and drawing them in the main and the glow pass. Needs WH3_DATA.
    //   VFX_PREVIEW   effect names, comma-separated, each run as many times as it is named (default: a Bale Taurus's)
    //   VFX_FRAMES    frames timed (default 600, ten seconds)
    //   VFX_LOOP      the clip's length in seconds, where it loops (default 2.2)
    public class VfxFrameCost
    {
        static string Env(string name, string fallback) => Environment.GetEnvironmentVariable(name) is { Length: > 0 } value ? value : fallback;

        [Test, Explicit, Apartment(ApartmentState.STA)]
        public void TimeAFrame()
        {
            var (runner, _) = VanillaPacks.Load("vfx_desc.pack", "vfx.pack");
            const string Legs = "wh3_dlc23_entity_bale_taurus_leg_fire";
            const string Wings = "wh3_dlc23_entity_bale_taurus_wing_fire";
            var names = Env("VFX_PREVIEW", string.Join(",", Enumerable.Repeat(Legs, 4).Concat(Enumerable.Repeat(Wings, 4))))
                .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            var frames = int.Parse(Env("VFX_FRAMES", "600"), CultureInfo.InvariantCulture);
            var loop = float.Parse(Env("VFX_LOOP", "2.2"), CultureInfo.InvariantCulture);

            var service = new GraphicsDeviceServiceMock();
            var device = service.GraphicsDevice;
            var log = new List<string>();
            using var content = ParticleRenderItemTests.Content(service);
            var shader = content.Load<Effect>("Shaders/Particle");
            var textures = new VfxTextureCache(ParticleRenderItem.PackTextureLoader(runner.PackFileService, log.Add));
            using var target = new RenderTarget2D(device, 1280, 720, false, SurfaceFormat.Color, DepthFormat.Depth24, 0, RenderTargetUsage.PreserveContents);
            var eye = new Vector3(6, 4, 6);
            var parameters = new CommonShaderParameters(Matrix.CreateLookAt(eye, new Vector3(0, 1, 0), Vector3.Up),
                Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4, 16 / 9f, 0.1f, 500), eye, new Vector3(0, 1, 0), 0, 0, 0, 1, [], Vector3.One);

            var resolver = new VfxResolver(runner.PackFileService);
            var simulators = names.Select((name, i) => new ParticleSimulator(resolver.Resolve(name, new Random(i)).Emitters.Select(EmitterSettings.From).ToList(), i,
                t => Matrix.CreateTranslation(i % 4 - 1.5f, 1, i / 4 - 0.5f), log: log.Add)).ToList();
            var items = simulators.Select(x => new ParticleRenderItem(x, textures, () => shader)).ToList();

            var step = new Stopwatch();
            var draw = new Stopwatch();
            var resets = new List<double>();
            var worst = 0.0;
            var particles = 0;
            var lights = 0;
            var pixel = new Color[1];
            for (var f = 0; f < frames; f++)
            {
                var time = f / 60f % loop;
                var looped = f > 0 && time < (f - 1) / 60f % loop;
                var started = step.Elapsed.TotalMilliseconds;
                step.Start();
                foreach (var simulator in simulators)
                    simulator.AdvanceTo(time);
                step.Stop();
                var spent = step.Elapsed.TotalMilliseconds - started;
                if (looped)
                    resets.Add(spent);
                else
                    worst = Math.Max(worst, spent);

                draw.Start();
                device.SetRenderTarget(target);
                device.Clear(Color.Transparent);
                device.DepthStencilState = DepthStencilState.DepthRead;
                device.RasterizerState = RasterizerState.CullNone;
                // The frame's lights, as the engine hands them every draw, and the ground light it draws once from them.
                var scene = parameters with { PointLights = items.SelectMany(x => x.Lights()).Take(CommonShaderParameters.MaxPointLights).ToList() };
                lights = scene.PointLights.Count;
                ParticleRenderItem.DrawGroundLight(device, shader, scene);
                foreach (var item in items)
                    item.Draw(device, scene, RenderingTechnique.Normal);
                foreach (var item in items)
                    item.Draw(device, scene, RenderingTechnique.Emissive);
                device.SetRenderTarget(null);
                // Waits for the GPU, so the draw time holds its work and not only the calls that queue it.
                target.GetData(0, new Rectangle(0, 0, 1, 1), pixel, 0, 1);
                draw.Stop();
                particles = simulators.Sum(x => x.AllEmitters.Sum(e => e.Particles.Count));
            }

            var steady = frames - resets.Count;
            TestContext.Out.WriteLine($"{names.Length} effects, {particles} particles and {lights} lights at the end");
            TestContext.Out.WriteLine($"step: {(step.Elapsed.TotalMilliseconds - resets.Sum()) / steady:0.000} ms a frame, worst {worst:0.000}");
            TestContext.Out.WriteLine($"loop: {resets.Count} resets, {resets.DefaultIfEmpty().Average():0.000} ms each, worst {resets.DefaultIfEmpty().Max():0.000}");
            TestContext.Out.WriteLine($"draw, main and glow: {draw.Elapsed.TotalMilliseconds / frames:0.000} ms a frame");
            foreach (var line in log.Distinct().Take(20))
                TestContext.Out.WriteLine(line);
            foreach (var item in items)
                item.Dispose();
            textures.Dispose();
            service.Release();
        }
    }
}
