using System;
using System.Collections.Generic;
using CommunityToolkit.Diagnostics;
using GameWorld.Core.Components.Selection;
using GameWorld.Core.Rendering;
using GameWorld.Core.Services;
using GameWorld.Core.Utility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Shared.Core.Events;
using Shared.Core.Services;
using Shared.Core.Settings;

namespace GameWorld.Core.Components.Rendering
{
    public class RenderEngineComponent : BaseComponent, IDisposable
    {
        Color _backgroundColour;

        private readonly Dictionary<RasterizerStateEnum, RasterizerState> _rasterStates = [];
        private readonly ArcBallCamera _camera;
        private readonly Dictionary<RenderBuckedId, List<IRenderItem>> _renderItems = [];
        private readonly List<VertexPositionColor> _renderLines = [];
        private readonly ResourceLibrary _resourceLib;
        private readonly IDeviceResolver _deviceResolverComponent;
        private readonly SceneRenderParametersStore _sceneLightParameters;
        private readonly IEventHub _eventHub;

        bool _cullingEnabled = false;
        bool _bigSceneDepthBiasMode = false;
        bool _drawGlow = true;

        private BloomFilter _bloomFilter;
        Texture2D _whiteTexture;

        RenderTarget2D _defaultRenderTarget;
        RenderTarget2D _glowRenderTarget;

        public RenderEngineComponent(ArcBallCamera camera, ResourceLibrary resourceLib, IDeviceResolver deviceResolverComponent, ApplicationSettingsService applicationSettingsService, SceneRenderParametersStore sceneLightParametersStore, IEventHub eventHub)
        {
            UpdateOrder = (int)ComponentUpdateOrderEnum.RenderEngine;
            DrawOrder = (int)ComponentDrawOrderEnum.RenderEngine;

            _backgroundColour = ApplicationSettingsHelper.GetEnumAsColour(applicationSettingsService.CurrentSettings.RenderEngineBackgroundColour);
            _camera = camera;
            _resourceLib = resourceLib;
            _deviceResolverComponent = deviceResolverComponent;
            _sceneLightParameters = sceneLightParametersStore;
            _eventHub = eventHub;

            foreach (RenderBuckedId value in Enum.GetValues(typeof(RenderBuckedId)))
                _renderItems.Add(value, new List<IRenderItem>(100));

            _renderLines = new List<VertexPositionColor>(1000);

            _eventHub.Register<SelectionChangedEvent>(this, OnSelectionChanged);
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
            _bloomFilter.Load(device, _resourceLib, device.Viewport.Width, device.Viewport.Height);
            _bloomFilter.BloomPreset = BloomFilter.BloomPresets.SuperWide;

            _whiteTexture = new Texture2D(_deviceResolverComponent.Device, 1, 1);
            _whiteTexture.SetData(new[] { Color.White });
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
            var spriteBatch = _resourceLib.CommonSpriteBatch;
            var screenWidth = device.Viewport.Width;
            var screenHeight = device.Viewport.Height;
            var commonShaderParameters = CommonShaderParameterBuilder.Build(_camera, _sceneLightParameters);

            _defaultRenderTarget = RenderTargetHelper.GetRenderTarget(device, _defaultRenderTarget);
            _glowRenderTarget = RenderTargetHelper.GetRenderTarget(device, _glowRenderTarget);

            // Configure render targets
            var backBufferRenderTarget = device.GetRenderTargets()[0].RenderTarget as RenderTarget2D;
            device.SetRenderTarget(_defaultRenderTarget);

            // 2D drawing
            Render2DObjects(device, commonShaderParameters);

            // 3D drawing - Normal scene
            device.DepthStencilState = DepthStencilState.Default;
            Render3DObjects(commonShaderParameters, RenderingTechnique.Normal);

            // 3D drawing - Emissive 
            device.SetRenderTarget(_glowRenderTarget);
            Render3DObjects(commonShaderParameters, RenderingTechnique.Emissive);

            // Draw the result to the backBuffer
            device.SetRenderTarget(backBufferRenderTarget);
            spriteBatch.Begin();
            spriteBatch.Draw(_defaultRenderTarget, new Rectangle(0, 0, screenWidth, screenHeight), Color.White);
            spriteBatch.End();

            if (_drawGlow)
            {
                var bloomRenderTarget = _bloomFilter.Draw(_glowRenderTarget, screenWidth, screenHeight);
                device.SetRenderTarget(backBufferRenderTarget);
                spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive);
                spriteBatch.Draw(bloomRenderTarget, new Rectangle(0, 0, screenWidth, screenHeight), Color.White);
                spriteBatch.End();
            }
        }

        private void Render2DObjects(GraphicsDevice device, CommonShaderParameters commonShaderParameters)
        {
            var spriteBatch = _resourceLib.CommonSpriteBatch;
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);

            // Clear the screen
            spriteBatch.Draw(_whiteTexture, new Rectangle(0, 0, device.Viewport.Width, device.Viewport.Height), _backgroundColour);

            foreach (var item in _renderItems[RenderBuckedId.Font])
                item.Draw(device, commonShaderParameters, RenderingTechnique.Normal);
            spriteBatch.End();
        }

        void Render3DObjects(CommonShaderParameters commonShaderParameters, RenderingTechnique renderingTechnique)
        {
            var device = _deviceResolverComponent.Device;
            device.RasterizerState = _rasterStates[RasterizerStateEnum.Normal];

            if (renderingTechnique == RenderingTechnique.Normal)
            {
                var shader = _resourceLib.GetStaticEffect(ShaderTypes.Line);
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

            _bloomFilter.Dispose();
            _defaultRenderTarget.Dispose();
            _glowRenderTarget.Dispose();
            _whiteTexture.Dispose();

            _renderLines.Clear();
            _renderItems.Clear();

            foreach (var item in _rasterStates.Values)
                item.Dispose();
            _rasterStates.Clear();
        }
    }
}
