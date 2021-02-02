using Common;
using Serilog;
using System.Collections.Generic;
using View3D.Components.Component;
using View3D.Rendering;
using View3D.Scene;

namespace View3D.Commands
{
    public class ObjectSelectionCommand : ICommand
    {
        ILogger _logger = Logging.Create<ObjectSelectionCommand>();
        private readonly SelectionManager _selectionManager;
        public List<RenderItem> Items { get; set; } = new List<RenderItem>();
        public bool IsModification { get; set; } = false;

        SelectionManager.State _oldState;

        public ObjectSelectionCommand(SelectionManager selectionManager)
        {
            _selectionManager = selectionManager;
            _oldState = _selectionManager.GetState();
        }

        public void Cancel()
        {
            Undo();
        }

        public void Execute()
        {
            _logger.Here().Information($"Executing SelectionCommand");
            if (!IsModification)
            {
                _selectionManager.GeometrySelectionMode = GeometrySelectionMode.Object;
                _selectionManager.ClearSelection();

                foreach (var newSelectionItem in Items)
                    _selectionManager.AddToSelection(newSelectionItem);
            }
            else
            {
                foreach (var newSelectionItem in Items)
                    _selectionManager.ModifySelection(newSelectionItem);
            }
        }

        public void Undo()
        {
            _logger.Here().Information($"Undoing SelectionCommand");
            _selectionManager.SetState(_oldState);
        }
    }
}

