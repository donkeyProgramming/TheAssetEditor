using Serilog;
using Shared.Core.ErrorHandling;
using System;
using System.Collections.Generic;
using System.Linq;
using View3D.Commands.Face;
using View3D.Components.Component.Selection;
using View3D.SceneNodes;

namespace View3D.Commands.Object
{
    public class DuplicateObjectCommand : ICommand
    {
        ILogger _logger = Logging.Create<FaceSelectionCommand>();
        List<ISceneNode> _objectsToCopy;
        List<ISceneNode> _clonedObjects = new List<ISceneNode>();
        SelectionManager _selectionManager;

        ISelectionState _oldState;

        public string HintText { get => "Duplicate Object"; }
        public bool IsMutation { get => true; }

        public void Configure(List<ISceneNode> objectsToCopy)
        {
            _objectsToCopy = new List<ISceneNode>(objectsToCopy);
        }

        public DuplicateObjectCommand(SelectionManager selectionManager)
        {
            _selectionManager = selectionManager;
        }

        public void Execute()
        {
            _logger.Here().Information($"Command info - Items[{string.Join(',', _objectsToCopy.Select(x => x.Name))}]");

            _oldState = _selectionManager.GetStateCopy();

            var state = _selectionManager.CreateSelectionSate(GeometrySelectionMode.Object, null);
            var objectState = state as ObjectSelectionState;

            foreach (var item in _objectsToCopy)
            {
                var clonedItem = SceneNodeHelper.CloneNode(item);
                clonedItem.Id = Guid.NewGuid().ToString();
                _clonedObjects.Add(clonedItem);
                item.Parent.AddObject(clonedItem);
                if (clonedItem is ISelectable selectableNode)
                    objectState.ModifySelectionSingleObject(selectableNode, false);
            }

            _selectionManager.SetState(objectState);
        }

        public void Undo()
        {
            foreach (var item in _clonedObjects)
                item.Parent.RemoveObject(item);

            _selectionManager.SetState(_oldState);
        }
    }
}
