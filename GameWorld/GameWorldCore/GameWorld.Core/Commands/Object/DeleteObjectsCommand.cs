using GameWorld.Core.Components.Selection;
using GameWorld.Core.SceneNodes;
using Serilog;
using Shared.Core.ErrorHandling;
using Shared.Core.Events;

namespace GameWorld.Core.Commands.Object
{
    public class DeleteObjectsCommand : IAeUndoCommandCommand
    {
        private readonly ILogger _logger;
        private readonly SelectionManager _selectionManager;
        
        List<ISceneNode> _itemsToDelete = [];
        ISelectionState? _oldState;

        public string HintText { get => "Delete Object"; }
        public bool IsMutation { get => true; }

        public DeleteObjectsCommand(SelectionManager selectionManager, IScopedLogger scopedLogger)
        {
            _logger = scopedLogger.ForContext<DeleteObjectsCommand>();
            _selectionManager = selectionManager;
        }

        public void Configure(List<ISelectable> itemsToDelete)
        {
            _itemsToDelete = new List<ISceneNode>(itemsToDelete.Select(x => x));
        }

        public void Configure(ISceneNode itemToDelete)
        {
            _itemsToDelete = [itemToDelete];
        }

        public void Execute()
        {
            _oldState = _selectionManager.GetStateCopy();

            _logger.Here().Information($"Command info - Items[{string.Join(',', _itemsToDelete.Select(x => x.Name))}]");
            foreach (var item in _itemsToDelete)
                item.Parent.RemoveObject(item);

            _selectionManager.CreateSelectionSate(GeometrySelectionMode.Object, null);
        }

        public void Undo()
        {
            foreach (var item in _itemsToDelete)
                item.Parent.AddObject(item);

            _selectionManager.SetState(_oldState!);
        }
    }
}
