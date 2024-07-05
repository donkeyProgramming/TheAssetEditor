using GameWorld.Core.Rendering;
using GameWorld.Core.Utility;
using GameWorld.WpfWindow.ResourceHandling;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Shared.Core.Services;
using System;
using System.Collections.Generic;

namespace GameWorld.Core.Components.Rendering
{
    public enum RenderBuckedId
    {
        Normal,
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
        }

        public override void Initialize()
        {
            RebuildRasterStates(_cullingEnabled, _bigSceneDepthBiasMode);
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

            _deviceResolverComponent.Device.DepthStencilState = DepthStencilState.Default;
            _deviceResolverComponent.Device.RasterizerState = RasterizerState.CullNone;
        }

        public void Dispose()
        {
            _renderItems.Clear();
            foreach (var item in _rasterStates.Values)
                item.Dispose();
            _rasterStates.Clear();
        }
    }
}
