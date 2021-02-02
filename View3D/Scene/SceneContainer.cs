using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.Framework.WpfInterop;
using MonoGame.Framework.WpfInterop.Input;
using System;
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
        bool _drawWireFrame = false;


        ArcBallCamera _camera;
        SceneManager _sceneManager;


        protected override void Initialize()
        {


            _disposed = false;
            new WpfGraphicsDeviceService(this);

            _basicEffect = new BasicEffect(GraphicsDevice);
            _basicEffect.AmbientLightColor = new Vector3(0.1f, 0.1f, 0.1f);
            _basicEffect.DiffuseColor = new Vector3(1.0f, 1.0f, 1.0f);
            _basicEffect.SpecularColor = new Vector3(0.25f, 0.25f, 0.25f);
            _basicEffect.SpecularPower = 5.0f;
            _basicEffect.Alpha = 1.0f;

            _basicEffect.LightingEnabled = true;
            if (_basicEffect.LightingEnabled)
            {
                _basicEffect.DirectionalLight0.Enabled = true; // enable each light individually
                if (_basicEffect.DirectionalLight0.Enabled)
                {
                    // x direction
                    _basicEffect.DirectionalLight0.DiffuseColor = new Vector3(1, 0, 0); // range is 0 to 1
                    _basicEffect.DirectionalLight0.Direction = Vector3.Normalize(new Vector3(-1, 0, 0));
                    _basicEffect.DirectionalLight0.SpecularColor = Vector3.One;
                }

                _basicEffect.DirectionalLight1.Enabled = true;
                if (_basicEffect.DirectionalLight1.Enabled)
                {
                    // y direction
                    _basicEffect.DirectionalLight1.DiffuseColor = new Vector3(0, 0.75f, 0);
                    _basicEffect.DirectionalLight1.Direction = Vector3.Normalize(new Vector3(0, -1, 0));
                    _basicEffect.DirectionalLight1.SpecularColor = Vector3.One;
                }

                _basicEffect.DirectionalLight2.Enabled = true;
                if (_basicEffect.DirectionalLight2.Enabled)
                {
                    // z direction
                    _basicEffect.DirectionalLight2.DiffuseColor = new Vector3(0, 0, 0.5f);
                    _basicEffect.DirectionalLight2.Direction = Vector3.Normalize(new Vector3(0, 0, -1));
                    _basicEffect.DirectionalLight2.SpecularColor = Vector3.One;
                }
            }

            _wireframeState = new RasterizerState();
            _wireframeState.FillMode = FillMode.WireFrame;
            _wireframeState.CullMode = CullMode.CullClockwiseFace;
            _wireframeState.DepthBias = -0.00008f;
            _wireframeState.DepthClipEnable = true;

            _wireframeEffect = new BasicEffect(GraphicsDevice);
            _wireframeEffect.DiffuseColor = Vector3.Zero;

            _camera = GetComponent<ArcBallCamera>();
            _sceneManager = GetComponent<SceneManager>();

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

            if (_drawWireFrame)
            {
                _wireframeEffect.Projection = _camera.ProjectionMatrix;
                _wireframeEffect.View = _camera.ViewMatrix;

                GraphicsDevice.RasterizerState = _wireframeState;
                foreach (var item in _sceneManager.RenderItems)
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
