using Common;
using Serilog;
using System.Collections.Generic;
using View3D.Components.Component;
using View3D.Rendering;
using View3D.Scene;

namespace View3D.Commands
{

    public class FaceSelectionCommand : ICommand
    {
        ILogger _logger = Logging.Create<ObjectSelectionCommand>();
        private readonly SelectionManager _selectionManager;
        

        SelectionManager.State _oldState;

        public bool IsModification { get; set; } = false;
        public FaceSelection SelectedFaces { get; set; }

        public FaceSelectionCommand(SelectionManager selectionManager)
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
            _logger.Here().Information($"Executing FaceSelectionCommand");
            if (!IsModification)
            {
                _selectionManager.SetFaceSelection(SelectedFaces);
            }
            else
            {
                var selection = _selectionManager.CurrentFaceSelection();
                foreach (var newSelectionItem in SelectedFaces.SelectedFaces)
                {
                    if (selection.SelectedFaces.Contains(newSelectionItem))
                        selection.SelectedFaces.Remove(newSelectionItem);
                    else
                        selection.SelectedFaces.Add(newSelectionItem);

                    _selectionManager.SetFaceSelection(selection);
                }
            }
        }

        public void Undo()
        {
            _logger.Here().Information($"Undoing FaceSelectionCommand");
            _selectionManager.SetState(_oldState);
        }
    }
}
