using GameWorld.Core.Components.Rendering;
using GameWorld.Core.Rendering;
using GameWorld.Core.SceneNodes;
using GameWorld.Core.Vfx;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Shared.GameFormats.Vfx;
using Test.TestingUtility.Shared;

namespace GameWorld.Core.Test.Vfx
{
    [Apartment(ApartmentState.STA)]
    public class ParticleRenderItemTests
    {
        GraphicsDeviceServiceMock _service;
        GraphicsDevice _device;
        Texture2D _white;
        ContentManager _content;
        // The content manager owns it, and caches it.
        Effect Shader => _content.Load<Effect>("Shaders/Particle");

        // Looking down -Z at the origin from 5 m; a 10 m quad there fills the view.
        static readonly CommonShaderParameters View = new(
            Matrix.CreateLookAt(new Vector3(0, 0, 5), Vector3.Zero, Vector3.Up),
            Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4, 1, 0.1f, 100),
            new Vector3(0, 0, 5), Vector3.Zero, 0, 0, 0, 1, [], Vector3.One);

        static readonly ParticleState RedHalfAlpha = new(Vector3.Zero, Vector3.Zero, new Vector2(10, 10), 0, new Vector3(1, 0, 0), 0.5f, 0, 0);

        [OneTimeSetUp]
        public void Setup()
        {
            _service = new GraphicsDeviceServiceMock();
            _device = _service.GraphicsDevice;
            _white = new Texture2D(_device, 1, 1);
            _white.SetData(new[] { Color.White });
            _content = Content(_service);
        }

        // Shaders/Particle comes out of this test's own copy of the content.
        internal static ContentManager Content(IGraphicsDeviceService service)
        {
            var services = new GameServiceContainer();
            services.AddService(service);
            return new ContentManager(services, Path.Combine(TestContext.CurrentContext.TestDirectory, "Content"));
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            _white.Dispose();
            _content.Dispose();
            _service.Release();
        }

