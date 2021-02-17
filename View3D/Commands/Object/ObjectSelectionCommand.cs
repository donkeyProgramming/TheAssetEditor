using Common;
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
        private readonly SelectionManager _selectionManager;
        public List<ISelectable> Items { get; set; } = new List<ISelectable>();
        public bool IsModification { get; set; } = false;
        public bool ClearSelection { get; set; } = false;

        ISelectionState _oldState;

        public ObjectSelectionCommand(SelectionManager selectionManager)
        {
            _selectionManager = selectionManager;
            _oldState = _selectionManager.GetStateCopy();
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
    }
}

