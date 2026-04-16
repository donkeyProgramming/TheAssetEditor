using System.Collections.Generic;
using System.Linq;
using GameWorld.Core.Commands;
using GameWorld.Core.Components.Selection;
using GameWorld.Core.SceneNodes;
using GameWorld.Core.Utility;
using Shared.Ui.Common;

namespace GameWorld.Core.Commands.Object
{
    public class CombineMeshCommand : ICommand
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

        public string HintText { get => "Combine Objects"; }
        public bool IsMutation { get => true; }



        public void Execute()
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
                foreach (var item in _combinedMeshes)
                    item.Parent.AddObject(item);

                // Select all new 
                var currentState = _selectionManager.GetState() as ObjectSelectionState;
                currentState.Clear();
                currentState.ModifySelection(_combinedMeshes.Cast<ISelectable>(), false);

            }
        }

        public void Undo()
        {
            foreach (var item in _objectsToCombine)
                item.Parent.AddObject(item);

            foreach (var item in _combinedMeshes)
                item.Parent.RemoveObject(item);

            _selectionManager.SetState(_originalSelectionState);
        }
    }
}