        // Draws onto mid grey, or the background given, and returns one pixel from the middle.
        Vector4 Render(Action draw, Color? background = null)
        {
            // Preserved as the app's are: a distortion copies the target and draws on, and a discarded one comes back cleared.
            using var target = new RenderTarget2D(_device, 4, 4, false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
            _device.SetRenderTarget(target);
            _device.Clear(background ?? new Color(128, 128, 128, 255));
            _device.DepthStencilState = DepthStencilState.None;
            _device.RasterizerState = RasterizerState.CullNone;
            draw();
            _device.SetRenderTarget(null);
            var pixels = new Color[16];
            target.GetData(pixels);
            return pixels[5].ToVector4();
        }

        [TestCase(true, 0f, 0.751f, 0.251f, Description = "alpha: red at half over grey")]
        [TestCase(true, 1f, 1.0f, 0.502f, Description = "additive: red added to grey")]
        [TestCase(true, 0.5f, 0.876f, 0.376f, Description = "half additive")]
        [TestCase(false, 0f, 0.751f, 0.251f, Description = "straight-alpha texture, alpha blend")]
        [TestCase(false, 1f, 1.0f, 0.502f, Description = "straight-alpha texture, additive")]
        public void Blend_OverAKnownBackground(bool premultiplied, float additiveMul, float red, float greenAndBlue)
        {
            var settings = new EmitterSettings { Premultiplied = premultiplied, AdditiveMul = additiveMul };
            var effect = Shader;
            var camera = ParticleRenderItem.CameraAxes.From(View, Matrix.Identity);
            var pixel = Render(() => ParticleRenderItem.DrawEmitter(_device, effect, View, Matrix.Identity, camera, settings, _white, [RedHalfAlpha]));
            Assert.Multiple(() =>
            {
                Assert.That(pixel.X, Is.EqualTo(red).Within(0.02));
                Assert.That(pixel.Y, Is.EqualTo(greenAndBlue).Within(0.02));
                Assert.That(pixel.Z, Is.EqualTo(greenAndBlue).Within(0.02));
            });
        }

        // ps_texture's overlay_colour: CA's process_texture_colour overlays the colour on the texture instead of multiplying it in,
        // and the alpha still multiplies. A mid-grey texel (0.216 linear, under the overlay's half) under a (1, 0.5, 0) colour:
        // 2 x 0.216 x 1 = 0.432 red, 0.216 green, none blue, back to the screen's gamma (0.690, 0.502, 0). The same blend in
        // gamma, or with the colour as the base, gives full red; a multiply gives (0.502, 0.251, 0). At half alpha over grey
        // the premultiplied colour is that half covering the background's half. An additive_blend_mul of 2 doubles the overlaid
        // colour after the blend and hides none of the background: 2 x 0.25 x (0.690, 0.502, 0) over all of the grey.
        [TestCase(true, 1f, 0f, 0.690f, 0.502f, 0f)]
        [TestCase(false, 1f, 0f, 0.502f, 0.251f, 0f)]
        [TestCase(true, 0.5f, 0f, 0.596f, 0.502f, 0.251f)]
        [TestCase(true, 0.25f, 2f, 0.847f, 0.753f, 0.502f)]
        public void OverlayColour_OverlaysTheColourOnTheTexture(bool overlay, float alpha, float additive, float red, float green, float blue)
        {
            using var grey = new Texture2D(_device, 1, 1);
            grey.SetData(new[] { new Color(128, 128, 128, 255) });
            var particle = RedHalfAlpha with { Colour = new Vector3(1, 0.5f, 0), Alpha = alpha };
            var camera = ParticleRenderItem.CameraAxes.From(View, Matrix.Identity);
            var pixel = Render(() => ParticleRenderItem.DrawEmitter(_device, Shader, View, Matrix.Identity, camera,
                new EmitterSettings { Overlay = overlay, AdditiveMul = additive }, grey, [particle]));
            Assert.Multiple(() =>
            {
                Assert.That(pixel.X, Is.EqualTo(red).Within(0.02));
                Assert.That(pixel.Y, Is.EqualTo(green).Within(0.02));
                Assert.That(pixel.Z, Is.EqualTo(blue).Within(0.02));
            });
        }

        // An overlay chain feeds the glow pass as any chain does, by what it draws (GlowPass_IsCAsBrightPass), and what it draws is
        // its overlaid colour held to 1, as CA holds it to the texel's alpha, times its additive_blend_mul. So no tint makes it
        // glow, not even (40, 0, 0) over a mid-grey texel, which overlays to 17; a white core in an orange tint glows white at
        // additive_blend_mul 8 (luminance 8), not at 0, and lit, not at all.
        [TestCase(255, 1f, 0.5f, 0f, false, false)]
        [TestCase(255, 1f, 0.5f, 0f, true, false)]
        [TestCase(128, 40f, 0f, 0f, false, false)]
        [TestCase(255, 1f, 0.5f, 8f, false, true)]
        public void OverlayColour_GlowsOnlyByItsBrightness(int texel, float red, float green, float additive, bool lit, bool glows)
        {
            using var texture = new Texture2D(_device, 1, 1);
            texture.SetData(new[] { new Color(texel, texel, texel, 255) });
            var camera = ParticleRenderItem.CameraAxes.From(View, Matrix.Identity);
            var core = RedHalfAlpha with { Colour = new Vector3(red, green, 0), Alpha = 1 };
            var settings = new EmitterSettings { Overlay = true, AdditiveMul = additive, Lighting = lit ? new ParticleLighting(0.8f, 0, 0.4f, 0, 0.8f) : null };
            var pixel = Render(() => ParticleRenderItem.DrawEmitter(_device, Shader, View, Matrix.Identity, camera, settings, texture, [core], glow: true), Color.Black);
            Assert.That(new[] { pixel.X, pixel.Y, pixel.Z }, Is.All.EqualTo(glows ? 1 : 0).Within(0.02));
        }

        // 122 of vanilla's 448 overlay chains are lit: the overlaid colour is the albedo, under the sky's 0.261 from a white ambient
        // cube. The grey texel decodes to 0.216; overlaid with (1, 0.5, 0) that is (0.432, 0.216, 0).
        [Test]
        public void OverlayColour_IsTheAlbedoOfALitChain()
        {
            using var sky = new TextureCube(_device, 1, false, SurfaceFormat.Color);
            foreach (var face in Enum.GetValues<CubeMapFace>())
                sky.SetData(face, new[] { Color.White });
            using var grey = new Texture2D(_device, 1, 1);
            grey.SetData(new[] { new Color(128, 128, 128, 255) });
            var effect = Shader;
            var camera = ParticleRenderItem.CameraAxes.From(View, Matrix.Identity);
            var quad = new ParticleState(Vector3.Zero, Vector3.Zero, new Vector2(10, 10), 0, new Vector3(1, 0.5f, 0), 1, 0, 0);
            effect.Parameters["AmbientCube"].SetValue(sky);
            var pixel = Render(() => ParticleRenderItem.DrawEmitter(_device, effect, View, Matrix.Identity, camera,
                new EmitterSettings { Lighting = new ParticleLighting(0, 0, 0.4f, 0, 0.6f), Overlay = true }, grey, [quad]));
            effect.Parameters["AmbientCube"].SetValue((TextureCube?)null);
            var texel = MathF.Pow((128 / 255f + 0.055f) / 1.055f, 2.4f);
            Assert.Multiple(() =>
            {
                Assert.That(pixel.X, Is.EqualTo(Encode(Uncharted2(2 * texel * 0.261f))).Within(0.01));
                Assert.That(pixel.Y, Is.EqualTo(Encode(Uncharted2(texel * 0.261f))).Within(0.01));
            });
        }

        // The app draws its scene onto nothing and lays it over the background by its alpha, so over nothing a particle's
        // alpha is what it hides of the background: half for one laid over at half, none for light it adds.
        [TestCase(false, 0f, 0.5f, Description = "straight-alpha texture, alpha blend")]
        [TestCase(false, 1f, 0f, Description = "straight-alpha texture, additive")]
        [TestCase(true, 0f, 0.5f, Description = "premultiplied, alpha blend")]
        [TestCase(true, 1f, 0f, Description = "premultiplied, additive")]
        public void OverNothing_AParticleHidesWhatItCovers(bool premultiplied, float additiveMul, float alpha)
        {
            var settings = new EmitterSettings { Premultiplied = premultiplied, AdditiveMul = additiveMul };
            var camera = ParticleRenderItem.CameraAxes.From(View, Matrix.Identity);
            var pixel = Render(() => ParticleRenderItem.DrawEmitter(_device, Shader, View, Matrix.Identity, camera, settings, _white, [RedHalfAlpha]), Color.Transparent);
            Assert.That(pixel.W, Is.EqualTo(alpha).Within(0.02));
        }

        // ps_texture reads a texture's alpha as sRGB, as the hardware reads its colour off a BC7_UNORM_SRGB file: CA's ps_main
        // linearises it before anything else uses it. A white texel at alpha 128 covers 21.6 % of black, not half.
        [TestCase(true)]
        [TestCase(false)]
        public void TextureAlpha_IsReadAsSrgb(bool premultiplied)
        {
            using var texture = new Texture2D(_device, 1, 1);
            texture.SetData(new[] { premultiplied ? new Color(128, 128, 128, 128) : new Color(255, 255, 255, 128) });
            var camera = ParticleRenderItem.CameraAxes.From(View, Matrix.Identity);
            var white = new ParticleState(Vector3.Zero, Vector3.Zero, new Vector2(10, 10), 0, Vector3.One, 1, 0, 0);
            var pixel = Render(() => ParticleRenderItem.DrawEmitter(_device, Shader, View, Matrix.Identity, camera,
                new EmitterSettings { Premultiplied = premultiplied }, texture, [white]), Color.Black);
            Assert.That(pixel.X, Is.EqualTo(0.216).Within(0.01));
        }

        // ps_lighting on a chain blended by alpha, against CA's ps_main transcribed below: smoke_heavy's settings lit from the
        // front, vanilla's defaults lit from behind, no particles to scatter (no sun at all), half emissive, and a scene light at
        // double strength. No ambient cube is bound, so the sky adds nothing. The 10 m quad faces the camera.
        [TestCase(0.8f, 0f, 0.4f, 0f, 0.8f, 0.04f, 0f, 1f)]
        [TestCase(0.1f, 0.5f, 0.4f, 0f, 0.6f, 0.5f, MathHelper.Pi, 1f)]
        [TestCase(0f, 0f, 0.4f, 0f, 0.6f, 0.5f, 0f, 1f)]
        [TestCase(0.8f, 0f, 0.4f, 0.5f, 0.8f, 0.2f, 0f, 1f)]
        [TestCase(0.8f, 0f, 0.4f, 0f, 0.8f, 0.04f, 0f, 2f)]
        public void Lighting_LightsASmokeQuadAsCaDoes(float small, float large, float directionality, float emissive, float averageSize, float colour, float turn, float intensity)
        {
            var lighting = new ParticleLighting(small, large, directionality, emissive, averageSize);
            var view = View with { DirLightRotationRadians_Y = turn, LightIntensityMult = intensity };
            var camera = ParticleRenderItem.CameraAxes.From(view, Matrix.Identity);
            var quad = new ParticleState(Vector3.Zero, Vector3.Zero, new Vector2(10, 10), 0, new Vector3(colour), 1, 0, 0);
            var pixel = Render(() => ParticleRenderItem.DrawEmitter(_device, Shader, view, Matrix.Identity, camera, new EmitterSettings { Lighting = lighting }, _white, [quad]));

            var toSun = Vector3.Normalize(Vector3.TransformNormal(Vector3.Normalize(new Vector3(0.1f, 0.1f, 1)), Matrix.CreateRotationY(turn)));
            var sun = 20000 * 3 * intensity * MathF.Pow(MathF.Sin(MathHelper.ToRadians(0.5f)), 2);
            var stored = new Color(new Vector4(colour, colour, colour, 1)).R / 255f;
            var expected = Encode(Math.Clamp(Uncharted2(stored * CaLight(lighting, 10, Vector3.UnitZ, toSun, Vector3.UnitZ, sun)), 0, 1));
            Assert.That(pixel.X, Is.EqualTo(expected).Within(0.01), $"lit {pixel.X}, unlit {colour}");
        }

        // The sky's light, through the ambient cube the models use, at its 0.261: no sun here, as no particles scatter it.
        [Test]
        public void Lighting_TheSkyLightsASmokeQuad()
        {
            using var sky = new TextureCube(_device, 1, false, SurfaceFormat.Color);
            foreach (var face in Enum.GetValues<CubeMapFace>())
                sky.SetData(face, new[] { new Color(128, 128, 128, 255) });
            var effect = Shader;
            var camera = ParticleRenderItem.CameraAxes.From(View, Matrix.Identity);
            var quad = new ParticleState(Vector3.Zero, Vector3.Zero, new Vector2(10, 10), 0, Vector3.One, 1, 0, 0);
            effect.Parameters["AmbientCube"].SetValue(sky);
            var pixel = Render(() => ParticleRenderItem.DrawEmitter(_device, effect, View, Matrix.Identity, camera,
                new EmitterSettings { Lighting = new ParticleLighting(0, 0, 0.4f, 0, 0.6f) }, _white, [quad]));
            effect.Parameters["AmbientCube"].SetValue((TextureCube?)null);
            Assert.That(pixel.X, Is.EqualTo(Encode(Uncharted2(128 / 255f * 0.261f))).Within(0.01));
        }

        // A lit particle's colour is light, as a model's is, and blooms no more than a model's does.
        [Test]
        public void Lighting_ALitQuadAddsNothingToTheGlowPass()
        {
            var camera = ParticleRenderItem.CameraAxes.From(View, Matrix.Identity);
            var quad = new ParticleState(Vector3.Zero, Vector3.Zero, new Vector2(10, 10), 0, new Vector3(40), 1, 0, 0);
            var pixel = Render(() => ParticleRenderItem.DrawEmitter(_device, Shader, View, Matrix.Identity, camera,
                new EmitterSettings { Lighting = new ParticleLighting(0.8f, 0, 0.4f, 10, 0.8f) }, _white, [quad], glow: true), Color.Black);
            Assert.That(pixel.X, Is.EqualTo(0).Within(0.01));
        }

        // ...but it still hides the glow of what lies behind it, as unlit smoke does: a fire's glow under a lit puff of alpha 1
        // is gone from the glow pass.
        [Test]
        public void Lighting_ALitQuadHidesTheGlowBehindIt()
        {
            var camera = ParticleRenderItem.CameraAxes.From(View, Matrix.Identity);
            var fire = new ParticleState(Vector3.Zero, Vector3.Zero, new Vector2(10, 10), 0, new Vector3(40, 0, 0), 1, 0, 0);
            var puff = fire with { Colour = new Vector3(4) };
            var pixel = Render(() =>
            {
                ParticleRenderItem.DrawEmitter(_device, Shader, View, Matrix.Identity, camera, new EmitterSettings { AdditiveMul = 1 }, _white, [fire], glow: true);
                ParticleRenderItem.DrawEmitter(_device, Shader, View, Matrix.Identity, camera,
                    new EmitterSettings { Lighting = new ParticleLighting(0.8f, 0, 0.4f, 0, 0.8f) }, _white, [puff], glow: true);
            }, Color.Black);
            Assert.That(pixel.X, Is.EqualTo(0).Within(0.01));
        }

        // ps_normal shapes that light. CA's ps_main reads a DXT5nm texel, x in alpha and y in green, each half a step up, y down
        // the texture. The sun high and the texel tilted up; the sun to the right and the texel tilted right; and the same quad
        // mirrored left to right, which tilts it left, away from the sun.
        [TestCase(-MathHelper.PiOver2, 0f, 128, 51, false)]
        [TestCase(0f, MathHelper.PiOver2, 204, 128, false)]
        [TestCase(0f, MathHelper.PiOver2, 204, 128, true)]
        public void NormalMap_ShapesTheLight(float pitch, float turn, int alpha, int green, bool mirrored)
        {
            var lighting = new ParticleLighting(0.8f, 0, 0.4f, 0, 0.8f);
            var view = View with { DirLightRotationRadians_X = pitch, DirLightRotationRadians_Y = turn };
            using var normals = new Texture2D(_device, 1, 1);
            normals.SetData(new[] { new Color(0, green, 0, alpha) });
            var camera = ParticleRenderItem.CameraAxes.From(view, Matrix.Identity);
            var quad = new ParticleState(Vector3.Zero, Vector3.Zero, new Vector2(10, 10), 0, new Vector3(128 / 255f), 1, 0, 0, FlipU: mirrored);
            var pixel = Render(() => ParticleRenderItem.DrawEmitter(_device, Shader, view, Matrix.Identity, camera,
                new EmitterSettings { Lighting = lighting }, _white, [quad], normalMap: normals));

            var (x, y) = (2 * (alpha / 255f + 0.001961f) - 1, 2 * (green / 255f + 0.001961f) - 1);
            var tangent = Vector3.Normalize(new Vector3(x, y, MathF.Sqrt(Math.Max(0, 1 - x * x - y * y))));
            // Across the quad is u, which the mirror turns round; down it is v.
            var normal = Vector3.Normalize(tangent.X * (mirrored ? -Vector3.UnitX : Vector3.UnitX) - tangent.Y * Vector3.UnitY + tangent.Z * Vector3.UnitZ);
            var toSun = Vector3.Normalize(Vector3.TransformNormal(Vector3.Normalize(new Vector3(0.1f, 0.1f, 1)), Matrix.CreateRotationY(turn) * Matrix.CreateRotationX(pitch)));
            var sun = 20000 * 3 * MathF.Pow(MathF.Sin(MathHelper.ToRadians(0.5f)), 2);
            float Seen(Vector3 n) => Encode(Math.Clamp(Uncharted2(128 / 255f * CaLight(lighting, 10, n, toSun, Vector3.UnitZ, sun)), 0, 1));
            Assert.That(pixel.X, Is.EqualTo(Seen(normal)).Within(0.01), $"flat {Seen(Vector3.UnitZ)}");
        }

        // CA's ps_main lighting (fx/particle_vfx_main in shaders.pack, disassembled): sunlight scattered through the particle,
        // Rayleigh's phase for small_particles and Henyey-Greenstein's, with (1 - g)^2 over it, for large_particles at
        // g = directionality, weighted by each, over light let through, exp(-(small + large) x average_size x size x (1 - N.L) / 2);
        // drawn toward emissive by its own amount. sun: the light a face square to the sun takes. c is the cosine between the
        // way the sunlight travels and the camera's axis toward the camera. No particles is 0 / 0, which the GPU saturates to 0.
        static float CaLight(ParticleLighting l, float size, Vector3 normal, Vector3 toSun, Vector3 toCamera, float sun)
        {
            var c = Vector3.Dot(-toSun, toCamera);
            var sigma = l.Small + l.Large;
            var through = MathF.Exp(-sigma * l.AverageSize * size * (1 - Vector3.Dot(normal, toSun)) * 0.5f);
            var g = l.Directionality;
            var rayleigh = 0.059683f * (1 + c * c);
            var henyey = (1 - g) * (1 - g) * 0.079577f / MathF.Pow(1 + g * g - 2 * g * c, 1.5f);
            var lit = ((l.Small * rayleigh + l.Large * henyey) / sigma) * (1 - through) + through;
            lit = float.IsNaN(lit) ? 0 : Math.Clamp(lit, 0, 1);
            return MathHelper.Lerp(lit * sun, l.Emissive, Math.Clamp(l.Emissive, 0, 1));
        }

        // Shaders/Pbr's tone map and sRGB encoding, through which a model's lit colour reaches the screen.
        static float Uncharted2(float x)
        {
            static float F(float v) => (v * (0.15f * v + 0.05f) + 0.004f) / (v * (0.15f * v + 0.5f) + 0.06f) - 0.02f / 0.3f;
            return F(x * 2) / F(11.2f);
        }

        static float Encode(float x) => x <= 0.0031308f ? 12.92f * x : 1.055f * MathF.Pow(x, 1 / 2.4f) - 0.055f;

        // The ground light and a heat haze hide nothing over nothing: the light's unlit corners and a haze with no scene behind
        // it blacked out the background.
        [Test]
        public void OverNothing_ALightOrAHazeHidesNothing()
        {
            var pool = Render(() => ParticleRenderItem.DrawGroundLight(_device, Shader, Above([new PointLight(new Vector3(0, 20, 0), 40, new Vector3(0.4f, 0, 0) * VfxConventions.LightShare)])), Color.Transparent);
            var normal = new Texture2D(_device, 1, 1);
            normal.SetData(new[] { new Color(255, 128, 255, 255) });
            using var cache = new VfxTextureCache((_, _) => normal);
            using var item = new ParticleRenderItem(OneParticle("normal", distortion: true), cache, () => Shader);
            var haze = Render(() => item.Draw(_device, View, RenderingTechnique.Normal), Color.Transparent);
            Assert.That(new[] { pool.W, haze.W }, Is.All.EqualTo(0).Within(0.01));
        }

        // A colour of 4 lights a quarter-grey texel fully: a vertex colour alone stops at 1 and would leave it a quarter.
        [Test]
        public void HdrColour_IsNotClippedBeforeTheTexture()
        {
            using var quarter = new Texture2D(_device, 1, 1);
            quarter.SetData(new[] { new Color(64, 64, 64, 255) });
            var effect = Shader;
            var camera = ParticleRenderItem.CameraAxes.From(View, Matrix.Identity);
            var bright = RedHalfAlpha with { Colour = new Vector3(4, 0, 0), Alpha = 1 };
            var pixel = Render(() => ParticleRenderItem.DrawEmitter(_device, effect, View, Matrix.Identity, camera, new EmitterSettings(), quarter, [bright]));
            Assert.That(pixel.X, Is.EqualTo(1).Within(0.02));
        }

        // The glow pass is CA's bloom bright pass (fx_hdr_tone_mapper's ps_high_pass_luminance_cutoff): what a particle adds,
        // weighed by how far its luminance lies over bloom_threshold, 4, times bloom_particle_strength, 0.5. Here a grey added onto
        // mid grey: 4.2 adds 4.2 x 0.2 x 0.5; 4, a chaos fire's brightness on a white texel, adds nothing, nor does 1.25. A
        // straight-alpha particle adds its colour times its alpha: 7 at alpha 0.6 adds as 4.2 does, 6.5 at alpha 0.6 nothing.
        [TestCase(4.2f, 1f, true, 0.922f)]
        [TestCase(4f, 1f, true, 0.502f)]
        [TestCase(1.25f, 1f, true, 0.502f)]
        [TestCase(7f, 0.6f, false, 0.922f)]
        [TestCase(6.5f, 0.6f, false, 0.502f)]
        public void GlowPass_IsCAsBrightPass(float colour, float alpha, bool premultiplied, float red)
        {
            var effect = Shader;
            var camera = ParticleRenderItem.CameraAxes.From(View, Matrix.Identity);
            var particle = RedHalfAlpha with { Colour = new Vector3(colour), Alpha = alpha };
            var settings = new EmitterSettings { AdditiveMul = 1, Premultiplied = premultiplied };
            var pixel = Render(() => ParticleRenderItem.DrawEmitter(_device, effect, View, Matrix.Identity, camera, settings, _white, [particle], glow: true));
            Assert.That(pixel.X, Is.EqualTo(red).Within(0.02));
        }

        // The app's emissive pass, which it blooms: an effect draws there too, a tint of 10 leaving white.
        [Test]
        public void TheEmissivePass_DrawsTheEffectsGlow()
        {
            using var item = new ParticleRenderItem(OneParticle(null, new Vector4(10, 10, 10, 1)), NoTextures(), () => Shader);
            var pixel = Render(() => item.Draw(_device, View, RenderingTechnique.Emissive));
            Assert.That(new[] { pixel.X, pixel.Y, pixel.Z }, Is.All.EqualTo(1).Within(0.02));
        }

        // ps_soft_edge against the preview's ground, world y = 0: a 1 m flat quad 0.5 m up, seen from 10 m straight above, is
        // 0.5 m of view ray from the ground, half of a 1 m soft_factor; below the ground it is hidden. A quad under a metre fades
        // over less, by scale_strength: a 0.25 m one 0.1 m up is 0.4 of the way in at full strength and 0.1 at none. The effect's
        // own y = 0 is not the ground: lifted 1 m by its matrix, a quad 0.5 m below its origin is 0.5 m up.
        [Test]
        public void SoftEdges_FadeIntoTheGround()
        {
            var looking = new ParticleRenderItem.CameraAxes(new Vector3(0, 10, 0), Vector3.Right, Vector3.Forward, Vector3.Down);
            float Alpha(EmitterSettings s, float height, ParticleRenderItem.CameraAxes camera, float size = 1) =>
                ParticleRenderItem.Quads(s, camera, [new(new Vector3(0, height, 0), Vector3.Zero, new Vector2(size), 0, Vector3.One, 1, 0, 0)])[0].Color.A / 255f;
            EmitterSettings Soft(float factor, float strength = 1) => new() { Premultiplied = false, Orientation = QuadOrientation.Horizontal, SoftEdge = factor, SoftStrength = strength };
            Assert.Multiple(() =>
            {
                Assert.That(Alpha(Soft(0), 0.5f, looking), Is.EqualTo(1f), "no ps_soft_edge");
                Assert.That(Alpha(Soft(1), 0.5f, looking), Is.EqualTo(0.5f).Within(0.01));
                Assert.That(Alpha(Soft(1, 2), 0.5f, looking), Is.EqualTo(0.5f).Within(0.01), "no exponent");
                Assert.That(Alpha(Soft(1), 0.1f, looking, 0.25f), Is.EqualTo(0.4f).Within(0.01), "small, full strength");
                Assert.That(Alpha(Soft(1, 0), 0.1f, looking, 0.25f), Is.EqualTo(0.1f).Within(0.01), "small, no strength");
                Assert.That(Alpha(Soft(1), -0.5f, looking), Is.Zero, "under the ground");
                var lifted = new ParticleRenderItem.CameraAxes(new Vector3(0, 9, 0), Vector3.Right, Vector3.Forward, Vector3.Down, ToWorld: Matrix.CreateTranslation(0, 1, 0));
                Assert.That(Alpha(Soft(1), -0.5f, lifted), Is.EqualTo(0.5f).Within(0.01), "the ground is the world's");
            });
        }

        // ps_soft_edge against the solid scene, when the engine has drawn its depth: a 10 m white quad 5 m from the camera, well above
        // the ground, with the scene 0.5 m behind it is half faded at a 1 m soft_factor, and with it 2 m behind not at all; the same
        // through the app's orthographic view. A scale_strength of 0 (33 vanilla chains) is the full soft_factor. A 0.4 m quad,
        // filling a 0.3 m view, with the scene 0.2 m behind it fades over 0.4 m at full strength, 0.7 at half and 1 at none.
        [TestCase(5.5f, 1f, false, 10f, 0.5f)]
        [TestCase(7f, 1f, false, 10f, 1f)]
        [TestCase(5.5f, 1f, true, 10f, 0.5f)]
        [TestCase(5.5f, 0f, false, 10f, 0.5f)]
        [TestCase(5.2f, 1f, true, 0.4f, 0.5f)]
        [TestCase(5.2f, 0.5f, true, 0.4f, 0.285f)]
        [TestCase(5.2f, 0f, true, 0.4f, 0.2f)]
        public void SoftEdges_FadeIntoTheSolidScene(float sceneDepth, float strength, bool orthographic, float size, float white)
        {
            using var depth = new Texture2D(_device, 4, 4, false, SurfaceFormat.Single);
            depth.SetData(Enumerable.Repeat(sceneDepth, 16).ToArray());
            var level = Level(depth, orthographic, size < 1 ? 0.3f : 12);
            var camera = ParticleRenderItem.CameraAxes.From(level, Matrix.Identity);
            var quad = new ParticleState(new Vector3(0, 10, 0), Vector3.Zero, new Vector2(size), 0, Vector3.One, 1, 0, 0);
            var settings = new EmitterSettings { Premultiplied = false, SoftEdge = 1, SoftStrength = strength };
            var pixel = Render(() => ParticleRenderItem.DrawEmitter(_device, Shader, level, Matrix.Identity, camera, settings, _white, [quad]), Color.Black);
            Assert.That(pixel.X, Is.EqualTo(white).Within(0.02));
        }

        // The glow pass draws onto a target holding only the emissive models' depth, so the scene's depth hides a particle's glow
        // behind a plain model: a glowing quad 5 m away adds nothing behind a model 3 m away, and its glow in front of one at 7 m.
        [TestCase(3f, false)]
        [TestCase(7f, true)]
        public void GlowPass_IsHiddenBehindTheSolidScene(float sceneDepth, bool glows)
        {
            using var depth = new Texture2D(_device, 4, 4, false, SurfaceFormat.Single);
            depth.SetData(Enumerable.Repeat(sceneDepth, 16).ToArray());
            var level = Level(depth, false);
            var camera = ParticleRenderItem.CameraAxes.From(level, Matrix.Identity);
            var quad = new ParticleState(new Vector3(0, 10, 0), Vector3.Zero, new Vector2(10, 10), 0, new Vector3(40, 0, 0), 1, 0, 0);
            var pixel = Render(() => ParticleRenderItem.DrawEmitter(_device, Shader, level, Matrix.Identity, camera, new EmitterSettings { Premultiplied = false }, _white, [quad], glow: true), Color.Black);
            Assert.That(pixel.X > 0.5f, Is.EqualTo(glows), $"red {pixel.X}");
        }

        // Looking level along -Z at a point 10 m up from 5 m away, in perspective or the app's orthographic view (this wide), over a
        // scene depth.
        static CommonShaderParameters Level(Texture2D depth, bool orthographic, float wide = 12) =>
            new(Matrix.CreateLookAt(new Vector3(0, 10, 5), new Vector3(0, 10, 0), Vector3.Up),
                orthographic ? Matrix.CreateOrthographic(wide, wide, 0.1f, 100) : Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4, 1, 0.1f, 100),
                new Vector3(0, 10, 5), new Vector3(0, 10, 0), 0, 0, 0, 1, [], Vector3.One) { SceneDepth = depth };

