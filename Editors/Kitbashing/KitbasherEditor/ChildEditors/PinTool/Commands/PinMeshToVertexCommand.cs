using GameWorld.Core.Commands;
using GameWorld.Core.Components.Selection;
using GameWorld.Core.Rendering.Geometry;
using GameWorld.Core.SceneNodes;
using Microsoft.Xna.Framework;
using Serilog;
using Shared.Core.ErrorHandling;

namespace Editors.KitbasherEditor.ChildEditors.PinTool.Commands
{
    public class PinMeshToVertexCommand : ICommand
    {
        private readonly ILogger _logger = Logging.Create<PinMeshToVertexCommand>();
        private readonly SelectionManager _selectionManager;

        private ISelectionState? _selectionOldState;
        private List<MeshObject>? _originalGeos;
        private List<Rmv2MeshNode> _meshesToPin = [];
        private Rmv2MeshNode? _source;
        private int _vertexId;

        public void Configure(IEnumerable<Rmv2MeshNode> meshesToPin, Rmv2MeshNode source, int vertexId)
        {
            _meshesToPin = meshesToPin.ToList();
            _source = source;
            _vertexId = vertexId;
        }

        public string HintText { get => "Pin meshes to vertex"; }
        public bool IsMutation { get => true; }

        public PinMeshToVertexCommand(SelectionManager selectionManager)
        {
            _selectionManager = selectionManager;
        }

        public void Execute()
        {
            if (_source == null || _meshesToPin.Count == 0)
                throw new InvalidOperationException("PinMeshToVertexCommand not configured before Execute");

            _logger.Here().Information("Executing pin: {Count} meshes to vertex {VertexId} on '{Source}'", _meshesToPin.Count, _vertexId, _source.Name);

            // Create undo state
            _originalGeos = _meshesToPin.Select(x => x.Geometry.Clone()).ToList();
            _selectionOldState = _selectionManager.GetStateCopy();

            // Update the meshes
            var sourceVert = _source.Geometry.GetVertexExtented(_vertexId);
            foreach (var currentMesh in _meshesToPin)
            {
                currentMesh.Geometry.ChangeVertexType(_source.Geometry.VertexFormat, false);
                currentMesh.Geometry.UpdateSkeletonName(_source.Geometry.SkeletonName);

                currentMesh.PivotPoint = Vector3.Zero;
                for (var i = 0; i < currentMesh.Geometry.VertexCount(); i++)
                {
                    currentMesh.Geometry.SetVertexBlendIndex(i, sourceVert.BlendIndices);
                    currentMesh.Geometry.SetVertexWeights(i, sourceVert.BlendWeights);
                }

                currentMesh.Geometry.RebuildVertexBuffer();
            }
        }

        public void Undo()
        {
            if (_originalGeos == null || _selectionOldState == null)
                return;

            for (var i = 0; i < _meshesToPin.Count; i++)
            {
                _meshesToPin[i].Geometry = _originalGeos[i];
                _meshesToPin[i].PivotPoint = Vector3.Zero;
            }

            _selectionManager.SetState(_selectionOldState);
        }
    }
}
