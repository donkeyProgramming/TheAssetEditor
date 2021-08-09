using MonoGame.Framework.WpfInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using View3D.Components.Component.Selection;
using View3D.SceneNodes;

namespace View3D.Commands.Object
{
    public class GroupObjectsCommand : CommandBase<GroupObjectsCommand>
    {
        SelectionManager _selectionManager;
        ISelectionState _oldState;

        ISceneNode _parent;
        List<ISelectable> _itemsToGroup { get; set; } = new List<ISelectable>();

        public GroupObjectsCommand(ISceneNode parent, List<ISelectable> itemsToGroup)
        {
            _itemsToGroup = itemsToGroup;
            _parent = parent;
        }

        public override string GetHintText()
        {
            return "Group Objects";
        }

        public override void Initialize(IComponentManager componentManager)
        {
            _selectionManager = componentManager.GetComponent<SelectionManager>();
        }

        protected override void ExecuteCommand()
        {
            _oldState = _selectionManager.GetStateCopy();
            var groupNode = _parent.AddObject(new GroupNode("New Group") { IsUngroupable = true, IsSelectable = true, IsLockable = true });

            foreach (var item in _itemsToGroup)
            {
                item.Parent.RemoveObject(item);
                groupNode.AddObject(item);
            }

            var currentState = _selectionManager.GetState() as ObjectSelectionState;
            currentState.Clear();

            var itemsToSelect = groupNode.Children.Where(x => (x as ISelectable)?.IsSelectable == true).Select(x=>x as ISelectable).ToList();
            currentState.ModifySelection(itemsToSelect, false);
        }

        protected override void UndoCommand()
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

    public class UnGroupObjectsCommand : CommandBase<UnGroupObjectsCommand>
    {
        SelectionManager _selectionManager;
        ISelectionState _oldState;

        ISceneNode _parent;
        List<ISelectable> _itemsToUngroup { get; set; } = new List<ISelectable>();
        ISceneNode _oldGroupNode;

        public UnGroupObjectsCommand(ISceneNode parent, List<ISelectable> itemsToUngroup, ISceneNode groupNode)
        {
            _itemsToUngroup = itemsToUngroup;
            _parent = parent;
            _oldGroupNode = groupNode;
        }

        public override string GetHintText()
        {
            return "Ungroup objects";
        }

        public override void Initialize(IComponentManager componentManager)
        {
            _selectionManager = componentManager.GetComponent<SelectionManager>();
        }

        protected override void ExecuteCommand()
        {
            _oldState = _selectionManager.GetStateCopy();

            foreach (var item in _itemsToUngroup)
            {
                item.Parent.RemoveObject(item);
                _parent.AddObject(item);
            }

            if (_oldGroupNode.Children.Count == 0)
                _oldGroupNode.Parent.RemoveObject(_oldGroupNode);

            var currentState = _selectionManager.GetState() as ObjectSelectionState;
            currentState.Clear();
            currentState.ModifySelection(_itemsToUngroup, false);
        }

        protected override void UndoCommand()
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

    public class AddObjectsToExistingGroupCommand : CommandBase<GroupObjectsCommand>
    {
        SelectionManager _selectionManager;
        ISelectionState _oldState;

        GroupNode _group;
        List<ISelectable> _itemsToGroup { get; set; } = new List<ISelectable>();

        public AddObjectsToExistingGroupCommand(GroupNode group, List<ISelectable> itemsToGroup)
        {
            _itemsToGroup = itemsToGroup;
            _group = group;
        }

        public override string GetHintText()
        {
            return "Add Objects to group";
        }

        public override void Initialize(IComponentManager componentManager)
        {
            _selectionManager = componentManager.GetComponent<SelectionManager>();
        }

        protected override void ExecuteCommand()
        {
            _oldState = _selectionManager.GetStateCopy();
            var groupNode = _group;

            foreach (var item in _itemsToGroup)
            {
                item.Parent.RemoveObject(item);
                groupNode.AddObject(item);
            }

            var currentState = _selectionManager.GetState() as ObjectSelectionState;
            currentState.Clear();
            currentState.ModifySelection(_itemsToGroup, false);
        }

        protected override void UndoCommand()
        {
            var rootNode = _group.Parent;

            foreach (var item in _itemsToGroup)
            {
                item.Parent.RemoveObject(item);
                rootNode.AddObject(item);
            }

            _selectionManager.SetState(_oldState);
        }
    }
}
