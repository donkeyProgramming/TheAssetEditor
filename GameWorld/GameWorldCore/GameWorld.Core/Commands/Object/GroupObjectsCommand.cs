using System.Collections.Generic;
using System.Linq;
using GameWorld.Core.Commands;
using GameWorld.Core.Components.Selection;
using GameWorld.Core.SceneNodes;

namespace GameWorld.Core.Commands.Object
{
    public class GroupObjectsCommand : ICommand
    {
        SelectionManager _selectionManager;
        ISelectionState _oldState;

        ISceneNode _parent;
        List<ISelectable> _itemsToGroup { get; set; } = new List<ISelectable>();

        public void Configure(ISceneNode parent, List<ISelectable> itemsToGroup)
        {
            _itemsToGroup = itemsToGroup;
            _parent = parent;
        }

        public string HintText { get => "Group Objects"; }
        public bool IsMutation { get => true; }


        public GroupObjectsCommand(SelectionManager selectionManager)
        {
            _selectionManager = selectionManager;
        }

        public void Execute()
        {
            _oldState = _selectionManager.GetStateCopy();
            var groupNode = _parent.AddObject(new GroupNode("New Group") { IsUngroupable = true, IsSelectable = true, IsLockable = true });

            foreach (var item in _itemsToGroup)
            {
                item.Parent.RemoveObject(item);
                groupNode.AddObject(item);
            }

            var currentState = _selectionManager.GetState<ObjectSelectionState>();
            currentState.Clear();

            var itemsToSelect = groupNode.Children.Where(x => (x as ISelectable)?.IsSelectable == true).Select(x => x as ISelectable).ToList();
            currentState.ModifySelection(itemsToSelect, false);
        }

        public void Undo()
        {
            var groupNode = _itemsToGroup.First().Parent;

            foreach (var item in _itemsToGroup)
            {
                item.Parent.RemoveObject(item);
                _parent.AddObject(item);
            }

            _parent.RemoveObject(groupNode);

            _selectionManager.SetState(_oldState);
        }
    }

    public class UnGroupObjectsCommand : ICommand
    {
        SelectionManager _selectionManager;
        ISelectionState _oldState;

        ISceneNode _parent;
        List<ISelectable> _itemsToUngroup { get; set; } = new List<ISelectable>();
        ISceneNode _oldGroupNode;

        public void Configure(ISceneNode parent, List<ISelectable> itemsToUngroup, ISceneNode groupNode)
        {
            _itemsToUngroup = itemsToUngroup;
            _parent = parent;
            _oldGroupNode = groupNode;
        }


        public string HintText { get => "Ungroup objects"; }
        public bool IsMutation { get => true; }

        public UnGroupObjectsCommand(SelectionManager selectionManager)
        {
            _selectionManager = selectionManager;
        }

        public void Execute()
        {
            _oldState = _selectionManager.GetStateCopy();

            foreach (var item in _itemsToUngroup)
            {
                item.Parent.RemoveObject(item);
                _parent.AddObject(item);
            }

            if (_oldGroupNode.Children.Count == 0)
                _oldGroupNode.Parent.RemoveObject(_oldGroupNode);

            var currentState = _selectionManager.GetState<ObjectSelectionState>();
            currentState.Clear();
            currentState.ModifySelection(_itemsToUngroup, false);
        }

        public void Undo()
        {
            foreach (var item in _itemsToUngroup)
            {
                item.Parent.RemoveObject(item);
                _oldGroupNode.AddObject(item);
            }

            if (_oldGroupNode.Parent.Children.Contains(_oldGroupNode) == false)
                _oldGroupNode.Parent.AddObject(_oldGroupNode);


            _selectionManager.SetState(_oldState);
        }
    }

    public class AddObjectsToGroupCommand : ICommand
    {
        ISelectionState _originalSelectionState;

        GroupNode _groupToAddItemsTo;
        private readonly SelectionManager _selectionManager;

        List<ISelectable> _itemsToAdd { get; set; } = new List<ISelectable>();

        public void Configure(GroupNode groupToAddItemsTo, List<ISelectable> itemsToAdd)
        {
            _itemsToAdd = itemsToAdd;
            _groupToAddItemsTo = groupToAddItemsTo;
        }

        public AddObjectsToGroupCommand(SelectionManager selectionManager)
        {
            _selectionManager = selectionManager;
        }

        public string HintText { get => "Add Objects to group"; }
        public bool IsMutation { get => true; }

        public void Execute()
        {
            // Create undo state
            _originalSelectionState = _selectionManager.GetStateCopy();

            // Add itsms to group
            foreach (var item in _itemsToAdd)
            {
                item.Parent.RemoveObject(item);
                _groupToAddItemsTo.AddObject(item);
            }

            // Select the grouped items
            var currentState = _selectionManager.GetState<ObjectSelectionState>();
            currentState.Clear();
            currentState.ModifySelection(_itemsToAdd, false);
        }

        public void Undo()
        {
            var rootNode = _groupToAddItemsTo.Parent;
            foreach (var item in _itemsToAdd)
            {
                item.Parent.RemoveObject(item);
                rootNode.AddObject(item);
            }

            _selectionManager.SetState(_originalSelectionState);
        }
    }
}