        // cell_blend: half way from a red cell to a blue one draws both at half.
        [TestCase(0f, 1f, 0f)]
        [TestCase(0.5f, 0.5f, 0.5f)]
        public void CellBlend_MixesTheCellWithTheNext(float blend, float red, float blue)
        {
            using var strip = new Texture2D(_device, 2, 1);
            strip.SetData(new[] { new Color(255, 0, 0, 255), new Color(0, 0, 255, 255) });
            var camera = ParticleRenderItem.CameraAxes.From(View, Matrix.Identity);
            var particle = RedHalfAlpha with { Colour = Vector3.One, Alpha = 1, Cell = 0, NextCell = 1, CellBlend = blend };
            var pixel = Render(() =>
            {
                _device.SamplerStates[0] = SamplerState.PointClamp;
                ParticleRenderItem.DrawEmitter(_device, Shader, View, Matrix.Identity, camera, new EmitterSettings { CellsX = 2 }, strip, [particle]);
            });
            Assert.That((pixel.X, pixel.Z), Is.EqualTo((red, blue)).Using<(float, float)>((a, b) => Math.Abs(a.Item1 - b.Item1) < 0.02f && Math.Abs(a.Item2 - b.Item2) < 0.02f));
        }

        // ps_alpha_crush on a texel of alpha 0.75 (225, read as sRGB) over mid grey: a soft crush of 0.5 leaves half of it, a
        // sharp one of 0.8 cuts it away and one of 0.5 leaves it whole.
        [TestCase(false, 0f, 0.875f)]
        [TestCase(false, 0.5f, 0.751f)]
        [TestCase(true, 0.8f, 0.502f)]
        [TestCase(true, 0.5f, 0.875f)]
        public void AlphaCrush_ThinsTheTexture(bool sharp, float crush, float grey)
        {
            using var faint = new Texture2D(_device, 1, 1);
            faint.SetData(new[] { new Color(225, 225, 225, 225) });
            var camera = ParticleRenderItem.CameraAxes.From(View, Matrix.Identity);
            var particle = RedHalfAlpha with { Colour = Vector3.One, Alpha = 1, Crush = crush };
            var pixel = Render(() => ParticleRenderItem.DrawEmitter(_device, Shader, View, Matrix.Identity, camera, new EmitterSettings { CrushSharp = sharp }, faint, [particle]));
            Assert.That(pixel.X, Is.EqualTo(grey).Within(0.02));
        }

