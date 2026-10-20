using System.Globalization;
using System.IO;
using GameWorld.Core.Components.Rendering;
using GameWorld.Core.Rendering;
using GameWorld.Core.Vfx;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Test.TestingUtility.Shared;

namespace GameWorld.Core.Test.Vfx
{
    // The calibration tool: renders effects alone at the origin over a 1 m grid, from the
    // front, side and top, at the given times, to PNGs to set beside captures from CA's editor. Needs WH3_DATA.
    //   VFX_PREVIEW      effect names, comma-separated (default: the steam engine firebox)
    //   VFX_TIMES        seconds after the effect starts (default 0.25,0.5,1,2)
    //   VFX_PREVIEW_OUT  output folder
    //   VFX_DISTANCE     camera distance in metres (default 6)
    //   VFX_FLIGHT       metres a second the effect flies along +X, 1 m up, passing x = 0 at each time (default 0)
    //   VFX_LOOSE        a folder of loose vfx *.xml read in place of the packs' copies, so a mod's effect draws
    public class VfxPreviewRender
    {
        static string Env(string name, string fallback) => Environment.GetEnvironmentVariable(name) is { Length: > 0 } value ? value : fallback;

        [Test, Explicit, Apartment(ApartmentState.STA)]
        public void RenderEffects()
        {
            var (runner, _) = VanillaPacks.Load("vfx_desc.pack", "vfx.pack");
            var names = Env("VFX_PREVIEW", "wh3_dlc23_warmachine_steam_engine_firebox").Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            var times = Env("VFX_TIMES", "0.25,0.5,1,2").Split(',').Select(x => float.Parse(x, CultureInfo.InvariantCulture)).ToList();
            var output = Env("VFX_PREVIEW_OUT", Path.Combine(TestContext.CurrentContext.WorkDirectory, "vfx_preview"));
            var distance = float.Parse(Env("VFX_DISTANCE", "6"), CultureInfo.InvariantCulture);
            var flight = float.Parse(Env("VFX_FLIGHT", "0"), CultureInfo.InvariantCulture);
            var loose = Env("VFX_LOOSE", "") is { Length: > 0 } folder
                ? Directory.GetFiles(folder, "*.xml").ToDictionary(x => VfxResolver.Key(Path.GetFileNameWithoutExtension(x)), File.ReadAllBytes)
                : [];
            Directory.CreateDirectory(output);

            var service = new GraphicsDeviceServiceMock();
            var device = service.GraphicsDevice;
            var log = new List<string>();
            using var content = ParticleRenderItemTests.Content(service);
            var shader = content.Load<Effect>("Shaders/Particle");
            var textures = new VfxTextureCache(ParticleRenderItem.PackTextureLoader(runner.PackFileService, log.Add));
            using var target = new RenderTarget2D(device, 512, 512, false, SurfaceFormat.Color, DepthFormat.Depth24, 0, RenderTargetUsage.PreserveContents);
            using var picture = new RenderTarget2D(device, 512, 512);
            using var batch = new SpriteBatch(device);
            using var lines = new BasicEffect(device) { VertexColorEnabled = true };
            var lookAt = new Vector3(0, 1, 0);
            var views = new (string Name, Vector3 Eye, Vector3 Up)[]
            {
                ("front", new Vector3(0, 1, distance), Vector3.Up),
                ("side", new Vector3(distance, 1, 0), Vector3.Up),
                ("top", new Vector3(0, distance + 1, 0), Vector3.Forward),
            };

            foreach (var name in names)
            {
                var resolved = new VfxResolver(runner.PackFileService, loose).Resolve(name, new Random(1));
                Assert.That(resolved.Found, $"{name} is not in vfx_desc.pack");
                var settings = resolved.Emitters.Select(EmitterSettings.From).ToList();
                foreach (var time in times)
                {
                    var simulator = new ParticleSimulator(settings, 1, t => Matrix.CreateTranslation(flight * (t - time), flight == 0 ? 0 : 1, 0), log: log.Add);
                    simulator.AdvanceTo(time);
                    using var item = new ParticleRenderItem(simulator, textures, () => shader);
                    foreach (var (view, eye, up) in views)
                    {
                        var parameters = new CommonShaderParameters(Matrix.CreateLookAt(eye, lookAt, up),
                            Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4, 1, 0.1f, 500), eye, lookAt, 0, 0, 0, 1, [], Vector3.One) { PointLights = item.Lights().ToList() };
                        device.SetRenderTarget(target);
                        device.Clear(Color.Transparent);
                        device.BlendState = BlendState.Opaque;
                        device.DepthStencilState = DepthStencilState.Default;
                        DrawGrid(device, lines, parameters);
                        device.DepthStencilState = DepthStencilState.DepthRead;
                        device.RasterizerState = RasterizerState.CullNone;
                        ParticleRenderItem.DrawGroundLight(device, shader, parameters);
                        item.Draw(device, parameters, RenderingTechnique.Normal);
                        // Laid over the background by its alpha, as the app lays its scene.
                        device.SetRenderTarget(picture);
                        device.Clear(new Color(40, 40, 40));
                        batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
                        batch.Draw(target, Vector2.Zero, Color.White);
                        batch.End();
                        device.SetRenderTarget(null);
                        using var stream = File.Create(Path.Combine(output, $"{name}_{view}_{time.ToString("0.00", CultureInfo.InvariantCulture)}s.png"));
                        picture.SaveAsPng(stream, picture.Width, picture.Height);
                    }
                }
                TestContext.Out.WriteLine($"{name}: {settings.Count(x => x.Draws)} of {settings.Count} emitters drawn");
                foreach (var problem in resolved.Problems)
                    TestContext.Out.WriteLine("  " + problem);
                foreach (var gap in settings.SelectMany(x => x.Skipped.Select(g => $"  {x.Name}: {g}")).Distinct())
                    TestContext.Out.WriteLine(gap);
            }
            foreach (var line in log.Distinct())
                TestContext.Out.WriteLine(line);
            textures.Dispose();
            service.Release();
        }

        // A 1 m grid on the ground, 10 m across; the X axis red, the Z axis blue.
        static void DrawGrid(GraphicsDevice device, BasicEffect effect, CommonShaderParameters parameters)
        {
            var vertices = new List<VertexPositionColor>();
            for (var i = -5; i <= 5; i++)
            {
                var alongX = i == 0 ? Color.Red : Color.Gray;
                var alongZ = i == 0 ? Color.Blue : Color.Gray;
                vertices.Add(new VertexPositionColor(new Vector3(-5, 0, i), alongX));
                vertices.Add(new VertexPositionColor(new Vector3(5, 0, i), alongX));
                vertices.Add(new VertexPositionColor(new Vector3(i, 0, -5), alongZ));
                vertices.Add(new VertexPositionColor(new Vector3(i, 0, 5), alongZ));
            }
            effect.World = Matrix.Identity;
            effect.View = parameters.View;
            effect.Projection = parameters.Projection;
            foreach (var pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                device.DrawUserPrimitives(PrimitiveType.LineList, vertices.ToArray(), 0, vertices.Count / 2);
            }
        }
    }
}
