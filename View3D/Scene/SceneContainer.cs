using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.Framework.WpfInterop;
using MonoGame.Framework.WpfInterop.Input;
using System;
using System.Text;
using View3D.Rendering;
using View3D.Rendering.Geometry;

namespace View3D.Scene
{
    public delegate void SceneInitializedDelegate(SceneContainer scene);
    public class SceneContainer : WpfGame
    {
        public event SceneInitializedDelegate SceneInitialized;
        private BasicEffect _basicEffect;


        private bool _disposed;

        public SceneManager SceneManager { get; set; }
        //public Input.Keyboard Keyboard { get; set; }
        //public Input.Mouse Mouse { get; set; }

        IGeometry _cubeMesh;

        RasterizerState _wireframeState;
        BasicEffect _wireframeEffect;
        bool _drawWireFrame = false;
       

        protected override void Initialize()
        {

//            Components

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

            //_cubeMesh = new CubeMesh(GraphicsDevice);
            //SceneManager = new SceneManager();
            //SceneManager.RenderItems.Add(new RenderItem(_cubeMesh, Matrix.CreateScale(.5f) * Matrix.CreateTranslation(2, 0, 0)) { Id = "Item0"});
            //SceneManager.RenderItems.Add(new RenderItem(_cubeMesh, Matrix.CreateScale(.5f) * Matrix.CreateTranslation(-2, 0, 0)) { Id = "Item1" });
            //SceneManager.RenderItems.Add(new RenderItem(_cubeMesh, Matrix.CreateScale(.5f) * Matrix.CreateTranslation(0, 0, 0)) { Id = "Item2" });


     
            _wireframeState = new RasterizerState();
            _wireframeState.FillMode = FillMode.WireFrame;
            _wireframeState.CullMode = CullMode.CullClockwiseFace;
            _wireframeState.DepthBias = -0.00008f;
            _wireframeState.DepthClipEnable = true;

            _wireframeEffect = new BasicEffect(GraphicsDevice);
            _wireframeEffect.DiffuseColor = Vector3.Zero;


          

            base.Initialize();
            SceneInitialized?.Invoke(this);
        }






        protected override void Update(GameTime gameTime)
        {
            //Keyboard.Update();
            //Mouse.Update();
            //Camera.Update(Mouse, Keyboard);
            //
            //if (Keyboard.IsKeyReleased(Keys.F5))
            //    _drawWireFrame = !_drawWireFrame;

            base.Update(gameTime);
        }

        public ArcBallCamera Camera;
        protected override void Draw(GameTime time)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);
            if (SceneManager == null)
                return;

            GraphicsDevice.DepthStencilState = DepthStencilState.Default;
            GraphicsDevice.RasterizerState = RasterizerState.CullNone;

            _basicEffect.Projection = Camera.ProjectionMatrix;
            _basicEffect.View = Camera.ViewMatrix;
           foreach (var item in SceneManager.RenderItems)
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
                _wireframeEffect.Projection = Camera.ProjectionMatrix;
                _wireframeEffect.View = Camera.ViewMatrix;

                GraphicsDevice.RasterizerState = _wireframeState;
                foreach (var item in SceneManager.RenderItems)
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
