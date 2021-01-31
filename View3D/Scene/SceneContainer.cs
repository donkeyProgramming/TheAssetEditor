using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.Framework.WpfInterop;
using MonoGame.Framework.WpfInterop.Input;
using System;
using System.Collections.Generic;
using System.Text;
using View3D.Rendering;
using View3D.Rendering.Geometry;

namespace View3D.Scene
{

    public class SceneManager
    { 
        public List<RenderItem> RenderItems = new List<RenderItem>();
    }

    public class SelectionManager
    {
        ArcBallCamera _camera;
        Input.Keyboard _keyboard;
        WpfMouse _mouse;
        GraphicsDevice _device;
        SceneManager _sceneManger;

        public SelectionManager(ArcBallCamera camera, Input.Keyboard keyboard, WpfMouse mouse, GraphicsDevice device, SceneManager sceneManger)
        {
            _camera = camera;
            _keyboard = keyboard;
            _mouse = mouse;
            _device = device;
            _sceneManger = sceneManger;
        }

        public void Update()
        {
            if (_keyboard.IsKeyReleased(Keys.Space))
            {
                Vector2 mouseLocation = new Vector2(_mouse.GetState().X, _mouse.GetState().Y);
                var ray = CreateCameraRay(mouseLocation, _camera.ViewMatrix, _camera.ProjectionMatrix, _device.Viewport);

                foreach (var item in _sceneManger.RenderItems)
                {
                    var distance = item.Geometry.Intersect(item.ModelMatrix, ray);
                    if (distance != null)
                    {

                    }
                }
            }
        }

        Ray CreateCameraRay(Vector2 mouseLocation, Matrix view, Matrix projection, Viewport viewport)
        {
            Vector3 nearPoint = viewport.Unproject(new Vector3(mouseLocation.X,
                   mouseLocation.Y, 0.0f),
                   projection,
                   view,
                   Matrix.Identity);

            Vector3 farPoint = viewport.Unproject(new Vector3(mouseLocation.X,
                    mouseLocation.Y, 1.0f),
                    projection,
                    view,
                    Matrix.Identity);

            Vector3 direction = farPoint - nearPoint;
            direction.Normalize();

            return new Ray(nearPoint, direction);
        }
    }

    public class SceneContainer : WpfGame
    {
        private BasicEffect _basicEffect;
        private MouseState _mouseState;

        private bool _disposed;

        ArcBallCamera _camera;

        Input.Keyboard _keyboard;
        WpfMouse _mouse;

        IGeometry _cubeMesh;

        RasterizerState _wireframeState;
        BasicEffect _wireframeEffect;
        bool _drawWireFrame = false;
        SceneManager _sceneManager = new SceneManager();
        SelectionManager _selectionManager;

        protected override void LoadContent()
        {
            _camera = new ArcBallCamera(1, new Vector3(0), 10, GraphicsDevice);
            _keyboard = new Input.Keyboard(new WpfKeyboard(this));

            _selectionManager = new SelectionManager(_camera, _keyboard, _mouse, GraphicsDevice, _sceneManager);
        }

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
                    // points from the light to the origin of the scene
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

            _cubeMesh = new CubeMesh(GraphicsDevice);
            _sceneManager.RenderItems.Add(new RenderItem(_cubeMesh, Matrix.CreateScale(.5f) * Matrix.CreateTranslation(2, 0, 0)));
            _sceneManager.RenderItems.Add(new RenderItem(_cubeMesh, Matrix.CreateScale(.5f) * Matrix.CreateTranslation(-2, 0, 0)));
            _sceneManager.RenderItems.Add(new RenderItem(_cubeMesh, Matrix.CreateScale(.5f) * Matrix.CreateTranslation(0, 0, 0)));

            _mouse = new WpfMouse(this);
            _mouse.CaptureMouseWithin = true;




     
            _wireframeState = new RasterizerState();
            _wireframeState.FillMode = FillMode.WireFrame;
            _wireframeState.CullMode = CullMode.CullClockwiseFace;
            _wireframeState.DepthBias = -0.00008f;
            _wireframeState.DepthClipEnable = true;

            _wireframeEffect = new BasicEffect(GraphicsDevice);
            _wireframeEffect.DiffuseColor = Vector3.Zero;


          

            base.Initialize();
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



        protected override void Update(GameTime gameTime)
        {
            _mouseState = _mouse.GetState();
            _keyboard.Update();
            _camera.Update(_mouseState, _keyboard);

            if (_keyboard.IsKeyReleased(Keys.F5))
                _drawWireFrame = !_drawWireFrame;

            _selectionManager.Update();
            base.Update(gameTime);
        }


        protected override void Draw(GameTime time)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);
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
    }
}
