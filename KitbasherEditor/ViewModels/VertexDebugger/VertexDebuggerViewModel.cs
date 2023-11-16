using CommonControls.MathViews;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monogame.WpfInterop.Common;
using System;
using System.Collections.ObjectModel;
using View3D.Components;
using View3D.Components.Component.Selection;
using View3D.Components.Rendering;
using View3D.Rendering;
using View3D.Rendering.Geometry;
using View3D.Rendering.RenderItems;
using View3D.SceneNodes;
using View3D.Utility;

namespace KitbasherEditor.ViewModels.VertexDebugger
{
    class VertexDebuggerViewModel : BaseComponent, IDisposable
    {
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

        private readonly RenderEngineComponent _renderEngineComponent;
        private readonly ResourceLibary _resourceLibary;
        private readonly SelectionManager _selectionManager;
        private readonly EventHub _eventHub;

        public VertexDebuggerViewModel(RenderEngineComponent renderEngineComponent,
            ResourceLibary resourceLibary,
            SelectionManager selectionManager,
            EventHub eventHub)
        {
            _renderEngineComponent = renderEngineComponent;
            _resourceLibary = resourceLibary;
            _selectionManager = selectionManager;
            _eventHub = eventHub;

            _eventHub.Register<SelectionChangedEvent>(OnSelectionChanged);
        }

        public override void Initialize()
        {
            _lineShader = _resourceLibary.GetStaticEffect(ShaderTypes.Line);
            _lineRenderer = new LineMeshRender(_resourceLibary);

            Refresh();
            base.Initialize();
        }

        public void Refresh()
        {
            VertexList.Clear();
            SelectedVertex = null;

            if (_selectionManager.GetState() is VertexSelectionState selection)
            {
                var mesh = selection.GetSingleSelectedObject() as Rmv2MeshNode;
                var vertexList = selection.SelectedVertices;
                foreach (var vertexIndex in vertexList)
                {
                    var vertexInfo = (mesh.Geometry as MeshObject).GetVertexExtented(vertexIndex);

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

                        Position = vertexInfo.Position
                    });
                }
            }
        }

        public override void Draw(GameTime gameTime)
        {
            _lineRenderer.Clear();

            var selection = _selectionManager.GetState<VertexSelectionState>();
            if (selection != null)
            {
                var mesh = selection.GetSingleSelectedObject() as Rmv2MeshNode;

                if (SelectedVertex != null)
                {
                    var bb = BoundingBox.CreateFromSphere(new BoundingSphere(mesh.Geometry.GetVertexById(SelectedVertex.Id), 0.05f));
                    _renderEngineComponent.AddRenderItem(RenderBuckedId.Normal, new BoundingBoxRenderItem(_lineShader, bb, Color.White));
                }

                var vertexList = selection.SelectedVertices;
                foreach (var vertexIndex in vertexList)
                {
                    var vertexInfo = mesh.Geometry.GetVertexExtented(vertexIndex);
                    var scale = (float)DebugScale.Value;
                    var pos = vertexInfo.Position3();
                    _lineRenderer.AddLine(pos, pos + vertexInfo.Normal * scale, Color.Red);
                    _lineRenderer.AddLine(pos, pos + vertexInfo.BiNormal * scale, Color.Green);
                    _lineRenderer.AddLine(pos, pos + vertexInfo.Tangent * scale, Color.Blue);
                }

                _renderEngineComponent.AddRenderItem(RenderBuckedId.Normal, new LineRenderItem() { LineMesh = _lineRenderer, ModelMatrix = mesh.ModelMatrix * Matrix.CreateTranslation(mesh.Material.PivotPoint) });
            }
        }

        public void Dispose()
        {
            _eventHub.UnRegister<SelectionChangedEvent>(OnSelectionChanged);
            _lineRenderer.Dispose();
        }

        void OnSelectionChanged(SelectionChangedEvent notification) => Refresh();
    }
}
