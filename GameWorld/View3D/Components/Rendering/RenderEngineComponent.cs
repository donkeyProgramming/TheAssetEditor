using GameWorld.WpfWindow.ResourceHandling;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Shared.Core.Services;
using System;
using System.Collections.Generic;
using View3D.Rendering;

namespace View3D.Components.Rendering
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

        Dictionary<RasterizerStateEnum, RasterizerState> _rasterStates = new Dictionary<RasterizerStateEnum, RasterizerState>();
        ArcBallCamera _camera;
        Dictionary<RenderBuckedId, List<IRenderItem>> _renderItems = new Dictionary<RenderBuckedId, List<IRenderItem>>();
        ResourceLibrary _resourceLib;
        private readonly IDeviceResolver _deviceResolverComponent;
        ApplicationSettingsService _applicationSettingsService;

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

            _rasterStates[RasterizerStateEnum.Default] = new RasterizerState();
            _rasterStates[RasterizerStateEnum.Default].FillMode = FillMode.Solid;
            _rasterStates[RasterizerStateEnum.Default].CullMode = cullMode;
            _rasterStates[RasterizerStateEnum.Default].DepthBias = bias;
            _rasterStates[RasterizerStateEnum.Default].DepthClipEnable = true;
            _rasterStates[RasterizerStateEnum.Default].MultiSampleAntiAlias = true;

            float depthOffsetBias = 0.00005f;
            _rasterStates[RasterizerStateEnum.Wireframe] = new RasterizerState();
            _rasterStates[RasterizerStateEnum.Wireframe].FillMode = FillMode.WireFrame;
            _rasterStates[RasterizerStateEnum.Wireframe].CullMode = cullMode;
            _rasterStates[RasterizerStateEnum.Wireframe].DepthBias = bias - depthOffsetBias; ;
            _rasterStates[RasterizerStateEnum.Wireframe].DepthClipEnable = true;
            _rasterStates[RasterizerStateEnum.Wireframe].MultiSampleAntiAlias = true;

            _rasterStates[RasterizerStateEnum.SelectedFaces] = new RasterizerState();
            _rasterStates[RasterizerStateEnum.SelectedFaces].FillMode = FillMode.Solid;
            _rasterStates[RasterizerStateEnum.SelectedFaces].CullMode = CullMode.None;
            _rasterStates[RasterizerStateEnum.SelectedFaces].DepthBias = bias - depthOffsetBias;
            _rasterStates[RasterizerStateEnum.SelectedFaces].DepthClipEnable = true;
            _rasterStates[RasterizerStateEnum.SelectedFaces].MultiSampleAntiAlias = true;

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
            CommonShaderParameters commonShaderParameters = new CommonShaderParameters()
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
