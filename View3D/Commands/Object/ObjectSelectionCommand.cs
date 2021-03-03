using Common;
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

        public override void Initialize(IComponentManager componentManager)
        {
            _selectionManager = componentManager.GetComponent<SelectionManager>();
        }

        protected override void ExecuteCommand()
        {
            _oldState = _selectionManager.GetStateCopy();
            var currentState = _selectionManager.GetState() as ObjectSelectionState;
            if (currentState == null)
                currentState = _selectionManager.CreateSelectionSate(GeometrySelectionMode.Object) as ObjectSelectionState;
            _logger.Here().Information($"Command info - Remove[{_isRemove}] Mod[{_isModification}] Items[{string.Join(',', _items.Select(x => x.Name))}]");

            if (!(_isModification || _isRemove))
                currentState.Clear();

            foreach (var newSelectionItem in _items)
                currentState.ModifySelection(newSelectionItem, _isRemove);
        }

        protected override void UndoCommand()
        {
            _selectionManager.SetState(_oldState);
        }
    }
}

        /* public class ObjectSelectionCommand : CommandBase<ObjectSelectionCommand>
         {
             private readonly SelectionManager _selectionManager;
             public List<ISelectable> Items { get; set; } = new List<ISelectable>();
             public bool IsModification { get; set; } = false;
             public bool ClearSelection { get; set; } = false;

             ISelectionState _oldState;

             public ObjectSelectionCommand(SelectionManager selectionManager, bool isModification)
             {
                 _selectionManager = selectionManager;
                 _oldState = _selectionManager.GetStateCopy();
                 IsModification = isModification;
             }

             protected override void ExecuteCommand()
             {
                 _logger.Here().Information($"Command info - Clear[{ClearSelection}] Mod[{IsModification}] Items[{string.Join(',', Items.Select(x=>x.Name))}]");

                 if (ClearSelection)
                 {
                     _selectionManager.CreateSelectionSate(GeometrySelectionMode.Object);
                 }
                 else
                 {
                     var currentState = _selectionManager.GetState();
                     if (currentState.Mode != GeometrySelectionMode.Object)
                         currentState = _selectionManager.CreateSelectionSate(GeometrySelectionMode.Object);

                     var objectState = currentState as ObjectSelectionState;
                     if (!IsModification)
                         objectState.Clear();

                     foreach (var newSelectionItem in Items)
                         objectState.ModifySelection(newSelectionItem);
                 }
             }

             protected override void UndoCommand()
             {
                 _selectionManager.SetState(_oldState);
             }
         }*/
    

