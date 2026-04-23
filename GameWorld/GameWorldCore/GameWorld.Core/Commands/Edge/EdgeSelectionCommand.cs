using System.Collections.Generic;
using GameWorld.Core.Components.Selection;

namespace GameWorld.Core.Commands.Edge
{
    public class EdgeSelectionCommand : ICommand
    {
        private readonly SelectionManager _selectionManager;
        private List<(int v0, int v1)> _edges;
        private bool _isAdd;
        private bool _isRemove;
        private ISelectionState _oldState;

        public string HintText => "Edge selection";
        public bool IsMutation => false;

        public EdgeSelectionCommand(SelectionManager selectionManager)
        {
            _selectionManager = selectionManager;
        }

        public void Configure(List<(int v0, int v1)> edges, bool isSelectionModification, bool removeSelection)
        {
            _edges = edges;
            _isAdd = isSelectionModification;
            _isRemove = removeSelection;
        }

        public void Execute()
        {
            _oldState = _selectionManager.GetStateCopy();
            var state = _selectionManager.GetState<EdgeSelectionState>();
            if (state == null)
                return;

            if (!_isAdd && !_isRemove)
                state.Clear();

            state.ModifySelection(_edges, _isRemove);
        }

        public void Undo()
        {
            _selectionManager.SetState(_oldState);
        }
    }
}
