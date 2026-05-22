using GameWorld.Core.Commands;
using GameWorld.Core.Components.Selection;
using Serilog;
using Shared.Core.ErrorHandling;
using System.Collections.Generic;

namespace GameWorld.Core.Commands.Vertex
{
    public class VertexSelectionCommand : ICommand
    {
        ILogger _logger = Logging.Create<VertexSelectionCommand>();
        SelectionManager _selectionManager;
        ISelectionState _oldState;

        bool _isAdd;
        bool _isRemove;
        List<int> _selectedVertices;



        public string HintText { get => "Select Vertex"; }
        public bool IsMutation { get => false; }


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

        public void Execute()
        {
            _oldState = _selectionManager.GetStateCopy();
            var currentState = _selectionManager.GetState() as VertexSelectionState;
            _logger.Here().Information($"Command info - Add[{_isAdd}] Item[{currentState.RenderObject.Name}] Vertices[{_selectedVertices.Count}]");

            if (!(_isAdd || _isRemove))
                currentState.Clear();

            currentState.ModifySelection(_selectedVertices, _isRemove);

            currentState.EnsureSorted();
        }

        public void Undo()
        {
            _selectionManager.SetState(_oldState);
        }


    }
}