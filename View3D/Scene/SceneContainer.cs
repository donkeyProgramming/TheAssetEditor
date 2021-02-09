using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.Framework.WpfInterop;
using MonoGame.Framework.WpfInterop.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using View3D.Components.Component;
using View3D.Components.Component.Selection;
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

        public VertexInstanceMesh VertexRenderer { get; set; }

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

        Effect _lineShader;

        protected override void LoadContent()
        {
            _bondingBoxRenderer = new BoundingBoxRenderer();
            _lineShader = Content.Load<Effect>("Shaders\\LineShader");

            VertexRenderer = new VertexInstanceMesh();
            VertexRenderer.Initialize(GraphicsDevice, Content, 0);
            base.LoadContent();
        }

        BoundingBoxRenderer _bondingBoxRenderer;
        protected override void Draw(GameTime time)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);
            if (_sceneManager == null)
                return;

            GraphicsDevice.DepthStencilState = DepthStencilState.Default;
            GraphicsDevice.RasterizerState = RasterizerState.CullNone;

  
            CommonShaderParameters commonShaderParameters = new CommonShaderParameters()
            {
                Projection = _camera.ProjectionMatrix,
                View = _camera.ViewMatrix,
                CameraPosition = _camera.Position,
                CameraLookAt = _camera.LookAt,
                EnvRotate = 0
            };


            var selectionState = _selectionManager.GetState();

            GraphicsDevice.DepthStencilState = DepthStencilState.Default;
            GraphicsDevice.RasterizerState = RasterizerState.CullNone;

            DrawBasicSceneHirarchy(_sceneManager.RootNode, GraphicsDevice, Matrix.Identity, commonShaderParameters);

            // Draw selection bounding box
            if (selectionState is ObjectSelectionState objectSelectionState)
            { 
                foreach(var item in objectSelectionState.CurrentSelection())
                    _bondingBoxRenderer.Render(_lineShader, GraphicsDevice, commonShaderParameters, item.Geometry.BoundingBox, item.ModelMatrix);
            }
            
            if (selectionState is FaceSelectionState selectionFaceState &&  selectionFaceState.RenderObject is MeshNode meshNode)
            {
                GraphicsDevice.RasterizerState = _selectedFaceState;
                meshNode.DrawSelectedFaces(GraphicsDevice, meshNode.ModelMatrix, commonShaderParameters, selectionFaceState.CurrentSelection());
                
                GraphicsDevice.RasterizerState = _wireframeState;
                meshNode.DrawWireframeOverlay(GraphicsDevice, meshNode.ModelMatrix, commonShaderParameters);
            }

            if (selectionState is VertexSelectionState selectionVertexState && selectionVertexState.RenderObject != null)
            {
                GraphicsDevice.RasterizerState = _selectedFaceState;
                var vertexObject = selectionVertexState.RenderObject as MeshNode;
                VertexRenderer.Update(vertexObject.Geometry, vertexObject.ModelMatrix, vertexObject.Orientation, commonShaderParameters.CameraPosition, selectionVertexState.SelectedVertices);
                VertexRenderer.Draw(commonShaderParameters.View, commonShaderParameters.Projection, GraphicsDevice, new Vector3(0, 1, 0));

                GraphicsDevice.RasterizerState = _wireframeState;
                vertexObject.DrawWireframeOverlay(GraphicsDevice, Matrix.Identity, commonShaderParameters);
            }

            GraphicsDevice.DepthStencilState = DepthStencilState.Default;
            GraphicsDevice.RasterizerState = RasterizerState.CullNone;

            base.Draw(time);
        }


        void DrawBasicSceneHirarchy(SceneNode root, GraphicsDevice device, Matrix parentMatrix, CommonShaderParameters commonShaderParameters)
        {
            if (root.IsVisible)
            {
                if (root is IDrawableNode drawableNode)
                    drawableNode.DrawBasic(GraphicsDevice, parentMatrix, commonShaderParameters);

                foreach (var child in root.Children)
                    DrawBasicSceneHirarchy(child, device, parentMatrix * child.ModelMatrix, commonShaderParameters);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            Components.Clear();
            _disposed = true;

            base.Dispose(disposing);
        }


        class BoundingBoxRenderer
        {
            VertexPosition[] _originalVertecies;

            public void CreateLineList((Vector3, Vector3)[] lines)
            {
                _originalVertecies = new VertexPosition[lines.Length * 2];
                for (int i = 0; i < lines.Length; i++)
                {
                    _originalVertecies[i * 2] = new VertexPosition(lines[i].Item1);
                    _originalVertecies[i * 2 + 1] = new VertexPosition(lines[i].Item2);
                }
            }

            public virtual void Render(Effect effect, GraphicsDevice device, CommonShaderParameters commonShaderParameters, BoundingBox b, Matrix ModelMatrix)
            {
                var corners = b.GetCorners();
                var data = new (Vector3, Vector3)[12];
                data[0] = (corners[0], corners[1]);
                data[1] = (corners[2], corners[3]);
                data[2] = (corners[0], corners[3]);
                data[3] = (corners[1], corners[2]);

                data[4] = (corners[4], corners[5]);
                data[5] = (corners[6], corners[7]);
                data[6] = (corners[4], corners[7]);
                data[7] = (corners[5], corners[6]);

                data[8] = (corners[0], corners[4]);
                data[9] = (corners[1], corners[5]);
                data[10] = (corners[2], corners[6]);
                data[11] = (corners[3], corners[7]);

                CreateLineList(data);

                effect.Parameters["View"].SetValue(commonShaderParameters.View);
                effect.Parameters["Projection"].SetValue(commonShaderParameters.Projection);
                effect.Parameters["World"].SetValue(Matrix.CreateScale(1.05f) * ModelMatrix);

                foreach (var pass in effect.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    device.DrawUserPrimitives(PrimitiveType.LineList, _originalVertecies, 0, _originalVertecies.Count() / 2);
                }
            }
        }
    }
}
