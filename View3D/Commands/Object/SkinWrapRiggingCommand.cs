using System.Collections.Generic;
using System.Linq;
using View3D.Components.Component.Selection;
using View3D.Rendering.Geometry;
using View3D.SceneNodes;
using View3D.Utility;

namespace View3D.Commands.Object
{
    // TODO: THis is delete and not used
    public class SkinWrapRiggingCommand : ICommand
    {
        ISelectionState _selectionOldState;
        SelectionManager _selectionManager;

        List<MeshObject> _originalGeos;
        List<string> _originalSkeletonNames;

        List<Rmv2MeshNode> _affectedMeshes;
        List<Rmv2MeshNode> _sources;

        public string HintText { get => "Skin wrap re-rigging"; }
        public bool IsMutation { get => true; }

        public void Configure(IEnumerable<Rmv2MeshNode> affectedMeshes, IEnumerable<Rmv2MeshNode> sources)
        {
            _affectedMeshes = affectedMeshes.ToList();
            _sources = sources.ToList();
        }

        public SkinWrapRiggingCommand(SelectionManager selectionManager)
        {
            _selectionManager = selectionManager; ;
        }

        public void Execute()
        {
            // Create undo state
            _originalGeos = _affectedMeshes.Select(x => x.Geometry.Clone()).ToList();
            _originalSkeletonNames = _affectedMeshes.Select(x => x.Geometry.ParentSkeletonName).ToList();
            _selectionOldState = _selectionManager.GetStateCopy();

            // Update the meshes
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
                    foreach (var source in _sources)
                    {
                        var closestIndex = IntersectionMath.FindClosestVertexIndex(source.Geometry, currentVertex, out float distance);
                        if (distance < clostestDist)
                        {

                            clostestDist = distance;
                            closestVertexId = closestIndex;
                            closestMesh = source.Geometry;
                        }
                    }

                    affectedMesh.Geometry.VertexArray[i].BlendIndices = closestMesh.VertexArray[closestVertexId].BlendIndices;
                    affectedMesh.Geometry.VertexArray[i].BlendWeights = closestMesh.VertexArray[closestVertexId].BlendWeights;
                }

                affectedMesh.Geometry.RebuildVertexBuffer();
            }
        }

        public void Undo()
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
