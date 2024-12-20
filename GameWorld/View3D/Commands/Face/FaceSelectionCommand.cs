using System.Collections.Generic;
using GameWorld.Core.Components.Selection;
using Serilog;
using Shared.Core.ErrorHandling;

namespace GameWorld.Core.Commands.Face
{
    public class FaceSelectionCommand : ICommand
    {
        ILogger _logger = Logging.Create<FaceSelectionCommand>();
        SelectionManager _selectionManager;

        ISelectionState _oldState;
        bool _isAdd;
        bool _isRemove;
        List<int> _selectedFaces;

        public string HintText { get => "Face selected"; }
        public bool IsMutation { get => false; }

        public FaceSelectionCommand(SelectionManager selectionManager)
        {
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
