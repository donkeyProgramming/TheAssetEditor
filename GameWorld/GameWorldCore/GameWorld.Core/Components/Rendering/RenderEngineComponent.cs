using System.IO;
using CommunityToolkit.Diagnostics;
using GameWorld.Core.Components.Selection;
using GameWorld.Core.Rendering;
using GameWorld.Core.Services;
using GameWorld.Core.Utility;
using GameWorld.Core.Vfx;
using GameWorld.Core.WpfWindow;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Shared.Core.ErrorHandling;
using Shared.Core.Events;
using Shared.Core.Misc;
using Shared.Core.Settings;

namespace GameWorld.Core.Components.Rendering
{
    /// <summary>DrawLines false leaves out the lines and the grid; DrawOverlays false also the labels, axis widget, overlay text,
    /// wireframe and selected faces, so the picture is saved alone.</summary>
    public record SaveRenderImageSettings(string Name, bool OpenFolder, bool DrawLines, float ImageUpScaleFactor, string OutputFolder, bool DrawOverlays = true);

    public class RenderEngineComponent : BaseComponent, IDisposable
    {
        private readonly ILogger _logger;

        private readonly Dictionary<RasterizerStateEnum, RasterizerState> _rasterStates = [];
        private readonly IWpfGame _wpfGame;
        private readonly ResourceLibrary _resourceLibrary;
        private readonly ArcBallCamera _camera;
        private readonly Dictionary<RenderBuckedId, List<IRenderItem>> _renderItems = [];
        private readonly List<VertexPositionColor> _renderLines = [];
        private readonly IDeviceResolver _deviceResolverComponent;
        private readonly ApplicationSettingsService _applicationSettingsService;
        private readonly SceneRenderParametersStore _sceneLightParameters;
        private readonly IEventHub _eventHub;
        private readonly IGraphicsResourceCreator _graphicsResourceCreator;

        bool _cullingEnabled = false;
        bool _bigSceneDepthBiasMode = false;
        bool _drawGlow = true;
        SaveRenderImageSettings? _saveRenderImageSettings;

        private BloomFilter _bloomFilter;
        Texture2D _whiteTexture;

        RenderTarget2D _normalRenderTarget;
        RenderTarget2D _emissiveRenderTarget;
        // The glow is light, added whole whatever alpha the glow pass left: weighed by alpha it dropped a premultiplied additive
        // particle's glow, which carries none, and a bloom with alpha hid the background behind it.
        readonly BlendState _addLight = new() { ColorSourceBlend = Blend.One, ColorDestinationBlend = Blend.One, AlphaSourceBlend = Blend.Zero, AlphaDestinationBlend = Blend.One };
        RenderTarget2D _screenRenderTarget;
        RenderTarget2D? _depthRenderTarget;

        public SpriteBatch CommonSpriteBatch { get; private set; }
        public SpriteFont DefaultFont { get; private set; }

        // Confines the 3D pass to a centred square instead of the full viewport, for a fixed-aspect (1:1) projection.
        public bool SquareViewport { get; set; }

        // Still has real alpha, unlike the final composited surface. Null before the first frame.
        public RenderTarget2D? LastFrame => _screenRenderTarget;

        public RenderEngineComponent(IWpfGame wpfGame, ResourceLibrary resourceLibrary, ArcBallCamera camera, IDeviceResolver deviceResolverComponent, ApplicationSettingsService applicationSettingsService, SceneRenderParametersStore sceneLightParametersStore, IEventHub eventHub, IGraphicsResourceCreator graphicsResourceCreator, IScopedLogger scopedLogger)
        {
            _logger = scopedLogger.ForContext<RenderEngineComponent>();
            UpdateOrder = (int)ComponentUpdateOrderEnum.RenderEngine;
            DrawOrder = (int)ComponentDrawOrderEnum.RenderEngine;

            _wpfGame = wpfGame;
            _resourceLibrary = resourceLibrary;
            _camera = camera;

            _deviceResolverComponent = deviceResolverComponent;
            _applicationSettingsService = applicationSettingsService;
            _sceneLightParameters = sceneLightParametersStore;
            _eventHub = eventHub;
            _graphicsResourceCreator = graphicsResourceCreator;

            foreach (RenderBuckedId value in Enum.GetValues(typeof(RenderBuckedId)))
                _renderItems.Add(value, new List<IRenderItem>(100));

            _renderLines = new List<VertexPositionColor>(1000);

            _eventHub.Register<SelectionChangedEvent>(this, OnSelectionChanged);
        }

