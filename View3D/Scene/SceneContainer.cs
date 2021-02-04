using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.Framework.WpfInterop;
using MonoGame.Framework.WpfInterop.Input;
using System;
using System.Linq;
using System.Text;
using View3D.Components.Component;
using View3D.Components.Rendering;
using View3D.Rendering;
using View3D.Rendering.Geometry;

namespace View3D.Scene
{

    public class SceneContainer : WpfGame
    {
        private bool _disposed;

        RasterizerState _wireframeState;
        RasterizerState _selectedFaceState;

        ArcBallCamera _camera;
        SceneManager _sceneManager;
        SelectionManager _selectionManager;

        protected override void Initialize()
        {
            _disposed = false;
            new WpfGraphicsDeviceService(this);

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
            _sceneManager = GetComponent<SceneManager>();
            _selectionManager = GetComponent<SelectionManager>();

            base.Initialize();
        }

        Instancing instancing;

        protected override void LoadContent()
        {
            instancing = new Instancing();
            instancing.Initialize(this.GraphicsDevice);
            instancing.Load(Content);

            base.LoadContent();
        }


        protected override void Draw(GameTime time)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);
            if (_sceneManager == null)
                return;

            CommonShaderParameters commonShaderParameters = new CommonShaderParameters()
            {
                Projection = _camera.ProjectionMatrix,
                View = _camera.ViewMatrix,
                CameraPosition = _camera.Position,
                CameraLookAt = _camera.LookAt,
                EnvRotate = 0
            };

            instancing.Draw(_camera.ViewMatrix, _camera.ProjectionMatrix, GraphicsDevice);

            GraphicsDevice.DepthStencilState = DepthStencilState.Default;
            GraphicsDevice.RasterizerState = RasterizerState.CullNone;
            foreach (var item in _sceneManager.RenderItems)
                item.DrawBasic(GraphicsDevice, Matrix.Identity, commonShaderParameters);

            foreach (var item in _sceneManager.RenderItems)
                item.DrawVertexes(GraphicsDevice, Matrix.Identity, commonShaderParameters);

            var selectionState = _selectionManager.GetState();
            var selectionFaceState = selectionState as FaceSelectionState;
            if (selectionFaceState != null && selectionFaceState.RenderObject != null)
            {
                GraphicsDevice.RasterizerState = _selectedFaceState;
                selectionFaceState.RenderObject.DrawSelectedFaces(GraphicsDevice, Matrix.Identity, commonShaderParameters, selectionFaceState.CurrentSelection());
                selectionFaceState.RenderObject.DrawVertexes(GraphicsDevice, Matrix.Identity, commonShaderParameters);
                
                GraphicsDevice.RasterizerState = _wireframeState;
                selectionFaceState.RenderObject.DrawWireframeOverlay(GraphicsDevice, Matrix.Identity, commonShaderParameters);
            }

            base.Draw(time);
        }


        protected override void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            Components.Clear();
            _disposed = true;

            base.Dispose(disposing);
        }
    }
}
