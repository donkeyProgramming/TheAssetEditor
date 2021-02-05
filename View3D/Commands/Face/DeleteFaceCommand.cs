using Common;
using Serilog;
using System;
using System.Collections.Generic;
using System.Text;
using View3D.Components.Component.Selection;
using View3D.Rendering;
using View3D.Rendering.Geometry;

namespace View3D.Commands.Face
{

    public class DeleteFaceCommand : ICommand
    {
        ILogger _logger = Logging.Create<FaceSelectionCommand>();
        private readonly SelectionManager _selectionManager;

        ISelectionState _oldState;
       // IGeometry 

        public List<int> FacesToDelete { get; set; }
        RenderItem RenderObject { get; set; }

        public DeleteFaceCommand(SelectionManager selectionManager)
        {
            _selectionManager = selectionManager;
            _oldState = _selectionManager.GetStateCopy();
        }

        public void Execute()
        {
            _logger.Here().Information($"Executing DeleteFaceCommand");
            //var currentState = _selectionManager.GetState() as FaceSelectionState;
            //
            //
            //
            //foreach (var newSelectionItem in SelectedFaces)
            //    currentState.ModifySelection(newSelectionItem);
            //
            //currentState.EnsureSorted();
        }

        public void Undo()
        {
            _logger.Here().Information($"Undoing DeleteFaceCommand");
            _selectionManager.SetState(_oldState);
        }
    }
}
