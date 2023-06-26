using CommonControls.Common;
using System.Collections.Generic;
using View3D.Components.Component.Selection;

namespace View3D.Commands.Face
{
    public class FaceSelectionCommand : CommandBase<FaceSelectionCommand>
    {
        SelectionManager _selectionManager;

        ISelectionState _oldState;
        bool _isAdd;
        bool _isRemove;
        List<int> _selectedFaces;

        public override string GetHintText() => "Face selected";
        public override bool IsMutation() => false;

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

        protected override void ExecuteCommand()
        {
            _oldState = _selectionManager.GetStateCopy();
            var currentState = _selectionManager.GetState() as FaceSelectionState;
            _logger.Here().Information($"Command info - Add[{_isAdd}] Item[{currentState.RenderObject.Name}] faces[{_selectedFaces.Count}]");

            if (!(_isAdd || _isRemove))
                currentState.Clear();

            currentState.ModifySelection(_selectedFaces, _isRemove);

            currentState.EnsureSorted();
        }

        protected override void UndoCommand()
        {
            _selectionManager.SetState(_oldState);
        }

        
    }
}