        // ps_gradient: a half-grey texel picks the colour half way along the strip, here red rising 0 to 1, no green, full blue.
        [Test]
        public void Gradient_ColoursTheTextureByItsValue()
        {
            using var grey = new Texture2D(_device, 1, 1);
            grey.SetData(new[] { new Color(128, 128, 128, 255) });
            var camera = ParticleRenderItem.CameraAxes.From(View, Matrix.Identity);
            var ramp = VfxCurve.Parse("up,1,0;0.0,0.0,linear,linear,0,0,0,0,0;1.0,1.0,linear,linear,0,0,0,0,0|none,1,0;0.0,0.0,linear,linear,0,0,0,0,0|full,1,0;0.0,1.0,linear,linear,0,0,0,0,0:r,0;0,0|g,0;1,1|b,0;2,2");
            var settings = new EmitterSettings { Gradient = ramp, GradientAlpha = VfxCurve.Constant(1) };
            var particle = RedHalfAlpha with { Colour = Vector3.One, Alpha = 1 };
            var pixel = Render(() => ParticleRenderItem.DrawEmitter(_device, Shader, View, Matrix.Identity, camera, settings, grey, [particle]));
            Assert.That(new[] { pixel.X, pixel.Y, pixel.Z }, Is.EqualTo(new[] { 0.5f, 0f, 1f }).Within(0.03));
        }

        // CA's vs_translation_velocity_v2 (fx/particle_vfx_library.hlsl). The camera at the origin looks down -Z at a particle 10 m
        // off, moving 10 m/s at a stretch of 1 per m/s, capped at 3. Head-on is 5 degrees off the view, as a muzzle flash seen down
        // its barrel is.
        static readonly ParticleRenderItem.CameraAxes DownMinusZ = new(Vector3.Zero, Vector3.UnitX, Vector3.UnitY, -Vector3.UnitZ);
        static readonly Vector3 HeadOn = new(10 * MathF.Sin(MathHelper.ToRadians(5)), 0, 10 * MathF.Cos(MathHelper.ToRadians(5)));

        static (Vector3 Across, Vector3 Up) Edges(QuadOrientation orientation, Vector3 velocity)
        {
            var particle = new ParticleState(new Vector3(0, 0, -10), velocity, new Vector2(1, 1), 0, Vector3.One, 1, 1, 0);
            var v = ParticleRenderItem.Quads(new EmitterSettings { Orientation = orientation, StretchCap = 3 }, DownMinusZ, [particle]);
            return (v[1].Position - v[0].Position, v[5].Position - v[0].Position);
        }

        // Mode 5 stretches by as much of the velocity as crosses the screen, never under its own size: seen down its path it is
        // a round burst, not a full-length streak along whatever crumb of the velocity is left on the screen.
        [Test]
        public void CameraAlignedWithStretching_SeenHeadOn_IsNotStretched()
        {
            var (across, up) = Edges(QuadOrientation.VelocityAligned, HeadOn);
            Assert.That((across.Length(), up.Length()), Is.EqualTo((1f, 1f)).Within(1e-3));
        }

        [Test]
        public void CameraAlignedWithStretching_SeenSideOn_StretchesAlongTheVelocity()
        {
            var (across, up) = Edges(QuadOrientation.VelocityAligned, new Vector3(10, 0, 0));
            Assert.Multiple(() =>
            {
                Assert.That(up.Length(), Is.EqualTo(3).Within(1e-3));
                Assert.That(Vector3.Dot(Vector3.Normalize(up), Vector3.UnitX), Is.EqualTo(1).Within(1e-4));
                Assert.That(Vector3.Dot(across, Vector3.UnitZ), Is.EqualTo(0).Within(1e-4), "facing the camera");
            });
        }

        // Mode 2 lies along the velocity itself, so seen head-on it is foreshortened by the view rather than turned flat to it.
        [Test]
        public void CameraFacingVelocityAligned_LiesAlongTheVelocity()
        {
            var (_, up) = Edges(QuadOrientation.VelocityAxis, HeadOn);
            Assert.Multiple(() =>
            {
                Assert.That(up.Length(), Is.EqualTo(3).Within(1e-3));
                Assert.That(Vector3.Dot(Vector3.Normalize(up), Vector3.Normalize(HeadOn)), Is.EqualTo(1).Within(1e-4));
            });
        }

        // A decal lies flat on the world's ground under its particle, whatever its own orientation and however the effect is
        // carried: here a camera-facing particle 2 m up in an effect lifted 1 m and tipped 90 degrees about X.
        [Test]
        public void Decal_LiesFlatOnTheGroundUnderItsParticle()
        {
            var effect = Matrix.CreateRotationX(MathHelper.PiOver2) * Matrix.CreateTranslation(3, 1, 0);
            var camera = ParticleRenderItem.CameraAxes.From(View, effect);
            var particle = new ParticleState(new Vector3(0, 2, 0), Vector3.Zero, new Vector2(2, 2), 0, Vector3.One, 1, 0, 0);
            var corners = ParticleRenderItem.Quads(new EmitterSettings { Decal = true }, camera, [particle]).Select(v => Vector3.Transform(v.Position, effect)).ToList();
            Assert.Multiple(() =>
            {
                Assert.That(corners.Select(c => c.Y), Is.All.EqualTo(0.01f).Within(1e-4), "on the ground");
                var middle = corners.Aggregate(Vector3.Zero, (sum, c) => sum + c) / corners.Count;
                Assert.That(new[] { middle.X, middle.Z }, Is.EqualTo(new[] { 3f, 2f }).Within(1e-4), "under the particle");
                Assert.That(corners.Max(c => c.X) - corners.Min(c => c.X), Is.EqualTo(2).Within(1e-3), "as wide as it");
            });
        }

