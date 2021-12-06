using CommonControls.Common;
using MonoGame.Framework.WpfInterop;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using View3D.Components.Component;
using View3D.Components.Component.Selection;
using View3D.Rendering;
using View3D.SceneNodes;

namespace View3D.Commands.Object
{
    public class DeleteObjectsCommand : CommandBase<DeleteObjectsCommand>
    {
        List<SceneNode> _itemsToDelete;
        SelectionManager _selectionManager;
        ISelectionState _oldState;

        public DeleteObjectsCommand(List<ISelectable> itemsToDelete)
        {
            _itemsToDelete = new List<SceneNode>(itemsToDelete.Select(x=>x as SceneNode));
        }

        public DeleteObjectsCommand(SceneNode itemToDelete)
        {
            _itemsToDelete = new List<SceneNode>() { itemToDelete };
        }

        public override string GetHintText()
        {
            return "Delete Object";
        }

        public override void Initialize(IComponentManager componentManager)
        {
            _selectionManager = componentManager.GetComponent<SelectionManager>();
        }

        protected override void ExecuteCommand()
        {
            _oldState = _selectionManager.GetStateCopy();

           _logger.Here().Information($"Command info - Items[{string.Join(',', _itemsToDelete.Select(x => x.Name))}]");
            foreach (var item in _itemsToDelete)
                item.Parent.RemoveObject(item);

            _selectionManager.CreateSelectionSate(GeometrySelectionMode.Object, null);
        }

        protected override void UndoCommand()
        {
            foreach (var item in _itemsToDelete)
                item.Parent.AddObject(item);

            _selectionManager.SetState(_oldState);
        }
    }
}
