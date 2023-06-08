using CommonControls.Common;
using MonoGame.Framework.WpfInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using View3D.Components.Component.Selection;
using View3D.Rendering.Geometry;
using View3D.SceneNodes;
using View3D.Services.MeshOptimization;

namespace View3D.Commands.Object
{

    public class ReduceMeshCommand : CommandBase<ObjectSelectionModeCommand>
    {
        private readonly List<Rmv2MeshNode> _meshList;
        List<MeshObject> _originalGeometry = new List<MeshObject>();
        private readonly float _factor;
        SelectionManager _selectionManager;
        ISelectionState _oldState;

        public ReduceMeshCommand(List<Rmv2MeshNode> meshList, float factor)
        {
            _meshList = meshList;
            _factor = factor;
        }

        public override string GetHintText()
        {
            return "Reduce mesh";
        }

        public override void Initialize(IComponentManager componentManager)
        {
            _selectionManager = componentManager.GetComponent<SelectionManager>();
        }

        protected override void ExecuteCommand()
        {
            _oldState = _selectionManager.GetStateCopy();

            foreach (var meshNode in _meshList)
            {
                var originalMesh = meshNode.Geometry;
                var reducedMesh = MeshOptimizerService.CreatedReducedCopy(originalMesh, _factor);
                meshNode.Geometry = reducedMesh;
                _originalGeometry.Add(originalMesh);
            }
        }

        protected override void UndoCommand()
        {
            for (int i = 0; i < _meshList.Count; i++)
                _meshList[i].Geometry = _originalGeometry[i];

            _selectionManager.SetState(_oldState);
        }
    }
}
