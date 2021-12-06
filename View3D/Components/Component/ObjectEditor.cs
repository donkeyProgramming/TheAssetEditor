using CommonControls.Common;
using MonoGame.Framework.WpfInterop;
using Serilog;
using System.Collections.Generic;
using System.Linq;
using View3D.Commands.Object;
using View3D.Components.Component.Selection;
using View3D.SceneNodes;
using View3D.Utility;
using static CommonControls.BaseDialogs.ErrorListDialog.ErrorListViewModel;

namespace View3D.Components.Component
{
    public class ObjectEditor : BaseComponent
    {
        ILogger _logger = Logging.Create<ObjectEditor>();

        CommandExecutor _commandManager;

        public ObjectEditor(WpfGame game) : base(game)
        {
        }

        public override void Initialize()
        {
            _commandManager = GetComponent<CommandExecutor>();
            base.Initialize();
        }


        public void DeleteObject(ObjectSelectionState objectSelectionState)
        {
            var selection = objectSelectionState.CurrentSelection();
            if (selection.Count != 0)
            {
                var command = new DeleteObjectsCommand(selection);
                _commandManager.ExecuteCommand(command);
            }
        }

        public void DuplicateObject(ObjectSelectionState objectSelectionState)
        {
            if (objectSelectionState.CurrentSelection().Count != 0)
            {
                var command = new DuplicateObjectCommand(objectSelectionState.CurrentSelection().Select(x => (ISceneNode)x).ToList());
                _commandManager.ExecuteCommand(command);
            }
        }

        public void DivideIntoSubmeshes(ObjectSelectionState objectSelectionState, bool combineOverlappingVertexes)
        {
            if (objectSelectionState.GetSingleSelectedObject() is IEditableGeometry drawableNode)
            {
                var command = new DivideObjectIntoSubmeshesCommand(drawableNode, combineOverlappingVertexes);
                _commandManager.ExecuteCommand(command);
            }
        }

        public bool CombineMeshes(ObjectSelectionState objectSelectionState, out ErrorList errorMessages)
        {
            ModelCombiner modelValidator = new ModelCombiner();
            var objs = objectSelectionState.SelectedObjects().Where(x => x is Rmv2MeshNode).Select(x => x as Rmv2MeshNode);
            if (!modelValidator.CanCombine(objs.ToList(), out errorMessages))
                return false;

            var command = new CombineMeshCommand(objectSelectionState.SelectedObjects());
            _commandManager.ExecuteCommand(command);
            
            return true;
        }

        public void ReduceMesh(List<Rmv2MeshNode> meshNodes, float factor, bool undoable)
        {
            var command = new ReduceMeshCommand(meshNodes, factor);
            _commandManager.ExecuteCommand(command, undoable);
        }

        public void ReduceMesh(Rmv2MeshNode meshNode, float factor, bool undoable)
        {
            var command = new ReduceMeshCommand(new List<Rmv2MeshNode>() { meshNode }, factor);
            _commandManager.ExecuteCommand(command, undoable);
        }

        public void GroupItems(ObjectSelectionState selectionState)
        {
            if (selectionState == null)
                return;
            var selectedObjects = selectionState.SelectedObjects();
            if (selectionState == null || selectedObjects.Count == 0)
                return;

            // If all items in same group
            var parents = selectedObjects.Select(x => x.Parent);
            var numDifferentParents = parents.Distinct().Count();
            if (numDifferentParents == 1 && parents.First() is GroupNode groupNode && groupNode.IsUngroupable)
            {
                var ungroupCommand = new UnGroupObjectsCommand(parents.First().Parent, selectedObjects, groupNode);
                _commandManager.ExecuteCommand(ungroupCommand);
                return;
            }

            // If there is a group, but not all items are members of it, add them to the group
            if (numDifferentParents == 2)
            {
                var groupParent = parents
                    .Where(x => x is GroupNode)
                    .Select(x => x as GroupNode)
                    .FirstOrDefault(x => x.IsUngroupable);

                var itemsInGroup = groupParent.Children;
                var itemsToAdd = selectedObjects.Where(x => itemsInGroup.Contains(x) == false).ToList();

                var addItemToGroupCmd = new AddObjectsToExistingGroupCommand(groupParent, itemsToAdd);
                _commandManager.ExecuteCommand(addItemToGroupCmd);
                return;
            }


            // Default - Create a new group and add all to it
            var parent = selectionState.SelectedObjects().First().Parent;
            if (parent is GroupNode parentGroupNode && parentGroupNode.IsUngroupable)
                parent = parent.Parent;
            var cmd = new GroupObjectsCommand(parent, selectionState.CurrentSelection());
            _commandManager.ExecuteCommand(cmd);
            
        }

        public void SortMeshes(ISceneNode node)
        {
            var children = new List<ISceneNode>(node.Children);
            for (int i = 0; i < children.Count; i++)
                node.RemoveObject(children[i]);

            children.Sort((x, y) => x.Name.CompareTo(y.Name));

            for (int i = 0; i < children.Count; i++)
                node.AddObject(children[i]);

            for (int i = 0; i < children.Count; i++)
            {
                if (children[i] is GroupNode)
                    SortMeshes(children[i]);
            }
        }

      
    }
}
