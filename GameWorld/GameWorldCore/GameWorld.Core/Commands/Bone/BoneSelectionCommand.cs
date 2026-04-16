using System.Collections.Generic;
using GameWorld.Core.Components.Selection;

namespace GameWorld.Core.Commands.Bone
{
    public class BoneSelectionCommand : ICommand
    {
        readonly SelectionManager _selectionManager;
        ISelectionState _oldState;

        BoneSelectionState _hackOldState;

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
            if (currentState != null)
            {
                _hackOldState = (BoneSelectionState)currentState.Clone();
            }
            if (currentState == null && _hackOldState != null)
            {
                currentState = _hackOldState;
            }

            if (!(_isAdd || _isRemove))
                currentState.Clear();

            currentState.ModifySelection(_selectedBones, _isRemove);

            currentState.EnsureSorted();
        }
    }
}
