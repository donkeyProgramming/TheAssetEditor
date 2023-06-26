using CommonControls.Common;
using System.Collections.Generic;
using System.Linq;
using View3D.Components.Component.Selection;
using View3D.SceneNodes;
using View3D.Utility;

namespace View3D.Commands.Object
{
    public class CombineMeshCommand : CommandBase<CombineMeshCommand>
    {
        List<ISelectable> _objectsToCombine;
        List<Rmv2MeshNode> _combinedMeshes = new List<Rmv2MeshNode>();

        SelectionManager _selectionManager;
        ISelectionState _originalSelectionState;


        public CombineMeshCommand(SelectionManager selectionManager)
        {
            _selectionManager = selectionManager;
        }

        public void Configure(List<ISelectable> objectsToCombine)
        {
            _objectsToCombine = new List<ISelectable>(objectsToCombine);
        }

        public override string GetHintText() => "Combine Objects";



        protected override void ExecuteCommand()
        {
            _originalSelectionState = _selectionManager.GetStateCopy();

            using (new WaitCursor())
            {
                var combinedMeshes = new List<IEditableGeometry>();

                var geometriesToCombine = _objectsToCombine
                    .Where(x => x is Rmv2MeshNode)
                    .Cast<Rmv2MeshNode>()
                    .ToList();
                _combinedMeshes = ModelCombiner.CombineMeshes(geometriesToCombine);

                // Remove all
                foreach (var item in _objectsToCombine)
                    item.Parent.RemoveObject(item);

                // Add all
                foreach(var item in _combinedMeshes)
                    item.Parent.AddObject(item);

                // Select all new 
                var currentState = _selectionManager.GetState() as ObjectSelectionState;
                currentState.Clear();
                currentState.ModifySelection(_combinedMeshes.Cast<ISelectable>(), false);
                
            }
        }

        protected override void UndoCommand()
        {
            foreach (var item in _objectsToCombine)
                item.Parent.AddObject(item);

            foreach(var item in _combinedMeshes)
                item.Parent.RemoveObject(item);

            _selectionManager.SetState(_originalSelectionState);
        }
    }
}