        public void SaveNextFrame(SaveRenderImageSettings settings)
        {
            _saveRenderImageSettings = settings;
            _logger.Here().Information($"Saving next frame - {settings.Name}");
        }

        void OnSelectionChanged(SelectionChangedEvent changedEvent)
        {
            if (changedEvent.NewState.Mode == GeometrySelectionMode.Object)
                _drawGlow = true;
            else
                _drawGlow = false;
        }

        public override void Initialize()
        {
            RebuildRasterStates(_cullingEnabled, _bigSceneDepthBiasMode);

            var device = _deviceResolverComponent.Device;

            _bloomFilter = new BloomFilter();
            _bloomFilter.Load(device, _resourceLibrary, _graphicsResourceCreator, device.Viewport.Width, device.Viewport.Height);
            _bloomFilter.BloomPreset = BloomFilter.BloomPresets.SuperWide;

            _whiteTexture = _graphicsResourceCreator.CreateTexture2D(1, 1);
            _whiteTexture.SetData(new[] { Color.White });

            CommonSpriteBatch = _graphicsResourceCreator.CreateSpriteBatch();
            DefaultFont = _wpfGame.Content.Load<SpriteFont>("Fonts//DefaultFont");
        }

        void RebuildRasterStates(bool cullingEnabled, bool bigSceneDepthBias)
        {
            _cullingEnabled = cullingEnabled;
            _bigSceneDepthBiasMode = bigSceneDepthBias;

            // Set renderState to something we dont use, so we can rebuild the ones we care about
            _deviceResolverComponent.Device.RasterizerState = RasterizerState.CullNone;
            RasterStateHelper.Rebuild(_rasterStates, _cullingEnabled, _bigSceneDepthBiasMode, _graphicsResourceCreator);
        }

        public bool BackfaceCulling { get => _cullingEnabled; set => RebuildRasterStates(value, _bigSceneDepthBiasMode); }
        public bool LargeSceneCulling { get => _bigSceneDepthBiasMode; set => RebuildRasterStates(_cullingEnabled, value); }

        public void AddRenderItem(RenderBuckedId id, IRenderItem item)
        {
            _renderItems[id].Add(item);
        }

        public void AddRenderLines(VertexPositionColor[] lineVertices)
        {
            Guard.IsTrue(lineVertices.Length % 2 == 0);
            _renderLines.AddRange(lineVertices);
        }

        public override void Update(GameTime gameTime)
        {
            foreach (var value in _renderItems.Keys)
                _renderItems[value].Clear();

            _renderLines.Clear();
        }

