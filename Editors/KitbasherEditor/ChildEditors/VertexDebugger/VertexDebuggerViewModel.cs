using System.Collections.ObjectModel;
using GameWorld.Core.Components;
using GameWorld.Core.Components.Rendering;
using GameWorld.Core.Components.Selection;
using GameWorld.Core.Rendering;
using GameWorld.Core.Rendering.Geometry;
using GameWorld.Core.SceneNodes;
using Microsoft.Xna.Framework;
using Shared.Core.Events;
using Shared.Ui.BaseDialogs.MathViews;

namespace KitbasherEditor.ViewModels.VertexDebugger
{
    class VertexDebuggerViewModel : BaseComponent, IDisposable
    {
        public ObservableCollection<VertexInstance> VertexList { get; set; } = [];

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


        private readonly RenderEngineComponent _renderEngineComponent;
        private readonly SelectionManager _selectionManager;
        private readonly IEventHub _eventHub;

        public VertexDebuggerViewModel(
            RenderEngineComponent renderEngineComponent,
            SelectionManager selectionManager,
            IEventHub eventHub)
        {
            _renderEngineComponent = renderEngineComponent;
            _selectionManager = selectionManager;
            _eventHub = eventHub;

            _eventHub.Register<SelectionChangedEvent>(this, OnSelectionChanged);
        }

        public override void Initialize()
        {
            Refresh();
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
            var selection = _selectionManager.GetState<VertexSelectionState>();
            if (selection != null)
            {
                var mesh = selection.GetSingleSelectedObject() as Rmv2MeshNode;

                if (SelectedVertex != null)
                {
                    var bb = BoundingBox.CreateFromSphere(new BoundingSphere(mesh.Geometry.GetVertexById(SelectedVertex.Id), 0.05f));
                    _renderEngineComponent.AddRenderLines(LineHelper.AddBoundingBox(bb, Color.Black));
                }

                var modelMatrix = mesh.ModelMatrix * Matrix.CreateTranslation(mesh.PivotPoint);
                var vertexList = selection.SelectedVertices;
                foreach (var vertexIndex in vertexList)
                {
                    var vertexInfo = mesh.Geometry.GetVertexExtented(vertexIndex);
                    var scale = (float)DebugScale.Value;
                    var pos = vertexInfo.Position3();

                    var transformedPos = Vector3.Transform(pos, modelMatrix);
                    var transformedNormal = Vector3.Transform(pos + vertexInfo.Normal * scale, modelMatrix);
                    var transformedBiNormal = Vector3.Transform(pos + vertexInfo.BiNormal * scale, modelMatrix);
                    var transformedTangent = Vector3.Transform(pos + vertexInfo.Tangent * scale, modelMatrix);

                    _renderEngineComponent.AddRenderLines(LineHelper.AddLine(transformedPos, transformedNormal , Color.Red));
                    _renderEngineComponent.AddRenderLines(LineHelper.AddLine(transformedPos, transformedBiNormal, Color.Green));
                    _renderEngineComponent.AddRenderLines(LineHelper.AddLine(transformedPos, transformedTangent, Color.Blue));
                }
            }
        }

        public void Dispose()
        {
            _eventHub.UnRegister(this);
        }

        void OnSelectionChanged(SelectionChangedEvent notification) => Refresh();
    }
}
