using Common;
using Serilog;
using System.Collections.Generic;
using View3D.Rendering;
using View3D.Scene;

namespace View3D.Commands
{
    public class SelectionCommand : ICommand
    {
        ILogger _logger = Logging.Create<SelectionCommand>();
        private readonly SelectionManager _selectionManager;
        public List<RenderItem> Items { get; set; } = new List<RenderItem>();
        public bool IsModification { get; set; } = false;

        List<RenderItem> _oldSelection;

        public SelectionCommand(SelectionManager selectionManager)
        {
            _selectionManager = selectionManager;
            _oldSelection = _selectionManager.CurrentSelection();
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
                _selectionManager.ClearSelection();

                foreach (var newSelectionItem in Items)
                    _selectionManager.AddToSelection(newSelectionItem);

                return;
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
            _selectionManager.SetCurrentSelection(_oldSelection);
        }
    }
}

