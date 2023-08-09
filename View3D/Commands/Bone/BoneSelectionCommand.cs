using System.Collections.Generic;
using View3D.Components.Component.Selection;

namespace View3D.Commands.Bone
{
    public class BoneSelectionCommand : ICommand
    {
        SelectionManager _selectionManager;
        ISelectionState _oldState;

        bool _isAdd;
        bool _isRemove;
        List<int> _selectedBones;

        public string HintText => "Select Bones";

        bool ICommand.IsMutation => false;

        public void Configure(List<int> selectedBones, bool isAdd, bool isRemove)
        {
            _selectedBones = selectedBones;
            _isAdd = isAdd;
            _isRemove = isRemove;
        }

        public BoneSelectionCommand(SelectionManager selectionManager)
        {
            _selectionManager = selectionManager;
        }

        public void Undo()
        {
            _selectionManager.SetState(_oldState);
        }

        public void Execute()
        {
            _oldState = _selectionManager.GetStateCopy();
            var currentState = _selectionManager.GetState() as BoneSelectionState;
            //_logger.Here().Information($"Command info - Add[{_isAdd}] Item[{currentState.RenderObject.Name}] Bones[{_selectedBones.Count}]");

            if (!(_isAdd || _isRemove))
                currentState.Clear();

            currentState.ModifySelection(_selectedBones, _isRemove);

            currentState.EnsureSorted();
        }
    }
}
