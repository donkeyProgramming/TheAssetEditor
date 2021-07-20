using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Framework.WpfInterop;
using System;
using System.Collections.Generic;
using System.Text;
using View3D.Rendering;

namespace View3D.Components.Rendering
{
    public enum RenderBuckedId
    { 
        Normal,
        Wireframe,
        Selection,
        Line,
    }

    public enum RenderMode
    { 
        Cinematic,
        Simple
    }

    public interface IRenderItem
    {
        void Draw(GraphicsDevice device, CommonShaderParameters parameters);
    }

    public class RenderEngineComponent : BaseComponent, IDisposable
    {
        RasterizerState _wireframeState;
        RasterizerState _selectedFaceState;

        ArcBallCamera _camera;

        Dictionary<RenderBuckedId, List<IRenderItem>> _renderItems = new Dictionary<RenderBuckedId, List<IRenderItem>>();

        public float LightRotationDegrees { get; set; } = 180;
        public float LightIntensityMult { get; set; } = 6;

        public RenderMode RenderMode { get; set; } = RenderMode.Cinematic;

        public RenderEngineComponent(WpfGame game) : base(game)
        {
            UpdateOrder = (int)ComponentUpdateOrderEnum.RenderEngine;
            DrawOrder = (int)ComponentDrawOrderEnum.RenderEngine;

            foreach (RenderBuckedId value in Enum.GetValues(typeof(RenderBuckedId)))
                _renderItems.Add(value, new List<IRenderItem>(100));
        }

        public override void Initialize()
        {
            _wireframeState = new RasterizerState();
            _wireframeState.FillMode = FillMode.WireFrame;
            _wireframeState.CullMode = CullMode.None;
            _wireframeState.DepthBias = -0.000008f;
            _wireframeState.DepthClipEnable = true;

            _selectedFaceState = new RasterizerState();
            _selectedFaceState.FillMode = FillMode.Solid;
            _selectedFaceState.CullMode = CullMode.None;
            _selectedFaceState.DepthBias = -0.000008f;
            _wireframeState.DepthClipEnable = true;

            _camera = GetComponent<ArcBallCamera>();

            base.Initialize();
        }



        public void AddRenderItem(RenderBuckedId id, IRenderItem item)
        {
            _renderItems[id].Add(item);
        }

        public override void Update(GameTime gameTime)
        {
            foreach (RenderBuckedId value in Enum.GetValues(typeof(RenderBuckedId)))
                _renderItems[value].Clear();

            base.Update(gameTime);
        }

        public override void Draw(GameTime gameTime)
        {
            GraphicsDevice.DepthStencilState = DepthStencilState.Default;
            GraphicsDevice.RasterizerState = RasterizerState.CullNone;

            CommonShaderParameters commonShaderParameters = new CommonShaderParameters()
            {
                Projection = _camera.ProjectionMatrix,
                View = _camera.ViewMatrix,
                CameraPosition = _camera.Position,
                CameraLookAt = _camera.LookAt,
                LightRotationRadians = MathHelper.ToRadians(LightRotationDegrees),
                LightIntensityMult = LightIntensityMult
            };

            foreach (var item in _renderItems[RenderBuckedId.Normal])
                item.Draw(GraphicsDevice, commonShaderParameters);

            GraphicsDevice.RasterizerState = _wireframeState;
            foreach (var item in _renderItems[RenderBuckedId.Wireframe])
                item.Draw(GraphicsDevice, commonShaderParameters);

            GraphicsDevice.RasterizerState = _selectedFaceState;
            foreach (var item in _renderItems[RenderBuckedId.Selection])
                item.Draw(GraphicsDevice, commonShaderParameters);

            foreach (var item in _renderItems[RenderBuckedId.Line])
                item.Draw(GraphicsDevice, commonShaderParameters);

            GraphicsDevice.DepthStencilState = DepthStencilState.Default;
            GraphicsDevice.RasterizerState = RasterizerState.CullNone;
        }

        public void Dispose()
        {
            _renderItems.Clear();
            _wireframeState.Dispose();
            _selectedFaceState.Dispose();
        }
    }
}
