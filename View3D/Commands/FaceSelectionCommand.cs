using Common;
using Serilog;
using System.Collections.Generic;
using View3D.Components.Component;
using View3D.Rendering;
using View3D.Scene;

namespace View3D.Commands
{

    public class FaceSelectionCommand : ICommand
    {
        ILogger _logger = Logging.Create<ObjectSelectionCommand>();
        private readonly SelectionManager _selectionManager;

        ISelectionState _oldState;
        public bool IsModification { get; set; } = false;
        public List<int> SelectedFaces { get; set; }

        public FaceSelectionCommand(SelectionManager selectionManager)
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
            var currentState = _selectionManager.GetState() as FaceSelectionState;
            _logger.Here().Information($"Executing FaceSelectionCommand Mod[{IsModification}] Item[{currentState.RenderObject.Name}] faces[{string.Join(',', SelectedFaces)}]");

            if (!IsModification)
                currentState.Clear();

            foreach (var newSelectionItem in SelectedFaces)
                currentState.ModifySelection(newSelectionItem);

            currentState.EnsureSorted();
        }

        public void Undo()
        {
            _logger.Here().Information($"Undoing FaceSelectionCommand");
            _selectionManager.SetState(_oldState);
        }
    }
}