        public override void Draw(GameTime gameTime)
        {
            var device = _deviceResolverComponent.Device;
            var spriteBatch = CommonSpriteBatch;
            var screenWidth = device.Viewport.Width;
            var screenHeight = device.Viewport.Height;
            var drawLines = _saveRenderImageSettings == null ? true: _saveRenderImageSettings.DrawLines;
            var drawOverlays = _saveRenderImageSettings == null ? true : _saveRenderImageSettings.DrawOverlays;
            var imageUpScale = _saveRenderImageSettings == null ? 1 : _saveRenderImageSettings.ImageUpScaleFactor;

            if (screenWidth <= 10 || screenHeight <= 10)
            {
                // Dont render the screen if its super small,
                // as it causes some werid corner case issues for some users
                return;
            }

            var commonShaderParameters = CommonShaderParameterBuilder.Build(_camera, _sceneLightParameters, screenWidth, screenHeight);
            // The VFX preview's light particles light the solid scene too, not only the ground their pools are drawn on.
            // The first MaxPointLights in draw order, not the brightest or nearest; rank them if a scene needs more.
            commonShaderParameters = commonShaderParameters with
            {
                PointLights = _renderItems[RenderBuckedId.Transparent].OfType<ParticleRenderItem>().SelectMany(x => x.Lights()).Take(CommonShaderParameters.MaxPointLights).ToList()
            };
            var backgroundColour = ApplicationSettingsHelper.GetEnumAsColour(_applicationSettingsService.CurrentSettings.RenderEngineBackgroundColour);

            _normalRenderTarget = RenderTargetHelper.GetRenderTarget(device, _normalRenderTarget, imageUpScale, _graphicsResourceCreator, screenWidth, screenHeight);
            _emissiveRenderTarget = RenderTargetHelper.GetRenderTarget(device, _emissiveRenderTarget, imageUpScale, _graphicsResourceCreator, screenWidth, screenHeight);
            _screenRenderTarget = RenderTargetHelper.GetRenderTarget(device, _screenRenderTarget, imageUpScale, _graphicsResourceCreator, screenWidth, screenHeight);

            // Configure render targets
            var backBufferRenderTarget = device.GetRenderTargets()[0].RenderTarget as RenderTarget2D;
            // Soft particles fade where they meet the solid scene, so with particles to draw, the scene's view depth is drawn first
            // into a texture they read. It draws the solid scene a second time, so a frame with no particles skips it.
            if (_renderItems[RenderBuckedId.Transparent].Count != 0)
                commonShaderParameters = commonShaderParameters with { SceneDepth = DrawSceneDepth(device, commonShaderParameters) };
            device.SetRenderTarget(_normalRenderTarget);
            device.Clear(Color.Transparent);

            // 2D drawing: every item in these buckets is an overlay (labels, the axis widget, FPS and stats text)
            if (drawOverlays)
                Render2DObjects(device, commonShaderParameters);

            // 3D drawing - Normal scene
            device.DepthStencilState = DepthStencilState.Default;
            Render3DObjects(commonShaderParameters, RenderingTechnique.Normal, drawLines, drawOverlays);
           
  
            // Draw the result to the backBuffer
            device.SetRenderTarget(_screenRenderTarget);
            device.Clear(Color.Transparent);
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            spriteBatch.Draw(_normalRenderTarget, new Rectangle(0, 0, _screenRenderTarget.Width, _screenRenderTarget.Height), Color.White);
            spriteBatch.End();

            if (_drawGlow)
            {
                // 3D drawing - Emissive 
                device.SetRenderTarget(_emissiveRenderTarget);
                device.Clear(Color.Transparent);
                device.DepthStencilState = DepthStencilState.Default;
                Render3DObjects(commonShaderParameters, RenderingTechnique.Emissive, drawLines, drawOverlays);

                // While re-sizing or changing view, there is a small chance that the
                // bloomRenderTarget could be null
                var bloomRenderTarget = _bloomFilter.Draw(_emissiveRenderTarget, _screenRenderTarget.Width, _screenRenderTarget.Height);
                if (bloomRenderTarget != null)
                {
                    device.SetRenderTarget(_screenRenderTarget);
                    spriteBatch.Begin(SpriteSortMode.Deferred, _addLight);
                    spriteBatch.Draw(bloomRenderTarget, new Rectangle(0, 0, _screenRenderTarget.Width, _screenRenderTarget.Height), Color.White);
                    spriteBatch.End();
                }
            }

            device.SetRenderTarget(backBufferRenderTarget);
            device.Clear(backgroundColour);
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            spriteBatch.Draw(_screenRenderTarget, new Rectangle(0, 0, screenWidth, screenHeight), Color.White);
            spriteBatch.End();

            HandleSaveLastFrame();
        }

