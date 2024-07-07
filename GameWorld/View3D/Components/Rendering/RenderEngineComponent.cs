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
        Line,
        Text,
        ConstantDebugLine,
    }

    public interface IRenderItem
    {
        Matrix ModelMatrix { get; set; }
        void Draw(GraphicsDevice device, CommonShaderParameters parameters);
        void DrawGlowPass(GraphicsDevice device, CommonShaderParameters parameters);
    }

    public class RenderEngineComponent : BaseComponent, IDisposable
    {
        enum RasterizerStateEnum
        {
            Default,
            Wireframe,
            SelectedFaces,
        }

        private readonly Dictionary<RasterizerStateEnum, RasterizerState> _rasterStates = [];
        private readonly ArcBallCamera _camera;
        private readonly Dictionary<RenderBuckedId, List<IRenderItem>> _renderItems = [];
        private readonly ResourceLibrary _resourceLib;
        private readonly IDeviceResolver _deviceResolverComponent;
        private readonly ApplicationSettingsService _applicationSettingsService;

        bool _cullingEnabled = false;
        bool _bigSceneDepthBiasMode = false;

        public float EnvLightRotationDegrees_Y { get; set; } = 20;
        public float DirLightRotationDegrees_X { get; set; } = 0;
        public float DirLightRotationDegrees_Y { get; set; } = 0;
        public float LightIntensityMult { get; set; } = 1;

        public RenderFormats MainRenderFormat { get; set; } = RenderFormats.SpecGloss;

        public RenderEngineComponent(ArcBallCamera camera, ResourceLibrary resourceLib, IDeviceResolver deviceResolverComponent, ApplicationSettingsService applicationSettingsService)
        {
            _camera = camera;
            _resourceLib = resourceLib;
            _deviceResolverComponent = deviceResolverComponent;
            _applicationSettingsService = applicationSettingsService;
            UpdateOrder = (int)ComponentUpdateOrderEnum.RenderEngine;
            DrawOrder = (int)ComponentDrawOrderEnum.RenderEngine;

            foreach (RenderBuckedId value in Enum.GetValues(typeof(RenderBuckedId)))
                _renderItems.Add(value, new List<IRenderItem>(100));

            if (_applicationSettingsService.CurrentSettings.CurrentGame == GameTypeEnum.Warhammer3 || _applicationSettingsService.CurrentSettings.CurrentGame == GameTypeEnum.ThreeKingdoms)
                MainRenderFormat = RenderFormats.MetalRoughness;

            _blendSPriteBatch = _resourceLib.CreateSpriteBatch();
        }



        private BloomFilter _bloomFilter;
        public override void Initialize()
        {
            RebuildRasterStates(_cullingEnabled, _bigSceneDepthBiasMode);


            _bloomFilter = new BloomFilter();
            _bloomFilter.Load(_deviceResolverComponent.Device, _resourceLib, _deviceResolverComponent.Device.Viewport.Width, _deviceResolverComponent.Device.Viewport.Height);
            _bloomFilter.BloomPreset = BloomFilter.BloomPresets.SuperWide;


            base.Initialize();
        }

        void RebuildRasterStates(bool cullingEnabled, bool bigSceneDepthBias)
        {
            foreach (var item in _rasterStates.Values)
                item.Dispose();
            _rasterStates.Clear();

            var cullMode = cullingEnabled ? CullMode.CullCounterClockwiseFace : CullMode.None;
            float bias = bigSceneDepthBias ? 0 : 0;

            _rasterStates[RasterizerStateEnum.Default] = new RasterizerState
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

        public void ToggelBackFaceRendering()
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
            {
                if (value == RenderBuckedId.ConstantDebugLine)
                    continue;
                _renderItems[value].Clear();
            }

            base.Update(gameTime);
        }

        public void ClearDebugBuffer()
        {
            _renderItems[RenderBuckedId.ConstantDebugLine].Clear();
        }

        RenderTarget2D _screen;
        private Microsoft.Xna.Framework.Graphics.SpriteBatch _blendSPriteBatch;

        RenderTarget2D GetScreenRenderTarget()
        {
            if (_screen == null)
            {
                _screen = new RenderTarget2D(_deviceResolverComponent.Device, _deviceResolverComponent.Device.Viewport.Width, _deviceResolverComponent.Device.Viewport.Height);
                return _screen;
            }

            if (_screen.Width == _deviceResolverComponent.Device.Viewport.Width && _screen.Height == _deviceResolverComponent.Device.Viewport.Height)
                return _screen;

            _screen.Dispose();
            _screen = new RenderTarget2D(_deviceResolverComponent.Device, _deviceResolverComponent.Device.Viewport.Width, _deviceResolverComponent.Device.Viewport.Height);
            return _screen;
        }

        public override void Draw(GameTime gameTime)
        {
            var commonShaderParameters = new CommonShaderParameters()
            {
                Projection = _camera.ProjectionMatrix,
                View = _camera.ViewMatrix,
                CameraPosition = _camera.Position,
                CameraLookAt = _camera.LookAt,
                EnvLightRotationsRadians_Y = MathHelper.ToRadians(EnvLightRotationDegrees_Y),
                DirLightRotationRadians_X = MathHelper.ToRadians(DirLightRotationDegrees_X),
                DirLightRotationRadians_Y = MathHelper.ToRadians(DirLightRotationDegrees_Y),
                LightIntensityMult = LightIntensityMult
            };



            var screen = GetScreenRenderTarget();
            

            // _deviceResolverComponent.Device.SetRenderTarget(null);
            var s = _deviceResolverComponent.Device.GetRenderTargets()[0];
            _deviceResolverComponent.Device.SetRenderTarget(screen);



            _resourceLib.CommonSpriteBatch.Begin();
            foreach (var item in _renderItems[RenderBuckedId.Text])
                item.Draw(_deviceResolverComponent.Device, commonShaderParameters);
            _resourceLib.CommonSpriteBatch.End();

            _deviceResolverComponent.Device.DepthStencilState = DepthStencilState.Default;
            _deviceResolverComponent.Device.RasterizerState = _rasterStates[RasterizerStateEnum.Default];

    

            foreach (var item in _renderItems[RenderBuckedId.Normal])
                item.Draw(_deviceResolverComponent.Device, commonShaderParameters);

            _deviceResolverComponent.Device.RasterizerState = _rasterStates[RasterizerStateEnum.Wireframe];
            foreach (var item in _renderItems[RenderBuckedId.Wireframe])
                item.Draw(_deviceResolverComponent.Device, commonShaderParameters);

            _deviceResolverComponent.Device.RasterizerState = _rasterStates[RasterizerStateEnum.SelectedFaces];
            foreach (var item in _renderItems[RenderBuckedId.Selection])
                item.Draw(_deviceResolverComponent.Device, commonShaderParameters);

            foreach (var item in _renderItems[RenderBuckedId.Line])
                item.Draw(_deviceResolverComponent.Device, commonShaderParameters);

            foreach (var item in _renderItems[RenderBuckedId.ConstantDebugLine])
                item.Draw(_deviceResolverComponent.Device, commonShaderParameters);

            //var _halfRes = true;
            //int w = _deviceResolverComponent.Device.Viewport.Width;
            //int h = _deviceResolverComponent.Device.Viewport.Height;
            //
            //if (_halfRes)
            //{
            //    w /= 2;
            //    h /= 2;
            //}
            //
            //
            ////
            //// Configure buffers
            //RenderTarget2D glowScreen = new RenderTarget2D();
            //_deviceResolverComponent.Device.SetRenderTarget(glowScreen);
            //
            //foreach (var item in _renderItems[RenderBuckedId.Glow])
            //    item.DrawGlowPass(_deviceResolverComponent.Device, commonShaderParameters);
            //
            //Texture2D bloom = _bloomFilter.Draw(glowScreen, w, h);
            //
            _deviceResolverComponent.Device.SetRenderTarget(s.RenderTarget as RenderTarget2D);
            _resourceLib.CommonSpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive);
            _resourceLib.CommonSpriteBatch.Draw(_screen, new Rectangle(0, 0, _deviceResolverComponent.Device.Viewport.Width/2, _deviceResolverComponent.Device.Viewport.Height/2), Color.White);
            //_spriteBatch.Draw(bloom, new Rectangle(0, 0, _width, _height), Color.White);
            _resourceLib.CommonSpriteBatch.End();
            // Blur and what not
            //------------------------


            _deviceResolverComponent.Device.DepthStencilState = DepthStencilState.Default;
            _deviceResolverComponent.Device.RasterizerState = RasterizerState.CullNone;
        }

        public void Dispose()
        {
            _blendSPriteBatch.Dispose();
            _bloomFilter.Dispose();
            _screen.Dispose();

            _renderItems.Clear();
            foreach (var item in _rasterStates.Values)
                item.Dispose();
            _rasterStates.Clear();
        }
    }
}