        // Seen from above, 5 m up, with the scene's light particles as the engine hands them (Lights() of every item).
        static CommonShaderParameters Above(IEnumerable<PointLight> lights) => new(Matrix.CreateLookAt(new Vector3(0, 5, 0), Vector3.Zero, Vector3.Forward),
            Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4, 1, 0.1f, 100), new Vector3(0, 5, 0), Vector3.Zero, 0, 0, 0, 1, [], Vector3.One) { PointLights = lights.ToList() };

        // A white light particle at this alpha, as Lights() hands it the scene: colour x alpha at CA's share of the sun.
        static PointLight White(Vector3 at, float reach, float alpha) => new(at, reach, Vector3.One * alpha * VfxConventions.LightShare);

        // The light CA's ps_main_deferred_dynamic_light puts on the ground: colour x alpha x 2500 on the scale of the models' sun
        // (20000 x 3 at intensity 1, both taking sin^2(0.5 deg)), times the battle's dynamic_light_scale of 0.1, on a mid-grey ground
        // of albedo 0.18, through the models' tone map.
        static float Expected(float lit) => Encode(Math.Clamp(Uncharted2(0.18f * 2500 * 0.1f * MathF.Pow(MathF.Sin(MathHelper.ToRadians(0.5f)), 2) * lit), 0, 1));

        // A dynamic light lights the ground under it, its alpha not held to 1, reaching as far from the light as the particle is
        // wide and falling off as (1 - d/r)^2, as CA's does with its smooth falloff. Over black, a white light 40 m across: a quarter
        // of its reach up it lights the centre at 0.5625 of its strength, at alpha 10 and at 200; half its reach up, at 0.25; its
        // reach up, not at all.
        [TestCase(10f, 10f, 0.5625f)]
        [TestCase(10f, 200f, 0.5625f)]
        [TestCase(20f, 200f, 0.25f)]
        [TestCase(40f, 200f, 0f)]
        public void Light_LightsTheGroundUnderIt(float height, float alpha, float fall)
        {
            var pixel = Render(() => ParticleRenderItem.DrawGroundLight(_device, Shader, Above([White(new Vector3(0, height, 0), 40, alpha)])), Color.Black);
            Assert.That(pixel.X, Is.EqualTo(Expected(alpha * fall)).Within(0.01));
        }

        // ...all round it, by how squarely the ground meets it: one 20 m up and 6 m off to either side lights the ground under the
        // camera.
        [TestCase(0f, 6f)]
        [TestCase(6f, 0f)]
        public void Light_LightsTheGroundAllRound(float x, float z)
        {
            var pixel = Render(() => ParticleRenderItem.DrawGroundLight(_device, Shader, Above([White(new Vector3(x, 20, z), 40, 200)])), Color.Black);
            var d = MathF.Sqrt(6 * 6 + 20 * 20);
            Assert.That(pixel.X, Is.EqualTo(Expected(200 * MathF.Pow(1 - d / 40, 2) * (20 / d))).Within(0.01));
        }

        // CA adds up every light reaching a spot of ground and tone maps the sum once: lights tone mapped one by one and added after
        // ran the Bale Taurus's leg lights to white. Lights 40 m wide and 20 m up: four at alpha 200 on one spot light it as one light
        // four times as strong; one there and one 100 m off, out of reach, as the one alone; and the 35 a Bale Taurus stands among,
        // at alpha 10, as 35 times one, every one counted once.
        [TestCase(4, 0, 200f)]
        [TestCase(1, 1, 200f)]
        [TestCase(35, 0, 10f)]
        public void Light_AllTheLightsOnASpotAddUpBeforeTheToneMap(int overhead, int away, float alpha)
        {
            var at = Enumerable.Repeat(new Vector3(0, 20, 0), overhead).Concat(Enumerable.Repeat(new Vector3(100, 20, 0), away));
            var pixel = Render(() => ParticleRenderItem.DrawGroundLight(_device, Shader, Above(at.Select(x => White(x, 40, alpha)))), Color.Black);
            Assert.That(pixel.X, Is.EqualTo(Expected(overhead * alpha * 0.25f)).Within(0.01));
        }

        // A light particle draws nothing of its own, in either pass: the engine draws the light all of them put on the ground at once
        // (DrawGroundLight), and light blooms no more than a lit model does.
        [TestCase(false)]
        [TestCase(true)]
        public void Light_AParticleDrawsNothingItself(bool glow)
        {
            var down = Above([]);
            var light = new ParticleState(new Vector3(0, 1, 0), Vector3.Zero, new Vector2(40, 40), 0, Vector3.One, 20, 0, 0);
            var pixel = Render(() => ParticleRenderItem.DrawEmitter(_device, Shader, down, Matrix.Identity, ParticleRenderItem.CameraAxes.From(down, Matrix.Identity),
                new EmitterSettings { Decal = true, Light = true }, _white, [light], glow: glow), Color.Black);
            Assert.That(pixel.X, Is.EqualTo(0).Within(0.01));
        }

        // CA reads a distortion map's x from alpha and its y from green (ps_distortion_alphablend samples it .ywxz); red is unused, and
        // is 255 in every texel of shockwave_normal.dds. Read as x, it moved the whole quad's scene sideways: the Slayer Pirates' rum
        // haze laid a second dwarf beside the first.
        [Test]
        public void Distortion_ReadsXFromAlpha_NotRed()
        {
            using var cache = new VfxTextureCache((_, _) => Dxt5nm(128));
            using var item = new ParticleRenderItem(OneParticle("normal", distortion: true), cache, () => Shader);
            Assert.That(Shift(HazeOver(item, At(5), Ramp)), Is.EqualTo(0).Within(0.5));
        }

        // fx_hdr_to_screen moves the finished scene by 0.02 of the view per unit of the distortion buffer, in screen space: the same
        // share at any distance, however big the particle. A full tilt at full alpha is 0.02 of a view 512 wide.
        [TestCase(1f)]
        [TestCase(5f)]
        public void Distortion_MovesAFixedShareOfTheView_AtAnyDistance(float distance)
        {
            using var cache = new VfxTextureCache((_, _) => Dxt5nm(255));
            using var item = new ParticleRenderItem(OneParticle("normal", distortion: true), cache, () => Shader);
            Assert.That(Shift(HazeOver(item, At(distance), Ramp)), Is.EqualTo(0.02f * 512).Within(0.5));
        }

        // The particle writes its alpha times its tilt, a, as the buffer's cover and a times the tilt as the shift; the buffer keeps
        // what is left uncovered, and the screen pass weighs the shift by the cover, so one particle moves the scene 0.02 x a^2 x tilt.
        [TestCase(1f, 255, 1f)]
        [TestCase(0.5f, 255, 0.25f)]
        [TestCase(1f, 191, 0.125f)]
        public void Distortion_WeighsTheShiftByAlphaAndTilt(float alpha, int x, float share)
        {
            using var cache = new VfxTextureCache((_, _) => Dxt5nm(x));
            using var item = new ParticleRenderItem(OneParticle("normal", new Vector4(1, 1, 1, alpha), distortion: true), cache, () => Shader);
            Assert.That(Shift(HazeOver(item, At(5), Ramp)), Is.EqualTo(0.02f * 512 * share).Within(0.5));
        }

        // The screen pass moves each pixel; it does not lay a moved copy over the scene. At half alpha the haze moves the centre 2.6
        // pixels, onto blue: a copy laid over at half alpha left it half red.
        [Test]
        public void Distortion_MovesThePixel_LeavesNoCopyBehind()
        {
            using var cache = new VfxTextureCache((_, _) => Dxt5nm(255));
            using var item = new ParticleRenderItem(OneParticle("normal", new Vector4(1, 1, 1, 0.5f), distortion: true), cache, () => Shader);
            var pixel = HazeOver(item, At(5), x => x <= HazeWidth / 2 ? Color.Red : Color.Blue);
            Assert.That(new[] { pixel.R, pixel.B }, Is.EqualTo(new[] { 0, 255 }).Within(3));
        }

        // The app lays its scene over the background by alpha, so the scene's alpha moves with it: a model's edge moved over empty
        // view leaves the background showing there, not the model where it was.
        [Test]
        public void Distortion_MovesTheScenesAlphaToo()
        {
            using var cache = new VfxTextureCache((_, _) => Dxt5nm(255));
            using var item = new ParticleRenderItem(OneParticle("normal", distortion: true), cache, () => Shader);
            var pixel = HazeOver(item, At(5), x => x <= HazeWidth / 2 ? Color.Red : Color.Transparent);
            Assert.That(pixel.A, Is.EqualTo(0).Within(3));
        }

        // A DXT5nm normal map of one texel, as CA's are: x in alpha, y in green, red and blue unused.
        Texture2D Dxt5nm(int x, int y = 128)
        {
            var normal = new Texture2D(_device, 1, 1);
            normal.SetData(new[] { new Color(255, y, 0, x) });
            return normal;
        }

        static CommonShaderParameters At(float distance) => new(Matrix.CreateLookAt(new Vector3(0, 0, distance), Vector3.Zero, Vector3.Up),
            Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4, 1, 0.1f, 100), new Vector3(0, 0, distance), Vector3.Zero, 0, 0, 0, 1, [], Vector3.One);

        const int HazeWidth = 512;

        // A red ramp, 4 levels a pixel across the middle 64 of the view, 128 at the centre pixel: the red the centre ends up with
        // says where the scene under it was read from.
        static Color Ramp(int x) => new(Math.Clamp((x - HazeWidth / 2 + 32) * 4, 0, 255), 0, 0, 255);

        static float Shift(Color centre) => centre.R / 4f - 32;

        // The centre pixel of a view HazeWidth across, after a haze draws over a scene of these columns.
        Color HazeOver(ParticleRenderItem item, CommonShaderParameters view, Func<int, Color> scene)
        {
            using var target = new RenderTarget2D(_device, HazeWidth, 8, false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
            using var columns = new Texture2D(_device, HazeWidth, 1);
            columns.SetData(Enumerable.Range(0, HazeWidth).Select(scene).ToArray());
            using var batch = new SpriteBatch(_device);
            _device.SetRenderTarget(target);
            batch.Begin(SpriteSortMode.Immediate, BlendState.Opaque, SamplerState.PointClamp);
            batch.Draw(columns, new Rectangle(0, 0, HazeWidth, 8), Color.White);
            batch.End();
            _device.DepthStencilState = DepthStencilState.None;
            _device.RasterizerState = RasterizerState.CullNone;
            item.Draw(_device, view, RenderingTechnique.Normal);
            _device.SetRenderTarget(null);
            var pixels = new Color[HazeWidth * 8];
            target.GetData(pixels);
            return pixels[4 * HazeWidth + HazeWidth / 2];
        }

        // A rigid particle's model: its pivot (a share of its box) on the particle, scaled by its size, turned X, Y, then Z.
        [Test]
        public void RigidMatrix_PutsThePivotOnTheParticle()
        {
            var box = new BoundingBox(Vector3.Zero, new Vector3(2, 4, 2));
            var m = VfxConventions.RigidMatrix(box, new Vector3(0.5f, 0, 0.5f), 3, new Vector3(0, MathHelper.PiOver2, 0), Matrix.Identity, new Vector3(10, 0, 0));
            Assert.That(Vector3.Distance(Vector3.Transform(new Vector3(1, 0, 1), m), new Vector3(10, 0, 0)), Is.LessThan(1e-4), "the pivot");
            Assert.That(Vector3.Distance(Vector3.Transform(new Vector3(2, 0, 1), m), new Vector3(10, 0, -3)), Is.LessThan(1e-4), "+X a metre off, scaled 3, turned to -Z");
            var framed = VfxConventions.RigidMatrix(box, new Vector3(0.5f, 0, 0.5f), 3, new Vector3(0, MathHelper.PiOver2, 0), Matrix.CreateRotationX(MathHelper.PiOver2), new Vector3(10, 0, 0));
            Assert.That(Vector3.Distance(Vector3.Transform(new Vector3(2, 0, 1), framed), new Vector3(10, 3, 0)), Is.LessThan(1e-4), "then turned by its emitter's frame");
        }

        // The preview's PBR materials stand in for CA's character and rigid shaders; the rest (Tidecall's water, the dissolves,
        // the unlit vortices: most of vanilla's rigid VFX models) are CA's own, which it cannot run.
        [TestCase("shaders/wh2_vfx_cst_tidecall.xml.shader", true)]
        [TestCase("shaders/wh3_vfx_rigid_emissive_dissolve.xml.shader", true)]
        [TestCase("shaders/campaign_vortex_unlit.xml.shader", true)]
        [TestCase("shaders/weighted4_character_alpha.xml.shader", false)]
        [TestCase("shaders/weighted2_character_emissive.xml.shader", false)]
        [TestCase("shaders/rigid_character.xml.shader", false)]
        [TestCase("shaders/rigid_default.xml.shader", false)]
        [TestCase("shaders/weighted2_default.xml.shader", false)]
        [TestCase("SHADERS/System/Weighted_Standard_4.xml.shader", false)]
        [TestCase("", false)]
        public void IsCustomShader_IsAnyButTheCharacterAndRigidOnes(string shader, bool custom) =>
            Assert.That(VfxConventions.IsCustomShader(shader), Is.EqualTo(custom));

        // A rigid particle's AnimFile plays at AnimSpeed from the particle's birth and loops: most of vanilla's 118 are run
        // cycles, idles and tentacles. A clip with no length holds its first frame.
        [TestCase(0.5f, 2f, 4f, 0.25f)]
        [TestCase(5f, 1f, 2f, 0.5f)]
        [TestCase(1f, 1f, 0f, 0f)]
        public void AnimShare_PlaysAtItsSpeedAndLoops(float age, float speed, float length, float share) =>
            Assert.That(VfxConventions.AnimShare(age, speed, length), Is.EqualTo(share).Within(1e-5));

        // A rigid particle takes its emitter's turn, without its place or scale; one RELATIVE_TO_TERRAIN_NORMAL stands on the
        // preview's flat ground, so it keeps only the emitter's heading.
        [Test]
        public void RigidFrame_TurnsWithTheEmitter_AndOnTheGroundKeepsOnlyItsHeading()
        {
            var frame = Matrix.CreateScale(2) * Matrix.CreateRotationX(MathHelper.PiOver4) * Matrix.CreateRotationY(MathHelper.PiOver2) * Matrix.CreateTranslation(5, 6, 7);
            var turned = VfxConventions.RigidFrame(frame, onTerrain: false);
            var heading = VfxConventions.RigidFrame(frame, onTerrain: true);
            Assert.Multiple(() =>
            {
                Assert.That(Vector3.Distance(Vector3.Transform(Vector3.UnitY, turned), Vector3.Normalize(Vector3.TransformNormal(Vector3.UnitY, frame))), Is.LessThan(1e-4), "turned");
                Assert.That(Vector3.Distance(Vector3.Transform(Vector3.UnitY, heading), Vector3.UnitY), Is.LessThan(1e-4), "upright");
                Assert.That(Vector3.Distance(Vector3.Transform(Vector3.UnitX, heading), -Vector3.UnitZ), Is.LessThan(1e-4), "heading");
            });
        }

        // Each model is loaded once per editor, a miss included; one that loads with no meshes draws nothing.
        [Test]
        public void MeshCache_LoadsEachModelOnce()
        {
            var loads = 0;
            using var cache = new VfxMeshCache(path => { loads++; return path == "missing" ? null : new GroupNode("empty"); });
            cache.Get("vfx/a.wsmodel");
            cache.Get(@"VFX\A.wsmodel");
            cache.Get("missing");
            cache.Get("missing");
            Assert.That((loads, cache.Get("vfx/a.wsmodel").Meshes.Count), Is.EqualTo((2, 0)));
        }

        // A wsmodel is built from a geometry file and material files the cache does not track, so a change to any model file
        // reloads every model; a texture or an effect file reloads none.
        [TestCase(@"variantmeshes\wh_variantmodels\x\x.rigid_model_v2", 2)]
        [TestCase("vfx/models/b.wsmodel", 2)]
        [TestCase("vfx/materials/wave.xml.material", 2)]
        [TestCase("vfx/textures/wave.dds", 1)]
        [TestCase("vfx/fx.xml", 1)]
        public void MeshCache_ForgetsEveryModelWhenAModelFileChanges(string changed, int loads)
        {
            var count = 0;
            using var cache = new VfxMeshCache(path => { count++; return new GroupNode("empty"); });
            cache.Get("vfx/models/a.wsmodel");
            cache.Forget(changed);
            cache.Get("vfx/models/a.wsmodel");
            Assert.That(count, Is.EqualTo(loads));
        }

        // enable_trail_mode: one strip through the particles, oldest to newest, as wide as each, facing the camera, with
        // the texture cell running from the newest (its top, as a velocity-aligned quad's head) to the oldest.
        [Test]
        public void Ribbon_JoinsTheParticles_AsWideAsEach_TextureHeadToTail()
        {
            ParticleState At(float x, float width) => new(new Vector3(x, 0, 0), Vector3.Zero, new Vector2(width, width), 0, Vector3.One, 1, 0, 0);
            var vertices = ParticleRenderItem.Ribbon(new EmitterSettings { Trail = true }, ParticleRenderItem.CameraAxes.From(View, Matrix.Identity), [At(0, 0.2f), At(1, 0.4f), At(2, 0.6f)]);
            Assert.That(vertices, Has.Length.EqualTo(12), "two segments of two triangles");
            var edges = vertices.Select(v => (X: MathF.Round(v.Position.X, 3), HalfWidth: MathF.Round(MathF.Abs(v.Position.Y), 3), V: v.TextureCoordinate.Y)).Distinct().OrderBy(x => x.X).ToList();
            Assert.That(edges, Is.EqualTo(new[] { (0f, 0.1f, 1f), (1f, 0.2f, 0.5f), (2f, 0.3f, 0f) }));
            Assert.That(vertices.All(v => v.Position.Z == 0), Is.True, "faces the camera on +Z");
        }

        // A trail draws as its particles would as quads: mirrored by its newest particle's flip, grown to vs_rendersize_limiter's
        // pixels across (2 m for a 10 cm strip 100 m away, where a metre at a metre is 100 pixels), and faded by ps_soft_edge where
        // it runs into the ground (half, 0.5 m up under a camera looking down, with a 1 m soft_factor).
        [Test]
        public void Ribbon_FlipsGrowsAndSoftensLikeAQuad()
        {
            ParticleState At(float x, float y = 0, float width = 0.1f, bool flipU = false) =>
                new(new Vector3(x, y, 0), Vector3.Zero, new Vector2(width), 0, Vector3.One, 1, 0, 0, FlipU: flipU);
            var camera = ParticleRenderItem.CameraAxes.From(View, Matrix.Identity);
            var far = camera with { Position = new Vector3(0, 0, 100), PixelScale = 100 };
            var looking = new ParticleRenderItem.CameraAxes(new Vector3(0, 10, 0), Vector3.Right, Vector3.Forward, Vector3.Down);
            var trail = new EmitterSettings { Trail = true, Premultiplied = false };
            Assert.Multiple(() =>
            {
                Assert.That(ParticleRenderItem.Ribbon(trail, camera, [At(0), At(1)])[0].TextureCoordinate.X, Is.EqualTo(0f), "plain");
                Assert.That(ParticleRenderItem.Ribbon(trail, camera, [At(0), At(1, flipU: true)])[0].TextureCoordinate.X, Is.EqualTo(1f), "mirrored");
                var grown = ParticleRenderItem.Ribbon(new EmitterSettings { Trail = true, MinPixels = 2 }, far, [At(0), At(1)]);
                Assert.That(grown.Max(v => MathF.Abs(v.Position.Y)), Is.EqualTo(1f).Within(1e-2), "grown");
                var soft = ParticleRenderItem.Ribbon(new EmitterSettings { Trail = true, Premultiplied = false, SoftEdge = 1, SoftStrength = 1 }, looking,
                    [At(0, 0.5f, 1), At(1, 0.5f, 1)]);
                Assert.That(soft.Max(v => v.Color.A / 255f), Is.EqualTo(0.5f).Within(0.02), "softened");
            });
        }

        // Two particles off the sides of the view: as quads they leave its middle grey, as a trail the strip crosses it.
        [TestCase(false, 0.502f)]
        [TestCase(true, 1f)]
        public void TrailMode_DrawsTheStripBetweenTheParticles(bool trail, float red)
        {
            ParticleState At(float x) => new(new Vector3(x, 0, 0), Vector3.Zero, new Vector2(2, 2), 0, new Vector3(1, 0, 0), 1, 0, 0);
            var effect = Shader;
            var camera = ParticleRenderItem.CameraAxes.From(View, Matrix.Identity);
            var pixel = Render(() => ParticleRenderItem.DrawEmitter(_device, effect, View, Matrix.Identity, camera, new EmitterSettings { Trail = trail }, _white, [At(-4), At(4)]));
            Assert.That(pixel.X, Is.EqualTo(red).Within(0.02));
        }

        // The quad's bottom-left corner: a mirrored particle samples the texture's right edge there, and a quarter turn
        // counter-clockwise brings the texture's top-left corner down to it.
        [Test]
        public void Quads_MirrorAndTurnTheTexture()
        {
            var camera = ParticleRenderItem.CameraAxes.From(View, Matrix.Identity);
            Vector2 BottomLeft(EmitterSettings s, bool flipU) => ParticleRenderItem.Quads(s, camera, [RedHalfAlpha with { FlipU = flipU }])[0].TextureCoordinate;
            Assert.That(BottomLeft(new EmitterSettings(), false), Is.EqualTo(new Vector2(0, 1)));
            Assert.That(BottomLeft(new EmitterSettings(), true), Is.EqualTo(new Vector2(1, 1)));
            Assert.That(BottomLeft(new EmitterSettings { UvTurn = 1 }, false), Is.EqualTo(new Vector2(0, 0)));
        }

        // The axis each rare orientation's quad lies flat across, seen from (10, 10, 0): an upright quad turns about Y to face
        // the camera, and the local ones lie in the emitter's own frame, here turned 90 degrees about X or Y.
        [TestCase(QuadOrientation.Vertical, 0f, 0f, 0)]
        [TestCase(QuadOrientation.LocalHorizontal, 90f, 0f, 2)]
        [TestCase(QuadOrientation.LocalVertical, 0f, 90f, 0)]
        public void RareOrientations_LieFlatAcrossTheirAxis(QuadOrientation mode, float aboutX, float aboutY, int flat)
        {
            var camera = ParticleRenderItem.CameraAxes.From(View, Matrix.Identity) with { Position = new Vector3(10, 10, 0) };
            var frame = Matrix.CreateRotationX(MathHelper.ToRadians(aboutX)) * Matrix.CreateRotationY(MathHelper.ToRadians(aboutY));
            var corners = ParticleRenderItem.Quads(new EmitterSettings { Orientation = mode }, camera, [RedHalfAlpha], frame).Select(v => v.Position).ToList();
            float Axis(Vector3 v, int i) => i == 0 ? v.X : i == 1 ? v.Y : v.Z;
            Assert.That(corners.Max(v => Math.Abs(Axis(v, flat))), Is.LessThan(1e-3f));
            foreach (var other in new[] { 0, 1, 2 }.Where(i => i != flat))
                Assert.That(corners.Max(v => Axis(v, other)) - corners.Min(v => Axis(v, other)), Is.GreaterThan(9f), $"spans axis {other}");
        }

        // vs_translation's restriction, as CA's gs_translation builds the quad, under a camera whose up leans 45 degrees toward
        // +Z, so a camera-facing quad lies across no axis. 1 stands it on the world's Y and spreads it along the camera's right
        // (+X), so it lies across Z whatever the camera's position (the name-read Vertical turns to the particle instead). 2
        // lays it in the world's XZ plane: with the effect turned 90 degrees about X, that is the model's XY. 3 turns it by
        // the particle's X, Y and Z and then its emitter, as a rigid particle turns.
        [TestCase(QuadOrientation.Upright, 0f, 0f, 0f, 2)]
        [TestCase(QuadOrientation.Flat, 0f, 0f, 0f, 1)]
        [TestCase(QuadOrientation.Flat, 0f, 0f, 90f, 2)]
        [TestCase(QuadOrientation.Object3D, 0f, 0f, 0f, 2)]
        [TestCase(QuadOrientation.Object3D, 90f, 0f, 0f, 1)]
        [TestCase(QuadOrientation.Object3D, 0f, 90f, 0f, 0)]
        public void Restrictions_LieFlatAcrossTheirAxis(QuadOrientation mode, float turnX, float frameAboutY, float worldAboutX, int flat)
        {
            var world = Matrix.CreateRotationX(MathHelper.ToRadians(worldAboutX));
            var camera = ParticleRenderItem.CameraAxes.From(View, world) with { Position = new Vector3(10, 10, 0), Up = Vector3.Normalize(new Vector3(0, 1, 1)) };
            var particle = RedHalfAlpha with { TurnXy = new Vector2(MathHelper.ToRadians(turnX), 0) };
            var frame = Matrix.CreateRotationY(MathHelper.ToRadians(frameAboutY));
            var corners = ParticleRenderItem.Quads(new EmitterSettings { Orientation = mode }, camera, [particle], frame).Select(v => v.Position).ToList();
            float Axis(Vector3 v, int i) => i == 0 ? v.X : i == 1 ? v.Y : v.Z;
            Assert.That(corners.Max(v => Math.Abs(Axis(v, flat))), Is.LessThan(1e-3f));
            foreach (var other in new[] { 0, 1, 2 }.Where(i => i != flat))
                Assert.That(corners.Max(v => Axis(v, other)) - corners.Min(v => Axis(v, other)), Is.GreaterThan(9f), $"spans axis {other}");
        }

        // On the ground (ParticleRelativeSpawnOrientation RELATIVE_TO_TERRAIN_NORMAL) a 3D quad keeps only its emitter's heading,
        // as a rigid model does: under an emitter tipped 90 degrees about X it stays in the XY plane, where it would otherwise
        // turn with the emitter into XZ.
        [TestCase(true, 2)]
        [TestCase(false, 1)]
        public void Object3DQuads_OnTheGround_KeepOnlyTheEmittersHeading(bool onGround, int flat)
        {
            var camera = ParticleRenderItem.CameraAxes.From(View, Matrix.Identity);
            var s = new EmitterSettings { Orientation = QuadOrientation.Object3D, RigidOnTerrain = onGround };
            var corners = ParticleRenderItem.Quads(s, camera, [RedHalfAlpha], Matrix.CreateRotationX(MathHelper.PiOver2)).Select(v => v.Position).ToList();
            float Axis(Vector3 v, int i) => i == 0 ? v.X : i == 1 ? v.Y : v.Z;
            Assert.That(corners.Max(v => Math.Abs(Axis(v, flat))), Is.LessThan(1e-3f));
        }

        // ParticleCullDistances fades a particle in from x to y metres from the camera and out from z to w; CullDistances does
        // the same to the whole emitter by its own distance. Seen from 3 m, the middle of a 2..4 band is half faded.
        [Test]
        public void CullDistances_FadeByDistanceFromTheCamera()
        {
            var camera = ParticleRenderItem.CameraAxes.From(View, Matrix.Identity) with { Position = new Vector3(0, 0, 3) };
            float Alpha(EmitterSettings s) => ParticleRenderItem.Quads(s, camera, [RedHalfAlpha])[0].Color.A / 255f;
            Assert.Multiple(() =>
            {
                Assert.That(Alpha(new EmitterSettings { Premultiplied = false }), Is.EqualTo(0.5f).Within(0.01));
                Assert.That(Alpha(new EmitterSettings { Premultiplied = false, ParticleCull = new Vector4(2, 4, 100, 200) }), Is.EqualTo(0.25f).Within(0.01));
                Assert.That(Alpha(new EmitterSettings { Premultiplied = false, EmitterCull = new Vector4(0, 0, 1, 5) }), Is.EqualTo(0.25f).Within(0.01));
                Assert.That(Alpha(new EmitterSettings { Premultiplied = false, ParticleCull = new Vector4(0, 0, 1, 2) }), Is.EqualTo(0f), "past the far distance");
            });
        }

        // vs_rendersize_limiter: a 10 cm spark 100 m away covers a tenth of a pixel where a metre at a metre is 100 pixels; a
        // 2 pixel limit grows it to 2 m across, and without one it stays as it is.
        [TestCase(0f, 0.1f)]
        [TestCase(2f, 2f)]
        public void RenderSizeLimiter_KeepsAFarParticleThisManyPixelsAcross(float minPixels, float across)
        {
            var camera = ParticleRenderItem.CameraAxes.From(View, Matrix.Identity) with { Position = new Vector3(0, 0, 100), PixelScale = 100 };
            var spark = RedHalfAlpha with { Size = new Vector2(0.1f, 0.1f) };
            var xs = ParticleRenderItem.Quads(new EmitterSettings { MinPixels = minPixels }, camera, [spark]).Select(v => v.Position.X).ToList();
            Assert.That(xs.Max() - xs.Min(), Is.EqualTo(across).Within(1e-3));
        }

        [Test]
        public void UvRect_AtlasCellThenFlipbookCell()
        {
            var atlas = new EmitterSettings { AtlasX = 8, AtlasY = 8, AtlasCell = 51, CellsX = 2, CellsY = 2 };
            Assert.That(ParticleRenderItem.UvRect(atlas, 3), Is.EqualTo(new Vector4(0.4375f, 0.8125f, 0.0625f, 0.0625f)));
            var strip = new EmitterSettings { CellsX = 8, CellsY = 8 };
            Assert.That(ParticleRenderItem.UvRect(strip, 9), Is.EqualTo(new Vector4(0.125f, 0.125f, 0.125f, 0.125f)));
        }

        // One particle 10 m across at the origin, filling the view.
        static ParticleSimulator OneParticle(string? texture, Vector4? tint = null, bool distortion = false) => Particles(Filling(texture, tint, distortion));

        // An emitter of one particle 10 m across, z metres along the view axis.
        static EmitterSettings Filling(string? texture, Vector4? tint = null, bool distortion = false, float z = 0) => new()
        {
            Name = "test", InfiniteLife = true, Rate = VfxCurve.Constant(60), MaxParticles = 1, ParticleLife = VfxCurve.Constant(100),
            InitialScale = VfxCurve.Constant(10, 10, 10), Texture = texture, TintMin = tint ?? Vector4.One, TintMax = tint ?? Vector4.One, Distortion = distortion,
            ReferenceTransform = Matrix.CreateTranslation(0, 0, z),
        };

        static ParticleSimulator Particles(params EmitterSettings[] settings)
        {
            var simulator = new ParticleSimulator(settings, 1, _ => Matrix.Identity);
            simulator.AdvanceTo(0.5f);
            return simulator;
        }

        // A heat haze shifts what lies behind it when it draws, the effect's own particles included: copied once before any
        // of them, it pasted the bare scene over the mist (tidecall's ripples over its splashes). A flat normal shifts
        // nothing, so the red particle behind stays red.
        [Test]
        public void Distortion_ShiftsTheEffectsOwnParticlesBehindIt()
        {
            using var cache = new VfxTextureCache((_, _) => Dxt5nm(128));
            using var item = new ParticleRenderItem(Particles(Filling(null, new Vector4(1, 0, 0, 1), z: -1), Filling("normal", distortion: true)), cache, () => Shader);
            var pixel = Render(() => item.Draw(_device, View, RenderingTechnique.Normal));
            Assert.That(new[] { pixel.X, pixel.Y }, Is.EqualTo(new[] { 1f, 0f }).Within(0.02));
        }

        // Emitters are drawn back to front, so the nearer lies over the farther whichever the file lists first.
        [Test]
        public void Emitters_AreDrawnFarthestFirst()
        {
            using var item = new ParticleRenderItem(Particles(Filling(null, new Vector4(0, 0, 1, 1), z: 1), Filling(null, new Vector4(1, 0, 0, 1), z: -1)), NoTextures(), () => Shader);
            var pixel = Render(() => item.Draw(_device, View, RenderingTechnique.Normal));
            Assert.That(new[] { pixel.X, pixel.Z }, Is.EqualTo(new[] { 0f, 1f }).Within(0.02));
        }

        // ...and so are an emitter's particles.
        [Test]
        public void Quads_TakeTheFarthestParticleFirst()
        {
            var camera = ParticleRenderItem.CameraAxes.From(View, Matrix.Identity);
            var vertices = ParticleRenderItem.Quads(new EmitterSettings(), camera, [RedHalfAlpha with { Position = new Vector3(0, 0, 1) }, RedHalfAlpha with { Position = new Vector3(0, 0, -1) }]);
            Assert.That(vertices.Take(6).Select(v => v.Position.Z), Is.All.EqualTo(-1f).Within(1e-4));
        }

        // A decal lies on the ground, so it does not fade into it: over a scene depth that is the ground 5 m below, it draws whole.
        [Test]
        public void Decal_DoesNotFadeIntoTheGroundItLiesOn()
        {
            using var depth = new Texture2D(_device, 4, 4, false, SurfaceFormat.Single);
            depth.SetData(Enumerable.Repeat(5f, 16).ToArray());
            var down = Above([]) with { SceneDepth = depth };
            var particle = new ParticleState(new Vector3(0, 1, 0), Vector3.Zero, new Vector2(10, 10), 0, Vector3.One, 1, 0, 0);
            var settings = new EmitterSettings { Decal = true, Premultiplied = false, SoftEdge = 1 };
            var pixel = Render(() => ParticleRenderItem.DrawEmitter(_device, Shader, down, Matrix.Identity, ParticleRenderItem.CameraAxes.From(down, Matrix.Identity), settings, _white, [particle]), Color.Black);
            Assert.That(pixel.X, Is.EqualTo(1).Within(0.02));
        }

        // The engine's depth pass draws an effect's rigid models, the only part of it that is solid.
        [Test]
        public void DepthPass_DrawsTheRigidModels()
        {
            var asked = new List<string>();
            using var meshes = new VfxMeshCache(path => { asked.Add(path); return new GroupNode("empty"); });
            var rigid = Filling(null);
            rigid.Rigid = [new VfxRigidEntry("vfx/models/rock.wsmodel", new Vector3(0.5f), null)];
            using var item = new ParticleRenderItem(Particles(rigid), NoTextures(), () => Shader, meshes);
            Render(() => item.Draw(_device, View, RenderingTechnique.Depth));
            Assert.That(asked, Is.EqualTo(new[] { "vfx/models/rock.wsmodel" }));
        }

        // The haze's copy of the scene leaves the caller's sampler as it found it: it left point sampling behind.
        [Test]
        public void Distortion_LeavesTheCallersSampler()
        {
            using var cache = new VfxTextureCache((_, _) => Dxt5nm(128));
            using var item = new ParticleRenderItem(OneParticle("normal", distortion: true), cache, () => Shader);
            SamplerState? after = null;
            Render(() =>
            {
                _device.SamplerStates[0] = SamplerState.AnisotropicWrap;
                item.Draw(_device, View, RenderingTechnique.Normal);
                after = _device.SamplerStates[0];
            });
            Assert.That(after, Is.SameAs(SamplerState.AnisotropicWrap));
        }

        // The particle shader's own samplers (the scene copy, the scene depth: clamped, point-filtered) stayed on the device after
        // its passes, and the models take whatever the device holds: theirs drew striped, in every tab, since the device is shared.
        [Test]
        public void Draw_LeavesEverySamplerAsItFoundIt()
        {
            using var depth = new Texture2D(_device, 4, 4, false, SurfaceFormat.Single);
            depth.SetData(Enumerable.Repeat(7f, 16).ToArray());
            var soft = Filling(null);
            soft.SoftEdge = 1;
            using var cache = new VfxTextureCache((_, _) => Dxt5nm(128));
            using var item = new ParticleRenderItem(Particles(soft, Filling("normal", distortion: true)), cache, () => Shader);
            var after = new SamplerState[16];
            Render(() =>
            {
                for (var i = 0; i < after.Length; i++)
                    _device.SamplerStates[i] = SamplerState.AnisotropicWrap;
                item.Draw(_device, View with { SceneDepth = depth }, RenderingTechnique.Normal);
                for (var i = 0; i < after.Length; i++)
                    after[i] = _device.SamplerStates[i];
            });
            Assert.That(after, Is.All.SameAs(SamplerState.AnisotropicWrap));
        }

        static VfxTextureCache NoTextures() => new((_, _) => null);

        [Test]
        public void MissingTexture_DrawsWhite()
        {
            using var item = new ParticleRenderItem(OneParticle(@"vfx\textures\missing.dds"), NoTextures(), () => Shader);
            var pixel = Render(() => item.Draw(_device, View, RenderingTechnique.Normal));
            Assert.That(new[] { pixel.X, pixel.Y, pixel.Z }, Is.All.EqualTo(1).Within(0.02));
        }

        // SuperView disposes an effect twice (CleanUp, then the scene's own dispose), and a frame can land
        // between: a draw after Dispose must build its effect again, not draw with disposed objects.
        [Test]
        public void DrawAfterDispose_DrawsAgain()
        {
            using var item = new ParticleRenderItem(OneParticle(null), NoTextures(), () => Shader);
            Render(() => item.Draw(_device, View, RenderingTechnique.Normal));
            item.Dispose();
            var pixel = Render(() => item.Draw(_device, View, RenderingTechnique.Normal));
            Assert.That(new[] { pixel.X, pixel.Y, pixel.Z }, Is.All.EqualTo(1).Within(0.02));
        }

        // SuperView's scene is disposed node by node when its editor closes, and only IDisposable nodes are
        // reached: a VfxEffectNode that was not one kept its GPU objects for the life of the shared device.
        [Test]
        public void DisposingTheEffectNode_DisposesItsRenderItem()
        {
            var item = new ParticleRenderItem(OneParticle(null), NoTextures(), () => Shader);
            Render(() => item.Draw(_device, View, RenderingTechnique.Normal));
            (new VfxEffectNode("test", item) as IDisposable)?.Dispose();
            Assert.That(item.IsDisposed, Is.True);
        }

        // A SuperView rebuild (every click in its tag list) makes new render items, and each one decoded its
        // textures again: about half a second per effect, measured on the firebox. The editor's cache loads a
        // path once and owns what it loads; a render item disposes only its own objects.
        [Test]
        public void TextureCache_RemembersAPathThatDidNotLoad()
        {
            using var cache = NoTextures();
            using var item = new ParticleRenderItem(OneParticle("broken"), cache, () => Shader);
            Assert.That(cache.Failed("broken"), Is.False, "not tried yet");
            Render(() => item.Draw(_device, View, RenderingTechnique.Normal));
            Assert.That((cache.Failed("broken"), cache.Failed("other")), Is.EqualTo((true, false)));
        }

        // A texture replaced in a pack: the cache lets go of the old one, by either separator, and loads the new one.
        [Test]
        public void TextureCache_ForgetsAChangedPath()
        {
            var loads = 0;
            var first = new Texture2D(_device, 1, 1);
            using var cache = new VfxTextureCache((_, _) => ++loads == 1 ? first : new Texture2D(_device, 1, 1));
            cache.Load(_device, @"vfx\textures\a.dds");
            cache.Forget("vfx/textures/A.dds");
            Assert.That(first.IsDisposed, Is.True);
            Assert.That(cache.Load(_device, @"vfx\textures\a.dds"), Is.Not.SameAs(first));
            Assert.That(loads, Is.EqualTo(2));
        }

        [Test]
        public void TextureCache_LoadsEachPathOnce_AndOwnsWhatItLoads()
        {
            var loads = 0;
            var texture = new Texture2D(_device, 1, 1);
            var cache = new VfxTextureCache((_, _) => { loads++; return texture; });
            for (var i = 0; i < 2; i++)
            {
                using var item = new ParticleRenderItem(OneParticle("t"), cache, () => Shader);
                Render(() => item.Draw(_device, View, RenderingTechnique.Normal));
            }
            Assert.Multiple(() =>
            {
                Assert.That(loads, Is.EqualTo(1), "loaded once for both render items");
                Assert.That(texture.IsDisposed, Is.False, "a render item leaves the cache's texture alone");
            });
            cache.Dispose();
            Assert.That(texture.IsDisposed, Is.True);
        }
    }
}
