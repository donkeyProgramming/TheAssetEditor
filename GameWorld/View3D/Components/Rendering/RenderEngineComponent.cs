using System.IO;
using CommunityToolkit.Diagnostics;
using GameWorld.Core.Components.Selection;
using GameWorld.Core.Rendering;
using GameWorld.Core.Services;
using GameWorld.Core.Utility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Serilog;
using Shared.Core.ErrorHandling;
using Shared.Core.Events;
using Shared.Core.Misc;
using Shared.Core.Services;
using Shared.Core.Settings;

namespace GameWorld.Core.Components.Rendering
{
    public record SaveRenderImageSettings(string Name, bool OpenFolder, bool DrawLines);

    public class RenderEngineComponent : BaseComponent, IDisposable
    {
        private readonly ILogger _logger = Logging.Create<RenderEngineComponent>();

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

        bool _cullingEnabled = false;
        bool _bigSceneDepthBiasMode = false;
        bool _drawGlow = true;
        SaveRenderImageSettings? _saveRenderImageSettings;

        private BloomFilter _bloomFilter;
        Texture2D _whiteTexture;

        RenderTarget2D _normalRenderTarget;
        RenderTarget2D _emissiveRenderTarget;
        RenderTarget2D _screenRenderTarget;

        public SpriteBatch CommonSpriteBatch { get; private set; }
        public SpriteFont DefaultFont { get; private set; }


        public RenderEngineComponent(IWpfGame wpfGame, ResourceLibrary resourceLibrary, ArcBallCamera camera, IDeviceResolver deviceResolverComponent, ApplicationSettingsService applicationSettingsService, SceneRenderParametersStore sceneLightParametersStore, IEventHub eventHub)
        {
            UpdateOrder = (int)ComponentUpdateOrderEnum.RenderEngine;
            DrawOrder = (int)ComponentDrawOrderEnum.RenderEngine;

            _wpfGame = wpfGame;
            _resourceLibrary = resourceLibrary;
            _camera = camera;

            _deviceResolverComponent = deviceResolverComponent;
            _applicationSettingsService = applicationSettingsService;
            _sceneLightParameters = sceneLightParametersStore;
            _eventHub = eventHub;

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
            _bloomFilter.Load(device, _resourceLibrary, device.Viewport.Width, device.Viewport.Height);
            _bloomFilter.BloomPreset = BloomFilter.BloomPresets.SuperWide;

            _whiteTexture = new Texture2D(_deviceResolverComponent.Device, 1, 1);
            _whiteTexture.SetData(new[] { Color.White });

            CommonSpriteBatch = new SpriteBatch(device);
            DefaultFont = _wpfGame.Content.Load<SpriteFont>("Fonts//DefaultFont");
        }

        void RebuildRasterStates(bool cullingEnabled, bool bigSceneDepthBias)
        {
            _cullingEnabled = cullingEnabled;
            _bigSceneDepthBiasMode = bigSceneDepthBias;

            // Set renderState to something we dont use, so we can rebuild the ones we care about
            _deviceResolverComponent.Device.RasterizerState = RasterizerState.CullNone;
            RasterStateHelper.Rebuild(_rasterStates, _cullingEnabled, _bigSceneDepthBiasMode);
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
            if (screenWidth <= 10 || screenHeight <= 10)
            {
                // Dont render the screen if its super small,
                // as it causes some werid corner case issues for some users
                return;
            }

            var commonShaderParameters = CommonShaderParameterBuilder.Build(_camera, _sceneLightParameters);
            var backgroundColour = ApplicationSettingsHelper.GetEnumAsColour(_applicationSettingsService.CurrentSettings.RenderEngineBackgroundColour);

            _normalRenderTarget = RenderTargetHelper.GetRenderTarget(device, _normalRenderTarget);
            _emissiveRenderTarget = RenderTargetHelper.GetRenderTarget(device, _emissiveRenderTarget);
            _screenRenderTarget = RenderTargetHelper.GetRenderTarget(device, _screenRenderTarget);

            // Configure render targets
            var backBufferRenderTarget = device.GetRenderTargets()[0].RenderTarget as RenderTarget2D;
            device.SetRenderTarget(_normalRenderTarget);
            device.Clear(Color.Transparent);

            // 2D drawing
            Render2DObjects(device, commonShaderParameters);

            // 3D drawing - Normal scene
            device.DepthStencilState = DepthStencilState.Default;
            Render3DObjects(commonShaderParameters, RenderingTechnique.Normal, drawLines);
           
  
            // Draw the result to the backBuffer
            device.SetRenderTarget(_screenRenderTarget);
            device.Clear(Color.Transparent);
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            spriteBatch.Draw(_normalRenderTarget, new Rectangle(0, 0, screenWidth, screenHeight), Color.White);
            spriteBatch.End();

            if (_drawGlow)
            {
                // 3D drawing - Emissive 
                device.SetRenderTarget(_emissiveRenderTarget);
                device.Clear(Color.Transparent);
                device.DepthStencilState = DepthStencilState.Default;
                Render3DObjects(commonShaderParameters, RenderingTechnique.Emissive, drawLines);

                // While re-sizing or changing view, there is a small chance that the
                // bloomRenderTarget could be null
                var bloomRenderTarget = _bloomFilter.Draw(_emissiveRenderTarget, screenWidth, screenHeight);
                if (bloomRenderTarget != null)
                {
                    device.SetRenderTarget(_screenRenderTarget);
                    spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive);
                    spriteBatch.Draw(bloomRenderTarget, new Rectangle(0, 0, screenWidth, screenHeight), Color.White);
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
                    var folder = "Screenshots";
                    DirectoryHelper.EnsureCreated(folder);
                    using Stream stream = File.Create(folder + "\\" + _saveRenderImageSettings.Name + "_" + DateTime.Now.Ticks + ".png");

                    _deviceResolverComponent.Device.SetRenderTarget(null);
                    _screenRenderTarget.SaveAsPng(stream, _screenRenderTarget.Width, _screenRenderTarget.Height);

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

        void Render3DObjects(CommonShaderParameters commonShaderParameters, RenderingTechnique renderingTechnique, bool drawLines)
        {
            var device = _deviceResolverComponent.Device;
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

            device.RasterizerState = _rasterStates[RasterizerStateEnum.Wireframe];
            foreach (var item in _renderItems[RenderBuckedId.Wireframe])
                item.Draw(device, commonShaderParameters, renderingTechnique);

            device.RasterizerState = _rasterStates[RasterizerStateEnum.SelectedFaces];
            foreach (var item in _renderItems[RenderBuckedId.Selection])
                item.Draw(device, commonShaderParameters, renderingTechnique);
        }

        public void Dispose()
        {
            _eventHub.UnRegister(this);

            CommonSpriteBatch?.Dispose();
            CommonSpriteBatch = null;

            _bloomFilter.Dispose();
            _normalRenderTarget.Dispose();
            _emissiveRenderTarget.Dispose();
            _screenRenderTarget.Dispose();
            _whiteTexture.Dispose();

            _renderLines.Clear();
            _renderItems.Clear();

            foreach (var item in _rasterStates.Values)
                item.Dispose();
            _rasterStates.Clear();
        }
    }
}
