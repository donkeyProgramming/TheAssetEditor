using CommonControls.Common;
using System.Collections.Generic;
using System.Linq;
using View3D.Components.Component.Selection;
using View3D.SceneNodes;

namespace View3D.Commands.Object
{
    public class ObjectSelectionCommand : CommandBase<ObjectSelectionCommand>
    {
        SelectionManager _selectionManager;

        ISelectionState _oldState;
        bool _isModification;
        bool _isRemove;
        List<ISelectable> _items { get; set; } = new List<ISelectable>();

        public override string GetHintText() => "Object Selected";
        public override bool IsMutation() => false;

        public ObjectSelectionCommand(SelectionManager selectionManager)
        {
            _selectionManager = selectionManager;
        }

        public void Configure(List<ISelectable> items, bool isModification = false, bool removeSelection = false)
        {
            _items = items;
            _isModification = isModification;
            _isRemove = removeSelection;
        }

        public void Configure(ISelectable item, bool isModification = false, bool removeSelection = false)
        {
            _items = new List<ISelectable>() { item };
            _isModification = isModification;
            _isRemove = removeSelection;
        }

        protected override void ExecuteCommand()
        {
            _oldState = _selectionManager.GetStateCopy();
            var currentState = _selectionManager.GetState() as ObjectSelectionState;
            if (currentState == null)
                currentState = _selectionManager.CreateSelectionSate(GeometrySelectionMode.Object, null) as ObjectSelectionState;
            _logger.Here().Information($"Command info - Remove[{_isRemove}] Mod[{_isModification}] Items[{string.Join(',', _items.Select(x => x.Name))}]");

            if (!(_isModification || _isRemove))
                currentState.Clear();

            currentState.ModifySelection(_items, _isRemove);
        }

        protected override void UndoCommand()
        {
            _selectionManager.SetState(_oldState);
        }
    }
}
    

