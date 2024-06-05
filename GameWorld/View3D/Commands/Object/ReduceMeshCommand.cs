using System.Collections.Generic;
using View3D.Components.Component.Selection;
using View3D.Rendering.Geometry;
using View3D.SceneNodes;
using View3D.Services.MeshOptimization;

namespace View3D.Commands.Object
{

    public class ReduceMeshCommand : ICommand
    {
        List<Rmv2MeshNode> _meshList;
        List<MeshObject> _originalGeometry = new List<MeshObject>();
        float _factor;
        SelectionManager _selectionManager;
        ISelectionState _oldState;

        public ReduceMeshCommand(SelectionManager selectionManager)
        {
            _selectionManager = selectionManager;
        }

        public void Configure(List<Rmv2MeshNode> meshList, float factor)
        {
            _meshList = meshList;
            _factor = factor;
        }

        public string HintText { get => "Reduce mesh"; }
        public bool IsMutation { get => true; }


        public void Execute()
        {
            _oldState = _selectionManager.GetStateCopy();

            foreach (var meshNode in _meshList)
            {
                var originalMesh = meshNode.Geometry;
                var reducedMesh = MeshOptimizerService_ThisShouldBeRemoved.CreatedReducedCopy(originalMesh, _factor);
                meshNode.Geometry = reducedMesh;
                _originalGeometry.Add(originalMesh);
            }
        }

        public void Undo()
        {
            for (int i = 0; i < _meshList.Count; i++)
                _meshList[i].Geometry = _originalGeometry[i];

            _selectionManager.SetState(_oldState);
        }
    }
}
