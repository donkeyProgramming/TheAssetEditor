using CommonControls.Common;
using System.Collections.Generic;
using View3D.Components.Component.Selection;

namespace View3D.Commands.Vertex
{
    public class VertexSelectionCommand : CommandBase<VertexSelectionCommand>
    {
        SelectionManager _selectionManager;
        ISelectionState _oldState;

        bool _isAdd;
        bool _isRemove;
        List<int> _selectedVertices;

        public override string GetHintText() => "Select Vertex";
        public override bool IsMutation() => false;

        public void Configure(List<int> selectedVertices, bool isAdd, bool isRemove)
        {
            _selectedVertices = selectedVertices;
            _isAdd = isAdd;
            _isRemove = isRemove;
        }

        public VertexSelectionCommand(SelectionManager selectionManager)
        {
            _selectionManager = selectionManager;
        }

        protected override void ExecuteCommand()
        {
            _oldState = _selectionManager.GetStateCopy();
            var currentState = _selectionManager.GetState() as VertexSelectionState;
            _logger.Here().Information($"Command info - Add[{_isAdd}] Item[{currentState.RenderObject.Name}] Vertices[{_selectedVertices.Count}]");

            if (!(_isAdd || _isRemove))
                currentState.Clear();

            currentState.ModifySelection(_selectedVertices, _isRemove);

            currentState.EnsureSorted();
        }

        protected override void UndoCommand()
        {
            _selectionManager.SetState(_oldState);
        }

      
    }
}