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

        List<SceneNode> _objectsToCopy;
        List<SceneNode> _clonedObjects = new List<SceneNode>();
        SceneManager _sceneManager;
        SelectionManager _selectionManager;

        ISelectionState _oldState;
        public DuplicateObjectCommand(List<SceneNode> objectsToCopy, SceneManager sceneManager, SelectionManager selectionManager)
        {
            _objectsToCopy = new List<SceneNode>(objectsToCopy);
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
                clonedItem.Id = Guid.NewGuid().ToString();
                _clonedObjects.Add(clonedItem);
                item.Parent.AddObject(clonedItem);
                if(clonedItem is ISelectable selectableNode)
                    objectState.ModifySelection(selectableNode);
            }
        }

        public void Undo()
        {
            _logger.Here().Information($"Undoing DuplicateObjectCommand");

            foreach (var item in _clonedObjects)
                item.Parent.RemoveObject(item);

            _selectionManager.SetState(_oldState);
        }
    }
}
