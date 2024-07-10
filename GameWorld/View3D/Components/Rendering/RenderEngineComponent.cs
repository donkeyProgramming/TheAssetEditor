using System;
using System.Collections.Generic;
using GameWorld.Core.Rendering;
using GameWorld.Core.Utility;
using GameWorld.WpfWindow.ResourceHandling;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Shared.Core.Services;

namespace GameWorld.Core.Components.Rendering
{
    //https://github.com/Kosmonaut3d/BloomFilter-for-Monogame-and-XNA/blob/master/Bloom%20Sample/Game1.cs

    public enum RenderBuckedId
    {
        Normal,
        Glow,
        Wireframe,
        Selection,
        Text,
    }

    public enum RenderingTechnique
    {
        Normal,
        Emissive,
    }


    public interface IRenderItem
    {
        void Draw(GraphicsDevice device, CommonShaderParameters parameters, RenderingTechnique renderingTechnique);
    }

    public class RenderEngineComponent : BaseComponent, IDisposable
    {
        enum RasterizerStateEnum
        {
            Normal,
            Wireframe,
            SelectedFaces,
        }

        private readonly Dictionary<RasterizerStateEnum, RasterizerState> _rasterStates = [];
        private readonly ArcBallCamera _camera;
        private readonly Dictionary<RenderBuckedId, List<IRenderItem>> _renderItems = [];
        private readonly ResourceLibrary _resourceLib;
        private readonly IDeviceResolver _deviceResolverComponent;
        private readonly ApplicationSettingsService _applicationSettingsService;
        private readonly SceneLightParametersStore _sceneLightParameters;

        bool _cullingEnabled = false;
        bool _bigSceneDepthBiasMode = false;



        public RenderFormats MainRenderFormat { get; set; } = RenderFormats.SpecGloss;

        public RenderEngineComponent(ArcBallCamera camera, ResourceLibrary resourceLib, IDeviceResolver deviceResolverComponent, ApplicationSettingsService applicationSettingsService, SceneLightParametersStore sceneLightParametersStore)
        {
            _camera = camera;
            _resourceLib = resourceLib;
            _deviceResolverComponent = deviceResolverComponent;
            _applicationSettingsService = applicationSettingsService;
            _sceneLightParameters = sceneLightParametersStore;

            UpdateOrder = (int)ComponentUpdateOrderEnum.RenderEngine;
            DrawOrder = (int)ComponentDrawOrderEnum.RenderEngine;

            foreach (RenderBuckedId value in Enum.GetValues(typeof(RenderBuckedId)))
                _renderItems.Add(value, new List<IRenderItem>(100));

            if (_applicationSettingsService.CurrentSettings.CurrentGame == GameTypeEnum.Warhammer3 || _applicationSettingsService.CurrentSettings.CurrentGame == GameTypeEnum.ThreeKingdoms)
                MainRenderFormat = RenderFormats.MetalRoughness;
        }

        private BloomFilter _bloomFilter;
        Texture2D _whiteTexture;
        public override void Initialize()
        {
            RebuildRasterStates(_cullingEnabled, _bigSceneDepthBiasMode);


            _whiteTexture = new Texture2D(_deviceResolverComponent.Device, 1, 1);
            _whiteTexture.SetData(new[] { Color.White });


            base.Initialize();
        }

        void RebuildRasterStates(bool cullingEnabled, bool bigSceneDepthBias)
        {
            foreach (var item in _rasterStates.Values)
                item.Dispose();
            _rasterStates.Clear();

            var cullMode = cullingEnabled ? CullMode.CullCounterClockwiseFace : CullMode.None;
            float bias = bigSceneDepthBias ? 0 : 0;

            _rasterStates[RasterizerStateEnum.Normal] = new RasterizerState
            {
                FillMode = FillMode.Solid,
                CullMode = cullMode,
                DepthBias = bias,
                DepthClipEnable = true,
                MultiSampleAntiAlias = true
            };

            var depthOffsetBias = 0.00005f;
            _rasterStates[RasterizerStateEnum.Wireframe] = new RasterizerState
            {
                FillMode = FillMode.WireFrame,
                CullMode = cullMode,
                DepthBias = bias - depthOffsetBias,
                DepthClipEnable = true,
                MultiSampleAntiAlias = true
            };

            _rasterStates[RasterizerStateEnum.SelectedFaces] = new RasterizerState
            {
                FillMode = FillMode.Solid,
                CullMode = CullMode.None,
                DepthBias = bias - depthOffsetBias,
                DepthClipEnable = true,
                MultiSampleAntiAlias = true
            };

            _cullingEnabled = cullingEnabled;
            _bigSceneDepthBiasMode = bigSceneDepthBias;
        }

        public void ToggleLargeSceneRendering()
        {
            _deviceResolverComponent.Device.RasterizerState = RasterizerState.CullNone;
            RebuildRasterStates(_cullingEnabled, !_bigSceneDepthBiasMode);
        }

        public void ToggleBackFaceRendering()
        {
            _deviceResolverComponent.Device.RasterizerState = RasterizerState.CullNone;
            RebuildRasterStates(!_cullingEnabled, _bigSceneDepthBiasMode);
        }

