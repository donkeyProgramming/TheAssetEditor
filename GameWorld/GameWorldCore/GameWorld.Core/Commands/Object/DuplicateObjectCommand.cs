using GameWorld.Core.Components.Selection;
using GameWorld.Core.SceneNodes;
using Shared.Core.ErrorHandling;
using Shared.Core.Events;

namespace GameWorld.Core.Commands.Object
{
    public class DuplicateObjectCommand : IAeUndoCommandCommand
    {
        readonly ILogger _logger;
        List<ISceneNode> _objectsToCopy;
        readonly List<ISceneNode> _clonedObjects = new List<ISceneNode>();
        readonly SelectionManager _selectionManager;

        ISelectionState _oldState;

        public string HintText { get => "Duplicate Object"; }
        public bool IsMutation { get => true; }

        public void Configure(List<ISceneNode> objectsToCopy)
        {
            _objectsToCopy = new List<ISceneNode>(objectsToCopy);
        }

        public DuplicateObjectCommand(SelectionManager selectionManager, IScopedLogger scopedLogger)
        {
            _logger = scopedLogger.ForContext<DuplicateObjectCommand>();
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
                clonedItem.Id = item.Id + Guid.NewGuid().ToString();
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
