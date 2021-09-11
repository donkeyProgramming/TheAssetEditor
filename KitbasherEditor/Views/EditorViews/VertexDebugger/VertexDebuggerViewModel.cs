using CommonControls.MathViews;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Framework.WpfInterop;
using System;
using System.Collections.ObjectModel;
using System.Windows;
using View3D.Components;
using View3D.Components.Component.Selection;
using View3D.Components.Rendering;
using View3D.Rendering;
using View3D.Rendering.Geometry;
using View3D.Rendering.RenderItems;
using View3D.SceneNodes;
using View3D.Utility;

namespace KitbasherEditor.Views.EditorViews.VertexDebugger
{
    class VertexDebuggerViewModel : BaseComponent, IDisposable
    {
        public class VertexInstance
        {
            public int Id { get; set; }
            public Vector4 AnimIndecies { get; set; }
            public Vector4 AnimWeights { get; set; }
            public float TotalWeight { get; set; }

            public Vector3 Normal { get; set; }
            public float NormalLength { get; set; }
            public Vector3 BiNormal { get; set; }
            public float BiNormalLength { get; set; }
            public Vector3 Tangent { get; set; }
            public float TangentLength { get; set; }

        }

        public ObservableCollection<VertexInstance> VertexList { get; set; } = new ObservableCollection<VertexInstance>();
        VertexInstance _selectedVertex;
        public VertexInstance SelectedVertex
        {
            get { return _selectedVertex; }
            set { SetAndNotify(ref _selectedVertex, value); }
        }

        DoubleViewModel _debugScale = new DoubleViewModel(0.03f);
        public DoubleViewModel DebugScale
        {
            get { return _debugScale; }
            set { SetAndNotify(ref _debugScale, value); }
        }

        LineMeshRender _lineRenderer;
        Effect _lineShader;

        public VertexDebuggerViewModel(WpfGame game) : base(game)
        {
            _lineShader = GetComponent<ResourceLibary>().GetStaticEffect(ShaderTypes.Line);
            _lineRenderer = new LineMeshRender(game.Content);
            var selectionMgr = GetComponent<SelectionManager>();
            selectionMgr.SelectionChanged += SelectionMgr_SelectionChanged;
            Refresh();
        }

        private void SelectionMgr_SelectionChanged(ISelectionState state)
        {
            Refresh();
        }

        public void Refresh()
        {
            VertexList.Clear();
            SelectedVertex = null;
            var selectionMgr = GetComponent<SelectionManager>();

            if (selectionMgr.GetState() is VertexSelectionState selection)
            {
                var mesh = selection.GetSingleSelectedObject() as Rmv2MeshNode;
                var vertexList = selection.SelectedVertices;
                foreach (var vertexIndex in vertexList)
                {
                    var vertexInfo = (mesh.Geometry as Rmv2Geometry).GetVertexExtented(vertexIndex);

                    VertexList.Add(new VertexInstance()
                    { 
                        Id = vertexIndex,
                        AnimWeights = vertexInfo.BlendWeights,
                        AnimIndecies = vertexInfo.BlendIndices,
                        TotalWeight = vertexInfo.BlendWeights.X + vertexInfo.BlendWeights.Y + vertexInfo.BlendWeights.Z + vertexInfo.BlendWeights.W,

                        Normal = vertexInfo.Normal,
                        NormalLength = vertexInfo.Normal.Length(),

                        BiNormal = vertexInfo.BiNormal,
                        BiNormalLength = vertexInfo.BiNormal.Length(),

                        Tangent = vertexInfo.Tangent,
                        TangentLength = vertexInfo.Tangent.Length(),
                    });
                }
            }
        }

        public override void Draw(GameTime gameTime)
        {
            var selectionMgr = GetComponent<SelectionManager>();
            var renderEngine = GetComponent<RenderEngineComponent>();

            _lineRenderer.Clear();
            if (selectionMgr.GetState() is VertexSelectionState selection)
            {
                var mesh = selection.GetSingleSelectedObject() as Rmv2MeshNode;
              
                if (SelectedVertex != null)
                {
                    var bb = BoundingBox.CreateFromSphere(new BoundingSphere(mesh.Geometry.GetVertexById(SelectedVertex.Id), 0.01f));
                    renderEngine.AddRenderItem(RenderBuckedId.Normal, new BoundingBoxRenderItem(_lineShader, bb));
                }

                var vertexList = selection.SelectedVertices;
                foreach (var vertexIndex in vertexList)
                {
                    var vertexInfo = (mesh.Geometry as Rmv2Geometry).GetVertexExtented(vertexIndex);
                    var scale = (float)DebugScale.Value;
                    var pos = vertexInfo.Position3();
                    _lineRenderer.AddLine(pos, pos + vertexInfo.Normal * scale, Color.Blue);
                    _lineRenderer.AddLine(pos, pos + vertexInfo.BiNormal * scale, Color.Green);
                    _lineRenderer.AddLine(pos, pos + vertexInfo.Tangent * scale, Color.Red);
                }

                renderEngine.AddRenderItem(RenderBuckedId.Normal, new LineRenderItem() { LineMesh = _lineRenderer });
            }
        }

        public static void Create(IComponentManager componentManager)
        {
            var renderComp = componentManager.GetComponent<RenderEngineComponent>();

            var viewModel = new VertexDebuggerViewModel(renderComp.Game);
            componentManager.AddComponent(viewModel);

            var containingWindow = new Window();
            containingWindow.Title = "Vertex debuger";
            containingWindow.Width = 1200;
            containingWindow.Height = 1100;
            containingWindow.DataContext = viewModel;
            containingWindow.Content = new VertexDebuggerView();
            containingWindow.Closed += (x, y) => { componentManager.RemoveComponent(viewModel); viewModel.Dispose(); };
            containingWindow.Show();         
        }

        public void Dispose()
        {
            var selectionMgr = GetComponent<SelectionManager>();
            selectionMgr.SelectionChanged -= SelectionMgr_SelectionChanged;
            _lineRenderer.Dispose();
        }
    }
}
