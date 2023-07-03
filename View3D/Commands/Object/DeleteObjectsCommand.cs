using CommonControls.Common;
using Serilog;
using System.Collections.Generic;
using System.Linq;
using View3D.Commands.Face;
using View3D.Components.Component.Selection;
using View3D.SceneNodes;

namespace View3D.Commands.Object
{
    public class DeleteObjectsCommand : ICommand
    {
        ILogger _logger = Logging.Create<DeleteObjectsCommand>();
        List<SceneNode> _itemsToDelete;
        SelectionManager _selectionManager;
        ISelectionState _oldState;

        public string HintText { get => "Delete Object"; }
        public bool IsMutation { get => true; }

        public DeleteObjectsCommand(SelectionManager selectionManager)
        {
            _selectionManager = selectionManager;
        }

        public void Configure(List<ISelectable> itemsToDelete)
        {
            _itemsToDelete = new List<SceneNode>(itemsToDelete.Select(x=>x as SceneNode));
        }

        public void Configure(SceneNode itemToDelete)
        {
            _itemsToDelete = new List<SceneNode>() { itemToDelete };
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

            _selectionManager.SetState(_oldState);
        }
    }
}
