using Common;
using Serilog;
using System;
using System.Collections.Generic;
using System.Text;
using View3D.Components.Component;
using View3D.Rendering;

namespace View3D.Commands
{

    public class ObjectSelectionModeCommand : ICommand
    {
        ILogger _logger = Logging.Create<ObjectSelectionModeCommand>();
        private readonly SelectionManager _selectionManager;
        GeometrySelectionMode _newMode;

        SelectionManager.State _oldState;

        public ObjectSelectionModeCommand(SelectionManager selectionManager, GeometrySelectionMode newMode)
        {
            _newMode = newMode;
            _selectionManager = selectionManager;
            _oldState = _selectionManager.GetState();
        }

        public void Cancel()
        {
            Undo();
        }

        public void Execute()
        {
            _logger.Here().Information($"Executing ObjectSelectionModeCommand");
            _selectionManager.GeometrySelectionMode = _newMode;
        }

        public void Undo()
        {
            _logger.Here().Information($"Undoing ObjectSelectionModeCommand");
            _selectionManager.SetState(_oldState);
        }
    }
}
