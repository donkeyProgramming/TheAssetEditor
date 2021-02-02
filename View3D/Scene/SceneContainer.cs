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
        BasicEffect _basicEffect;
        BasicEffect _wireframeEffect;
        BasicEffect _selectedFaceEffect;

        RasterizerState _selectedFaceState;
        bool _drawWireFrame = false;


        ArcBallCamera _camera;
        SceneManager _sceneManager;
        SelectionManager _selectionManager;

        protected override void Initialize()
        {
            _disposed = false;
            new WpfGraphicsDeviceService(this);

            _basicEffect = new BasicEffect(GraphicsDevice);
            _basicEffect.DiffuseColor = new Vector3(1.0f, 1.0f, 1.0f);
            _basicEffect.EnableDefaultLighting(); ;// ApplyLightToShader(_basicEffect);

            _wireframeState = new RasterizerState();
            _wireframeState.FillMode = FillMode.WireFrame;
            _wireframeState.CullMode = CullMode.CullClockwiseFace;
            _wireframeState.DepthBias = -0.00008f;
            _wireframeState.DepthClipEnable = true;

            _selectedFaceState = new RasterizerState();
            _selectedFaceState.FillMode = FillMode.Solid;
            _selectedFaceState.CullMode = CullMode.CullClockwiseFace;
            _selectedFaceState.DepthBias = -0.00008f;
            _wireframeState.DepthClipEnable = true;

            _wireframeEffect = new BasicEffect(GraphicsDevice);
            _wireframeEffect.DiffuseColor = Vector3.Zero;

            _selectedFaceEffect = new BasicEffect(GraphicsDevice);
            _selectedFaceEffect.DiffuseColor = new Vector3(1,0,0);
            _selectedFaceEffect.SpecularColor = new Vector3(1, 0, 0);
            _selectedFaceEffect.EnableDefaultLighting();// ApplyLightToShader(_selectedFaceEffect);

            _camera = GetComponent<ArcBallCamera>();
            _sceneManager = GetComponent<SceneManager>();
            _selectionManager = GetComponent<SelectionManager>();

            base.Initialize();
        }


        protected override void Draw(GameTime time)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);
            if (_sceneManager == null)
                return;

            GraphicsDevice.DepthStencilState = DepthStencilState.Default;
            GraphicsDevice.RasterizerState = RasterizerState.CullNone;

            _basicEffect.Projection = _camera.ProjectionMatrix;
            _basicEffect.View = _camera.ViewMatrix;
           foreach (var item in _sceneManager.RenderItems)
           {
                _basicEffect.World = item.ModelMatrix;
                item.Geometry.ApplyMesh(_basicEffect, GraphicsDevice);
           }


            if(_selectionManager.GeometrySelectionMode == GeometrySelectionMode.Face)
            {
                var faceModeItems = _selectionManager.CurrentSelection();
                if (faceModeItems.Count() != 0)
                {
                    _selectedFaceEffect.Projection = _camera.ProjectionMatrix;
                    _selectedFaceEffect.View = _camera.ViewMatrix;

                    GraphicsDevice.RasterizerState = _selectedFaceState;
                    foreach (var item in faceModeItems)
                    {
                        _selectedFaceEffect.World = item.ModelMatrix;
                        item.Geometry.ApplyMeshPart(_selectedFaceEffect, GraphicsDevice, _selectionManager.CurrentFaceSelection());
                    }

                    _wireframeEffect.Projection = _camera.ProjectionMatrix;
                    _wireframeEffect.View = _camera.ViewMatrix;

                    GraphicsDevice.RasterizerState = _wireframeState;
                    foreach (var item in faceModeItems)
                    {
                        _wireframeEffect.World = item.ModelMatrix;
                        item.Geometry.ApplyMesh(_wireframeEffect, GraphicsDevice);
                    }
                }
            }

            base.Draw(time);
        }


        protected override void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            Components.Clear();
            _disposed = true;

            _basicEffect.Dispose();
            _basicEffect = null;

            base.Dispose(disposing);
        }
    }
}
