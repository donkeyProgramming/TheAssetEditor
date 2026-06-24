using GameWorld.Core.Components.Selection;
using Shared.Core.ErrorHandling;
using Shared.Core.Events;

namespace GameWorld.Core.Commands.Face
{
    public class FaceSelectionCommand : IAeUndoCommandCommand
    {
        ILogger _logger;
        SelectionManager _selectionManager;

        ISelectionState _oldState;
        bool _isAdd;
        bool _isRemove;
        List<int> _selectedFaces;

        public string HintText { get => "Face selected"; }
        public bool IsMutation { get => false; }

        public FaceSelectionCommand(SelectionManager selectionManager, IScopedLogger scopedLogger)
        {
            _logger = scopedLogger.ForContext<FaceSelectionCommand>();
            _selectionManager = selectionManager;
        }

        public void Configure(int selectedFace, bool isAdd = false, bool removeSelection = false)
        {
            _selectedFaces = new List<int>() { selectedFace };
            _isAdd = isAdd;
            _isRemove = removeSelection;
        }

        public void Configure(List<int> selectedFaces, bool isAdd = false, bool removeSelection = false)
        {
            _selectedFaces = selectedFaces;
            _isAdd = isAdd;
            _isRemove = removeSelection;
        }

        public void Execute()
        {
            _oldState = _selectionManager.GetStateCopy();
            var currentState = _selectionManager.GetState() as FaceSelectionState;
            _logger.Here().Information($"Command info - Add[{_isAdd}] Item[{currentState.RenderObject.Name}] faces[{_selectedFaces.Count}]");

            if (!(_isAdd || _isRemove))
                currentState.Clear();

            currentState.ModifySelection(_selectedFaces, _isRemove);

            currentState.EnsureSorted();
        }

        public void Undo()
        {
            _selectionManager.SetState(_oldState);
        }
    }
}
