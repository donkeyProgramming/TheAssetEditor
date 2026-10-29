using System.Globalization;
using System.IO;
using Editors.VfxEditor.Preview;
using Editors.VfxEditor.ViewModels;
using GameWorld.Core.Components.Rendering;
using GameWorld.Core.WpfWindow;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Shared.Core.Commands;
using Shared.Core.PackFiles.Models;
using Shared.Core.PackFiles.Utility;
using Shared.Core.ToolCreation;
using Test.TestingUtility.Shared;

namespace Test.VfxEditor
{
    // Renders effects the way the app shows them: opened in the VFX editor, one frame of every component of its tab run
    // (the grid, the scene with rigid models, the render engine with its glow and its composite over the background), to
    // PNGs. VfxPreviewRender in GameWorld.CoreTest draws the particles alone. Needs WH3_DATA.
    //   VFX_PREVIEW      effect names, comma-separated
    //   VFX_TIMES        seconds after the effect starts (default 0.25,0.5,1,2)
    //   VFX_PREVIEW_OUT  output folder
    //   VFX_DISTANCE     camera distance in metres (default 10)
    //   VFX_PACKS        packs loaded from WH3_DATA (default vfx_desc, vfx, vfx_models and models2)
    public class VfxAppRender
    {
        static string Env(string name, string fallback) => Environment.GetEnvironmentVariable(name) is { Length: > 0 } value ? value : fallback;

        [Test, Explicit, Apartment(ApartmentState.STA)]
        public void RenderEffects()
        {
            var data = Environment.GetEnvironmentVariable("WH3_DATA");
            Assume.That(data != null && Directory.Exists(data), "set WH3_DATA");
            var names = Env("VFX_PREVIEW", "wh2_dlc11_lib_tidecall_breath").Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            var times = Env("VFX_TIMES", "0.25,0.5,1,2").Split(',').Select(x => float.Parse(x, CultureInfo.InvariantCulture)).ToList();
            var output = Env("VFX_PREVIEW_OUT", Path.Combine(TestContext.CurrentContext.WorkDirectory, "vfx_app_render"));
            var distance = float.Parse(Env("VFX_DISTANCE", "10"), CultureInfo.InvariantCulture);
            var packs = Env("VFX_PACKS", "vfx_desc.pack,vfx.pack,vfx_models.pack,models2.pack").Split(',', StringSplitOptions.TrimEntries);
            Directory.CreateDirectory(output);

            var lookAt = new Vector3(0, 1, 0);
            var views = new (string Name, Vector3 Eye, Vector3 Up)[]
            {
                ("front", new Vector3(0, 1, distance), Vector3.Up),
                ("side", new Vector3(distance, 1, 0), Vector3.Up),
                ("top", new Vector3(0, distance + 1, 0), Vector3.Forward),
                ("angle", new Vector3(distance * 0.68f, distance * 0.33f + 1, distance * 0.68f), Vector3.Up),
            };

            foreach (var name in names)
            {
                var runner = new AssetEditorTestRunner();
                runner.CreateCaContainer();
                var loader = runner.ServiceProvider.GetRequiredService<IPackFileContainerLoader>();
                foreach (var pack in packs)
                    runner.PackFileService.AddContainer(loader.CreateFromPackFile(PackFileContainerType.Normal, Path.Combine(data!, pack), true));
                var editor = (VfxEditorViewModel)runner.CommandFactory.Create<OpenEditorCommand>().Execute($"vfx\\{name}.xml", EditorEnums.Vfx_Editor);
                var preview = runner.GetRequiredServiceInCurrentEditorScope<VfxPreviewComponent>();
                var camera = runner.GetRequiredServiceInCurrentEditorScope<ArcBallCamera>();
                var components = runner.GetRequiredServiceInCurrentEditorScope<IEnumerable<IGameComponent>>().ToList();
                var device = runner.ServiceProvider.GetRequiredService<IWpfGame>().GraphicsDevice;
                preview.IsPlaying = false;
                using var target = new RenderTarget2D(device, 768, 512, false, SurfaceFormat.Color, DepthFormat.Depth24, 0, RenderTargetUsage.PreserveContents);

                foreach (var time in times)
                {
                    preview.SetTime(time);
                    // The app's own camera last, looking down at this distance: the grid's spacing follows its zoom.
                    foreach (var (view, eye, up) in views.Append(("arcball", Vector3.Zero, Vector3.Zero)))
                    {
                        camera.ViewMatrixOverride = view == "arcball" ? null : Matrix.CreateLookAt(eye, lookAt, up);
                        (camera.Zoom, camera.LookAt, camera.Pitch, camera.Yaw) = (distance, lookAt, -0.32f, 0.8f);
                        device.SetRenderTarget(target);
                        Frame(components);
                        device.SetRenderTarget(null);
                        using var stream = File.Create(Path.Combine(output, $"{name}_{view}_{time.ToString("0.00", CultureInfo.InvariantCulture)}s.png"));
                        target.SaveAsPng(stream, target.Width, target.Height);
                    }
                }
                TestContext.Out.WriteLine($"{name}:");
                foreach (var line in preview.Lines)
                    TestContext.Out.WriteLine("  " + line);
            }
        }

        // One frame as the game runs it: every component's Update in update order, then every Draw in draw order. The
        // render engine's Update clears last frame's items and its Draw comes after the scene and the grid add theirs.
        static void Frame(List<IGameComponent> components)
        {
            var time = new GameTime(TimeSpan.Zero, TimeSpan.Zero);
            foreach (var component in components.OfType<IUpdateable>().OrderBy(x => x.UpdateOrder))
                component.Update(time);
            foreach (var component in components.OfType<IDrawable>().OrderBy(x => x.DrawOrder))
                component.Draw(time);
        }
    }
}