        public void AddRenderItem(RenderBuckedId id, IRenderItem item)
        {
            _renderItems[id].Add(item);
        }

        public override void Update(GameTime gameTime)
        {
            foreach (var value in _renderItems.Keys)
                _renderItems[value].Clear();

            base.Update(gameTime);
        }



        RenderTarget2D _screen;
        RenderTarget2D _glow;


        RenderTarget2D EnsureRenderTarget(RenderTarget2D existingRenderTarget)
        {
            var width = _deviceResolverComponent.Device.Viewport.Width;
            var height = _deviceResolverComponent.Device.Viewport.Height;

            if (existingRenderTarget == null)
            {
                return new RenderTarget2D(_deviceResolverComponent.Device, width, height, false, SurfaceFormat.Color, DepthFormat.Depth24);
            }

            if (_screen.Width == width && _screen.Height == height)
                return existingRenderTarget;

            existingRenderTarget.Dispose();
            return new RenderTarget2D(_deviceResolverComponent.Device, width, height, false, SurfaceFormat.Color, DepthFormat.Depth24);
        }

        RenderTarget2D GetScreenRenderTarget()
        {
            _screen = EnsureRenderTarget(_screen);
            return _screen;
        }

        RenderTarget2D GetGlowRenderTarget()
        {
            _glow = EnsureRenderTarget(_glow);
            return _glow;
        }
       

        public override void Draw(GameTime gameTime)
        {
            var w = _deviceResolverComponent.Device.Viewport.Width;
            var h = _deviceResolverComponent.Device.Viewport.Height;

            if (_bloomFilter == null)
            {
                _bloomFilter = new BloomFilter();
                _bloomFilter.Load(_deviceResolverComponent.Device, _resourceLib, w, h);
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
            var backBufferRenderTarget = _deviceResolverComponent.Device.GetRenderTargets()[0];
            var defaultRenderTarget = GetScreenRenderTarget();
           _deviceResolverComponent.Device.SetRenderTarget(defaultRenderTarget);

            // 2D drawing
            var clearColour = new Color(54, 54, 54);
            _resourceLib.CommonSpriteBatch.Begin();
            _resourceLib.CommonSpriteBatch.Draw(_whiteTexture, new Rectangle(0, 0, w, h), clearColour);
            _resourceLib.CommonSpriteBatch.End();

            _resourceLib.CommonSpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            foreach (var item in _renderItems[RenderBuckedId.Text])
                item.Draw(_deviceResolverComponent.Device, commonShaderParameters, RenderingTechnique.Normal);
            _resourceLib.CommonSpriteBatch.End();

            // 3D drawing - Normal scene
            _deviceResolverComponent.Device.DepthStencilState = DepthStencilState.Default;
            
            _deviceResolverComponent.Device.RasterizerState = _rasterStates[RasterizerStateEnum.Normal];
            foreach (var item in _renderItems[RenderBuckedId.Normal])
                item.Draw(_deviceResolverComponent.Device, commonShaderParameters, RenderingTechnique.Normal);

            _deviceResolverComponent.Device.RasterizerState = _rasterStates[RasterizerStateEnum.Wireframe];
            foreach (var item in _renderItems[RenderBuckedId.Wireframe])
                item.Draw(_deviceResolverComponent.Device, commonShaderParameters, RenderingTechnique.Normal);

            _deviceResolverComponent.Device.RasterizerState = _rasterStates[RasterizerStateEnum.SelectedFaces];
            foreach (var item in _renderItems[RenderBuckedId.Selection])
                item.Draw(_deviceResolverComponent.Device, commonShaderParameters, RenderingTechnique.Normal);

            // 3D drawing - Emissive 
            var glowRenderTarget = GetGlowRenderTarget();
            _deviceResolverComponent.Device.SetRenderTarget(glowRenderTarget);
            foreach (var item in _renderItems[RenderBuckedId.Normal])
                item.Draw(_deviceResolverComponent.Device, commonShaderParameters, RenderingTechnique.Emissive);
            
            var bloomRenderTarget = _bloomFilter.Draw(glowRenderTarget, w, h);

            _deviceResolverComponent.Device.SetRenderTarget(backBufferRenderTarget.RenderTarget as RenderTarget2D);
            _resourceLib.CommonSpriteBatch.Begin();
            _resourceLib.CommonSpriteBatch.Draw(defaultRenderTarget, new Rectangle(0, 0, w, h), Color.White);
            _resourceLib.CommonSpriteBatch.End();
          
            _resourceLib.CommonSpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive);
            _resourceLib.CommonSpriteBatch.Draw(bloomRenderTarget, new Rectangle(0, 0, w, h), Color.White);
            _resourceLib.CommonSpriteBatch.End();
        }

        public void Dispose()
        {
            _bloomFilter.Dispose();
            _screen.Dispose();

            _renderItems.Clear();
            foreach (var item in _rasterStates.Values)
                item.Dispose();
            _rasterStates.Clear();
        }
    }
}
