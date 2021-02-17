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
using View3D.Rendering.RenderItems;
using View3D.SceneNodes;

namespace View3D.Scene
{

    public class SceneContainer : WpfGame
    {
        private bool _disposed;


        SceneManager _sceneManager;
        SelectionManager _selectionManager;


        RenderEngineComponent _renderEngine;

        public VertexInstanceMesh VertexRenderer { get; set; }

        protected override void Initialize()
        {
            _disposed = false;
            new WpfGraphicsDeviceService(this);


            _sceneManager = GetComponent<SceneManager>();
            _selectionManager = GetComponent<SelectionManager>();
            _renderEngine = GetComponent<RenderEngineComponent>();




            base.Initialize();
        }



        protected override void LoadContent()
        {
            _bondingBoxRenderer = new BoundingBoxRenderer(Content);
           // _lineShader = Content.Load<Effect>("Shaders\\LineShader");

            VertexRenderer = new VertexInstanceMesh();
            VertexRenderer.Initialize(GraphicsDevice, Content, 0);
            base.LoadContent();
        }

        BoundingBoxRenderer _bondingBoxRenderer;

        protected override void Draw(GameTime time)
        {

            //DrawBasicSceneHirarchy(_sceneManager.RootNode, GraphicsDevice, Matrix.Identity);

            var selectionState = _selectionManager.GetState();

            // Draw selection bounding box
            if (selectionState is ObjectSelectionState objectSelectionState)
            {
                foreach (var item in objectSelectionState.CurrentSelection())
                    _renderEngine.AddRenderItem(RenderBuckedId.Selection, new BoundingBoxRenderItem() { BoundingBox = item.Geometry.BoundingBox, World = item.ModelMatrix, BoundingBoxRenderer = _bondingBoxRenderer });
            }

            if (selectionState is FaceSelectionState selectionFaceState &&  selectionFaceState.RenderObject is MeshNode meshNode)
            {
                _renderEngine.AddRenderItem(RenderBuckedId.Selection, new FaceRenderItem() {Node = meshNode, World = meshNode.ModelMatrix, SelectedFaces = selectionFaceState.CurrentSelection() });
                _renderEngine.AddRenderItem(RenderBuckedId.Wireframe, new WireFrameRenderItem() { World = meshNode.ModelMatrix, Node = meshNode });
            }

            if (selectionState is VertexSelectionState selectionVertexState && selectionVertexState.RenderObject != null)
            {
                var vertexObject = selectionVertexState.RenderObject as MeshNode;
                _renderEngine.AddRenderItem(RenderBuckedId.Selection, new VertexRenderItem() { Node = vertexObject, World = vertexObject.ModelMatrix, SelectedVertices = selectionVertexState.SelectedVertices, VertexRenderer = VertexRenderer });
                _renderEngine.AddRenderItem(RenderBuckedId.Wireframe, new WireFrameRenderItem() { World = Matrix.Identity, Node = vertexObject });
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