        void HandleSaveLastFrame( )
        {
            if (_saveRenderImageSettings != null && _screenRenderTarget != null)
            {
                try
                {
                    var folder = _saveRenderImageSettings.OutputFolder;
                    DirectoryHelper.EnsureCreated(folder);
                    using Stream stream = File.Create(folder + "\\" + _saveRenderImageSettings.Name + "_" + DateTime.Now.Ticks + ".png");

                    _deviceResolverComponent.Device.SetRenderTarget(null);
                    var pixels = new Color[_screenRenderTarget.Width * _screenRenderTarget.Height];
                    _screenRenderTarget.GetData(pixels);
                    SavePng(stream, pixels, _screenRenderTarget.Width, _screenRenderTarget.Height);

                    if (_saveRenderImageSettings.OpenFolder)
                        DirectoryHelper.OpenOrFocusFolder(folder);
                }
                catch (Exception e)
                {
                    _logger.Here().Information($"Failed to save frame - {e.Message}");
                }

                _saveRenderImageSettings = null;
            }
        }

        /// <summary>The frame as a PNG. The frame is premultiplied, as the viewport draws it, and a PNG's alpha is straight. Additive
        /// light (particles, bloom) adds colour and no alpha, so over the empty background it is saved with alpha from its brightness:
        /// over black the picture is the viewport's, and a viewer that reads the alpha no longer drops the light.</summary>
        public static void SavePng(Stream stream, Color[] premultiplied, int width, int height)
        {
            var bgra = new byte[premultiplied.Length * 4];
            for (var i = 0; i < premultiplied.Length; i++)
            {
                var c = premultiplied[i];
                var a = Math.Max(c.A, Math.Max(c.R, Math.Max(c.G, c.B)));
                if (a == 0)
                    continue;
                bgra[i * 4] = (byte)Math.Round(c.B * 255f / a);
                bgra[i * 4 + 1] = (byte)Math.Round(c.G * 255f / a);
                bgra[i * 4 + 2] = (byte)Math.Round(c.R * 255f / a);
                bgra[i * 4 + 3] = a;
            }
            var encoder = new System.Windows.Media.Imaging.PngBitmapEncoder();
            encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(System.Windows.Media.Imaging.BitmapSource.Create(
                width, height, 96, 96, System.Windows.Media.PixelFormats.Bgra32, null, bgra, width * 4)));
            encoder.Save(stream);
        }

        private void Render2DObjects(GraphicsDevice device, CommonShaderParameters commonShaderParameters)
        {
            var spriteBatch = CommonSpriteBatch;
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);

            foreach (var item in _renderItems[RenderBuckedId.Texture2D])
                item.Draw(device, commonShaderParameters, RenderingTechnique.Normal);

            foreach (var item in _renderItems[RenderBuckedId.Font])
                item.Draw(device, commonShaderParameters, RenderingTechnique.Normal);

