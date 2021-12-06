using CommonControls.Common;
using MonoGame.Framework.WpfInterop;
using Serilog;
using System.Collections.Generic;
using System.Linq;
using View3D.Components.Component;
using View3D.Components.Component.Selection;
using View3D.Rendering;
using View3D.Scene;
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

        public ObjectSelectionCommand(List<ISelectable> items, bool isModification = false, bool removeSelection = false)
        {
            _items = items;
            _isModification = isModification;
            _isRemove = removeSelection;
        }

        public ObjectSelectionCommand(ISelectable item, bool isModification = false, bool removeSelection = false)
        {
            _items = new List<ISelectable>() { item };
            _isModification = isModification;
            _isRemove = removeSelection;
        }

        public override string GetHintText()
        {
            return "Object Selected";
        }

        public override void Initialize(IComponentManager componentManager)
        {
            _selectionManager = componentManager.GetComponent<SelectionManager>();
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
    

