using Common;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using View3D.Components.Component;
using View3D.Components.Component.Selection;
using View3D.Rendering;

namespace View3D.Commands.Object
{

    public class ObjectSelectionModeCommand : ICommand
    {
        ILogger _logger = Logging.Create<ObjectSelectionModeCommand>();
        private readonly SelectionManager _selectionManager;
        GeometrySelectionMode _newMode;
        ISelectable _selectedItem;
        ISelectionState _oldState;

        public ObjectSelectionModeCommand(SelectionManager selectionManager, GeometrySelectionMode newMode)
        {
            _newMode = newMode;
            _selectionManager = selectionManager;
            _oldState = _selectionManager.GetStateCopy();
        }

        public ObjectSelectionModeCommand(ISelectable selectedItem, SelectionManager selectionManager, GeometrySelectionMode newMode) : this(selectionManager, newMode)
        {
            _selectedItem = selectedItem;
        }

        public void Execute()
        {
            _logger.Here().Information($"Executing ObjectSelectionModeCommand");
            var newSelectionState = _selectionManager.CreateSelectionSate(_newMode);

            if (newSelectionState.Mode == GeometrySelectionMode.Object && _selectedItem != null)
                (newSelectionState as ObjectSelectionState).ModifySelection(_selectedItem);
            else if (newSelectionState.Mode == GeometrySelectionMode.Face)
                (newSelectionState as FaceSelectionState).RenderObject = _selectedItem;
            else if(newSelectionState.Mode == GeometrySelectionMode.Vertex)
                (newSelectionState as VertexSelectionState).RenderObject = _selectedItem;
        }

        public void Undo()
        {
            _logger.Here().Information($"Undoing ObjectSelectionModeCommand");
            _selectionManager.SetState(_oldState);
        }
    }
}
