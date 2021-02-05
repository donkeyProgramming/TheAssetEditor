using Common;
using Serilog;
using System.Collections.Generic;
using View3D.Components.Component;
using View3D.Components.Component.Selection;
using View3D.Rendering;
using View3D.Scene;

namespace View3D.Commands.Vertex
{
    public class VertexSelectionCommand : ICommand
    {
        ILogger _logger = Logging.Create<VertexSelectionCommand>();
        private readonly SelectionManager _selectionManager;

        ISelectionState _oldState;
        public bool IsModification { get; set; } = false;
        public List<int> SelectedVertices { get; set; }

        public VertexSelectionCommand(SelectionManager selectionManager)
        {
            _selectionManager = selectionManager;
            _oldState = _selectionManager.GetStateCopy();
        }

        public void Cancel()
        {
            Undo();
        }

        public void Execute()
        {
            var currentState = _selectionManager.GetState() as VertexSelectionState;
            _logger.Here().Information($"Executing VertexSelectionCommand Mod[{IsModification}] Item[{currentState.RenderObject.Name}] Vertices[{SelectedVertices.Count}]");

            if (!IsModification)
                currentState.Clear();

            foreach (var newSelectionItem in SelectedVertices)
                currentState.ModifySelection(newSelectionItem);

            currentState.EnsureSorted();
        }

        public void Undo()
        {
            _logger.Here().Information($"Undoing VertexSelectionCommand");
            _selectionManager.SetState(_oldState);
        }
    }
}