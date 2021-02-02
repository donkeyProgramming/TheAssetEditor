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
        private BasicEffect _basicEffect;

        private bool _disposed;

        RasterizerState _wireframeState;


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
               GraphicsDevice.SetVertexBuffer(item.Geometry.VertexBuffer);
               _basicEffect.World = item.ModelMatrix;
                
               foreach (var pass in _basicEffect.CurrentTechnique.Passes)
               {
                   pass.Apply();
                   GraphicsDevice.DrawPrimitives(PrimitiveType.TriangleList, 0, 12);
               }
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
                        GraphicsDevice.SetVertexBuffer(item.Geometry.VertexBuffer);

                        _selectedFaceEffect.World = item.ModelMatrix;
                        foreach (var pass in _selectedFaceEffect.CurrentTechnique.Passes)
                        {
                            pass.Apply();
                            GraphicsDevice.DrawPrimitives(PrimitiveType.TriangleList, 0, 1);
                        }
                    }

                    _wireframeEffect.Projection = _camera.ProjectionMatrix;
                    _wireframeEffect.View = _camera.ViewMatrix;

                    GraphicsDevice.RasterizerState = _wireframeState;
                    foreach (var item in faceModeItems)
                    {
                        GraphicsDevice.SetVertexBuffer(item.Geometry.VertexBuffer);

                        _wireframeEffect.World = item.ModelMatrix;
                        foreach (var pass in _wireframeEffect.CurrentTechnique.Passes)
                        {
                            pass.Apply();
                            GraphicsDevice.DrawPrimitives(PrimitiveType.TriangleList, 0, 12);
                        }

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
