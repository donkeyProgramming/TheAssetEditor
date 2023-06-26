using System.Collections.Generic;
using System.Linq;
using View3D.Components.Component.Selection;
using View3D.Rendering.Geometry;
using View3D.SceneNodes;
using View3D.Utility;

namespace View3D.Commands.Object
{
    public class SkinWrapRiggingCommand : CommandBase<SkinWrapRiggingCommand>
    {
        ISelectionState _selectionOldState;
        SelectionManager _selectionManager;

        List<MeshObject> _originalGeos;
        List<string> _originalSkeletonNames;

        List<Rmv2MeshNode> _affectedMeshes;
        List<Rmv2MeshNode> _sources;

        public void Configure(IEnumerable<Rmv2MeshNode> affectedMeshes, IEnumerable<Rmv2MeshNode> sources)
        {
            _affectedMeshes = affectedMeshes.ToList();
            _sources = sources.ToList();
        }

        public override string GetHintText()
        {
            return "Skin wrap re-rigging";
        }

        public SkinWrapRiggingCommand(SelectionManager selectionManager)
        {
            _selectionManager = selectionManager;;
        }

        protected override void ExecuteCommand()
        {
            // Create undo state
            _originalGeos = _affectedMeshes.Select(x => x.Geometry.Clone()).ToList();
            _originalSkeletonNames = _affectedMeshes.Select(x => x.Geometry.ParentSkeletonName).ToList();
            _selectionOldState = _selectionManager.GetStateCopy();

            // Update the meshes
            var calculators = _sources.Select(x => new MeshDistanceCalculator(x.Geometry)).ToArray();

            foreach (var affectedMesh in _affectedMeshes)
            {
                // Set skeleton and vertex type from first source object
                affectedMesh.Geometry.ChangeVertexType(_sources.First().Geometry.VertexFormat, _sources.First().Geometry.ParentSkeletonName, false);

                var clostestDist = float.PositiveInfinity;
                MeshObject closestMesh = null;
                int closestVertexId = 0;

                for (int i = 0; i < affectedMesh.Geometry.VertexCount(); i++)
                {
                    var currentVertex = affectedMesh.Geometry.VertexArray[i].Position3();
                    foreach (var distanceCalculator in calculators)
                    {
                        var closestIndex = distanceCalculator.FindClosestVertexIndex(currentVertex, out float distance);
                        if (distance < clostestDist)
                        {

                            clostestDist = distance;
                            closestVertexId = closestIndex;
                            closestMesh = distanceCalculator.Mesh;
                        }
                    }

                    affectedMesh.Geometry.VertexArray[i].BlendIndices = closestMesh.VertexArray[closestVertexId].BlendIndices;
                    affectedMesh.Geometry.VertexArray[i].BlendWeights = closestMesh.VertexArray[closestVertexId].BlendWeights;
                }

                affectedMesh.Geometry.RebuildVertexBuffer();
            }
        }

        protected override void UndoCommand()
        {
            for (int i = 0; i < _affectedMeshes.Count; i++)
            {
                _affectedMeshes[i].Geometry = _originalGeos[i];
                _affectedMeshes[i].Geometry.ParentSkeletonName = _originalSkeletonNames[i];
            }

            _selectionManager.SetState(_selectionOldState);
        }
    }
}
