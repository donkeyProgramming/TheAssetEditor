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
    public class DuplicateObjectCommand : ICommand
    {
        ILogger _logger = Logging.Create<DeleteObjectsCommand>();

        List<RenderItem> _objectsToCopy;
        List<RenderItem> _clonedObjects = new List<RenderItem>();
        SceneManager _sceneManager;
        SelectionManager _selectionManager;

        ISelectionState _oldState;
        public DuplicateObjectCommand(List<RenderItem> objectsToCopy, SceneManager sceneManager, SelectionManager selectionManager)
        {
            _objectsToCopy = new List<RenderItem>(objectsToCopy);
            _sceneManager = sceneManager;
            _selectionManager = selectionManager;
        }

        public void Execute()
        {
            _logger.Here().Information($"Executing DuplicateObjectCommand Items[{string.Join(',', _objectsToCopy.Select(x => x.Name))}]");

            _oldState = _selectionManager.GetStateCopy();

            var state = _selectionManager.CreateSelectionSate(GeometrySelectionMode.Object);
            var objectState = state as ObjectSelectionState;

            foreach (var item in _objectsToCopy)
            {
                var clonedItem = item.Clone();
                _clonedObjects.Add(clonedItem);
                _sceneManager.AddObject(clonedItem);
                objectState.ModifySelection(clonedItem);
            }
        }

        public void Undo()
        {
            _logger.Here().Information($"Undoing DuplicateObjectCommand");
            
            foreach(var item in _clonedObjects)
                _sceneManager.RemoveObject(item);

            _selectionManager.SetState(_oldState);
        }
    }
}