            spriteBatch.End();
        }

        RenderTarget2D DrawSceneDepth(GraphicsDevice device, CommonShaderParameters parameters)
        {
            var (width, height) = (_normalRenderTarget.Width, _normalRenderTarget.Height);
            if (_depthRenderTarget == null || _depthRenderTarget.Width != width || _depthRenderTarget.Height != height)
            {
                _graphicsResourceCreator.DisposeTracked(_depthRenderTarget);
                _depthRenderTarget = _graphicsResourceCreator.CreateRenderTarget2D(width, height, false, SurfaceFormat.Single, DepthFormat.Depth24, 0, RenderTargetUsage.DiscardContents);
            }
            device.SetRenderTarget(_depthRenderTarget);
            device.Clear(ClearOptions.Target | ClearOptions.DepthBuffer, new Vector4(float.MaxValue), 1, 0);
            SetSquareViewport(device);
            (device.BlendState, device.DepthStencilState, device.RasterizerState) = (BlendState.Opaque, DepthStencilState.Default, _rasterStates[RasterizerStateEnum.Normal]);
            foreach (var item in _renderItems[RenderBuckedId.Normal].Concat(_renderItems[RenderBuckedId.Transparent]))
                item.Draw(device, parameters, RenderingTechnique.Depth);
            return _depthRenderTarget;
        }

        void SetSquareViewport(GraphicsDevice device)
        {
            if (SquareViewport)
            {
                var size = Math.Min(device.Viewport.Width, device.Viewport.Height);
                device.Viewport = new Viewport((device.Viewport.Width - size) / 2, (device.Viewport.Height - size) / 2, size, size);
            }
        }

        void Render3DObjects(CommonShaderParameters commonShaderParameters, RenderingTechnique renderingTechnique, bool drawLines, bool drawOverlays)
        {
            var device = _deviceResolverComponent.Device;

            SetSquareViewport(device);

            device.RasterizerState = _rasterStates[RasterizerStateEnum.Normal];

            if (renderingTechnique == RenderingTechnique.Normal && _renderLines.Count != 0 && drawLines)
            {
                var shader = _resourceLibrary.GetStaticEffect(ShaderTypes.Line);
                shader.Parameters["View"].SetValue(commonShaderParameters.View);
                shader.Parameters["Projection"].SetValue(commonShaderParameters.Projection);
                shader.Parameters["World"].SetValue(Matrix.Identity);

                foreach (var pass in shader.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    device.DrawUserPrimitives(PrimitiveType.LineList, _renderLines.ToArray(), 0, _renderLines.Count / 2);
                }
            }

            foreach (var item in _renderItems[RenderBuckedId.Normal])
                item.Draw(device, commonShaderParameters, renderingTechnique);

            if (drawLines)
            {
                foreach (var item in _renderItems[RenderBuckedId.Grid])
                    item.Draw(device, commonShaderParameters, renderingTechnique);
            }

            if (drawOverlays)
            {
                device.RasterizerState = _rasterStates[RasterizerStateEnum.Wireframe];
                foreach (var item in _renderItems[RenderBuckedId.Wireframe])
                    item.Draw(device, commonShaderParameters, renderingTechnique);

                device.RasterizerState = _rasterStates[RasterizerStateEnum.SelectedFaces];
                foreach (var item in _renderItems[RenderBuckedId.Selection])
                    item.Draw(device, commonShaderParameters, renderingTechnique);
            }

            // Particles: after everything solid, in both passes so what lies over 1 blooms, reading depth without writing it,
            // both faces; each item sets its blend.
            if (_renderItems[RenderBuckedId.Transparent].Count != 0)
            {
                var blendState = device.BlendState;
                device.DepthStencilState = DepthStencilState.DepthRead;
                device.RasterizerState = RasterizerState.CullNone;
                // The light every light particle puts on the ground, once for all of them and under them; it blooms no more than a
                // lit model does.
                if (renderingTechnique == RenderingTechnique.Normal)
                    ParticleRenderItem.DrawGroundLight(device, _resourceLibrary.GetStaticEffect(ShaderTypes.Particle), commonShaderParameters);
                foreach (var item in _renderItems[RenderBuckedId.Transparent])
                    item.Draw(device, commonShaderParameters, renderingTechnique);
                device.BlendState = blendState;
                device.DepthStencilState = DepthStencilState.Default;
            }

        }

        public void Dispose()
        {
            _eventHub.UnRegister(this);

            CommonSpriteBatch = _graphicsResourceCreator.DisposeTracked(CommonSpriteBatch);

            _bloomFilter.Dispose();
            _addLight.Dispose();
            _normalRenderTarget = _graphicsResourceCreator.DisposeTracked(_normalRenderTarget);
            _emissiveRenderTarget = _graphicsResourceCreator.DisposeTracked(_emissiveRenderTarget);
            _screenRenderTarget = _graphicsResourceCreator.DisposeTracked(_screenRenderTarget);
            _depthRenderTarget = _graphicsResourceCreator.DisposeTracked(_depthRenderTarget);
            _whiteTexture = _graphicsResourceCreator.DisposeTracked(_whiteTexture);

            _renderLines.Clear();
            _renderItems.Clear();

            foreach (var item in _rasterStates.Values)
                _graphicsResourceCreator.DisposeTracked(item);
            _rasterStates.Clear();
        }
    }
}
