using System;
using System.Collections.Generic;
using CommunityToolkit.Diagnostics;
using GameWorld.Core.Rendering;
using GameWorld.Core.Utility;
using GameWorld.WpfWindow.ResourceHandling;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Shared.Core.Services;

namespace GameWorld.Core.Components.Rendering
{
    //https://github.com/Kosmonaut3d/BloomFilter-for-Monogame-and-XNA/blob/master/Bloom%20Sample/Game1.cs



    public class RenderEngineComponent : BaseComponent, IDisposable
    {
        private readonly Dictionary<RasterizerStateEnum, RasterizerState> _rasterStates = [];
        private readonly ArcBallCamera _camera;
        private readonly Dictionary<RenderBuckedId, List<IRenderItem>> _renderItems = [];
        private readonly List<VertexPositionColor> _renderLines = [];
        private readonly ResourceLibrary _resourceLib;
        private readonly IDeviceResolver _deviceResolverComponent;
        private readonly ApplicationSettingsService _applicationSettingsService;
        private readonly SceneLightParametersStore _sceneLightParameters;

        bool _cullingEnabled = false;
        bool _bigSceneDepthBiasMode = false;

        private BloomFilter _bloomFilter;
        Texture2D _whiteTexture;

        RenderTarget2D _screen;
        RenderTarget2D _glow;

        public RenderFormats MainRenderFormat { get; set; } = RenderFormats.SpecGloss;  // This should be removed!

        public RenderEngineComponent(ArcBallCamera camera, ResourceLibrary resourceLib, IDeviceResolver deviceResolverComponent, ApplicationSettingsService applicationSettingsService, SceneLightParametersStore sceneLightParametersStore)
        {
            UpdateOrder = (int)ComponentUpdateOrderEnum.RenderEngine;
            DrawOrder = (int)ComponentDrawOrderEnum.RenderEngine;

            _camera = camera;
            _resourceLib = resourceLib;
            _deviceResolverComponent = deviceResolverComponent;
            _applicationSettingsService = applicationSettingsService;
            _sceneLightParameters = sceneLightParametersStore;

            foreach (RenderBuckedId value in Enum.GetValues(typeof(RenderBuckedId)))
                _renderItems.Add(value, new List<IRenderItem>(100));

            _renderLines = new List<VertexPositionColor>(1000);
            
            if (_applicationSettingsService.CurrentSettings.CurrentGame == GameTypeEnum.Warhammer3 || _applicationSettingsService.CurrentSettings.CurrentGame == GameTypeEnum.ThreeKingdoms)
                MainRenderFormat = RenderFormats.MetalRoughness;
        }


        public override void Initialize()
        {
            RebuildRasterStates(_cullingEnabled, _bigSceneDepthBiasMode);

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

        public void ToggleLargeSceneRendering() => RebuildRasterStates(_cullingEnabled, !_bigSceneDepthBiasMode);

        public void ToggleBackFaceRendering() => RebuildRasterStates(!_cullingEnabled, _bigSceneDepthBiasMode);

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
            _screen = RenderTargetHelper.GetRenderTarget(device, _screen);
            _glow = RenderTargetHelper.GetRenderTarget(device, _glow);

            var w = device.Viewport.Width;
            var h = device.Viewport.Height;

            if (_bloomFilter == null)
            {
                _bloomFilter = new BloomFilter();
                _bloomFilter.Load(device, _resourceLib, w, h);
                _bloomFilter.BloomPreset = BloomFilter.BloomPresets.SuperWide;
            }

            var commonShaderParameters = new CommonShaderParameters()
            {
                Projection = _camera.ProjectionMatrix,
                View = _camera.ViewMatrix,
                CameraPosition = _camera.Position,
                CameraLookAt = _camera.LookAt,
                EnvLightRotationsRadians_Y = MathHelper.ToRadians(_sceneLightParameters.EnvLightRotationDegrees_Y),
                DirLightRotationRadians_X = MathHelper.ToRadians(_sceneLightParameters.DirLightRotationDegrees_X),
                DirLightRotationRadians_Y = MathHelper.ToRadians(_sceneLightParameters.DirLightRotationDegrees_Y),
                LightIntensityMult = _sceneLightParameters.LightIntensityMult
            };

            // Configure render targets
            var backBufferRenderTarget = device.GetRenderTargets()[0];
            var defaultRenderTarget = _screen;
            device.SetRenderTarget(defaultRenderTarget);

            var spriteBatch = _resourceLib.CommonSpriteBatch;

            // 2D drawing
            var clearColour = new Color(54, 54, 54);
            spriteBatch.Begin();
            spriteBatch.Draw(_whiteTexture, new Rectangle(0, 0, w, h), clearColour);
            spriteBatch.End();

            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            foreach (var item in _renderItems[RenderBuckedId.Text])
                item.Draw(device, commonShaderParameters, RenderingTechnique.Normal);
            spriteBatch.End();

            // 3D drawing - Lines
            device.DepthStencilState = DepthStencilState.Default;

            
             var shader = _resourceLib.GetStaticEffect(ShaderTypes.Line);

                shader.Parameters["View"].SetValue(commonShaderParameters.View);
                shader.Parameters["Projection"].SetValue(commonShaderParameters.Projection);
                shader.Parameters["World"].SetValue(Matrix.Identity);

                foreach (var pass in shader.CurrentTechnique.Passes)
                {
                    pass.Apply();
                device.DrawUserPrimitives(PrimitiveType.LineList, _renderLines.ToArray(), 0, _renderLines.Count / 2);
                }


            // 3D drawing - Normal scene

            device.RasterizerState = _rasterStates[RasterizerStateEnum.Normal];
            foreach (var item in _renderItems[RenderBuckedId.Normal])
                item.Draw(device, commonShaderParameters, RenderingTechnique.Normal);

            device.RasterizerState = _rasterStates[RasterizerStateEnum.Wireframe];
            foreach (var item in _renderItems[RenderBuckedId.Wireframe])
                item.Draw(device, commonShaderParameters, RenderingTechnique.Normal);

            device.RasterizerState = _rasterStates[RasterizerStateEnum.SelectedFaces];
            foreach (var item in _renderItems[RenderBuckedId.Selection])
                item.Draw(device, commonShaderParameters, RenderingTechnique.Normal);

            // TODO - dont draw emissive when in edit mode!
            // 3D drawing - Emissive 
            var glowRenderTarget = _glow;
            device.SetRenderTarget(glowRenderTarget);
            foreach (var item in _renderItems[RenderBuckedId.Normal])
                item.Draw(device, commonShaderParameters, RenderingTechnique.Emissive);
            
            var bloomRenderTarget = _bloomFilter.Draw(glowRenderTarget, w, h);

            device.SetRenderTarget(backBufferRenderTarget.RenderTarget as RenderTarget2D);
            spriteBatch.Begin();
            spriteBatch.Draw(defaultRenderTarget, new Rectangle(0, 0, w, h), Color.White);
            spriteBatch.End();
          
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive);
            spriteBatch.Draw(bloomRenderTarget, new Rectangle(0, 0, w, h), Color.White);
            spriteBatch.End();
        }

        public void Dispose()
        {
            _bloomFilter.Dispose();
            _screen.Dispose();
            _glow.Dispose();
            _whiteTexture.Dispose();

            _renderLines.Clear();
            _renderItems.Clear();

            foreach (var item in _rasterStates.Values)
                item.Dispose();
            _rasterStates.Clear();
        }
    }
}
