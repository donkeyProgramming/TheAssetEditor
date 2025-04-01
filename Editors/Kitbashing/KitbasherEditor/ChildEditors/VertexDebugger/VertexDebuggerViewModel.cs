using System.Collections.ObjectModel;
using GameWorld.Core.Components;
using GameWorld.Core.Components.Rendering;
using GameWorld.Core.Components.Selection;
using GameWorld.Core.Rendering;
using GameWorld.Core.Rendering.Geometry;
using GameWorld.Core.SceneNodes;
using Microsoft.Xna.Framework;
using Serilog;
using Shared.Core.ErrorHandling;
using Shared.Core.Events;
using Shared.Ui.BaseDialogs.MathViews;

namespace Editors.KitbasherEditor.ChildEditors.VertexDebugger
{
    public class VertexDebuggerViewModel : BaseComponent, IDisposable
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
        private readonly ILogger _logger = Logging.Create<VertexDebuggerViewModel>();

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

                        Position = vertexInfo.Position,

                        Uv0 = vertexInfo.TextureCoordinate,
                        Uv1 = vertexInfo.TextureCoordinate1,
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
                    _renderEngineComponent.AddRenderLines(LineHelper.AddBoundingBox(bb, Color.Black, Vector3.Zero));
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

                    _renderEngineComponent.AddRenderLines(LineHelper.AddLine(transformedPos, transformedNormal, Color.Red));
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

        internal void ShowStatistics()
        {
            var minUv0 = new Vector2(10000, 10000);
            var maxUv0 = new Vector2(-10000, -10000);
            var minUv1 = new Vector2(10000, 10000);
            var maxUv1 = new Vector2(-10000, -10000);

            foreach (var vert in VertexList)
            {
                minUv0 = Vector2.Min(vert.Uv0, minUv0);
                maxUv0 = Vector2.Max(vert.Uv0, maxUv0);

                minUv1 = Vector2.Min(vert.Uv1, minUv1);
                maxUv1 = Vector2.Max(vert.Uv1, maxUv1);
            }

            var str = "Mesh statistics\n";
            str += $"   Vertex count = {VertexList.Count}\n";
            str += $"   Uv0 = Min[{minUv0}] Max[{maxUv0}] \n";
            str += $"   Uv1 = Min[{minUv1}] Max[{maxUv1}] \n";

            _logger.Here().Information(str);    
        }

        void GetMinMaxUv()
        { 
        
        }
    }
}
