using System.Collections.Generic;
using System.Linq;
using GameWorld.Core.Commands;
using GameWorld.Core.Commands.Object;
using GameWorld.Core.Components.Selection;
using GameWorld.Core.SceneNodes;
using GameWorld.Core.Utility;
using Shared.Core.ErrorHandling;

namespace GameWorld.Core.Services
{
    public class ObjectEditor
    {
        private readonly CommandFactory _commandFactory;

        public ObjectEditor(CommandFactory commandFactory)
        {
            _commandFactory = commandFactory;
        }

        public void DeleteObject(ObjectSelectionState objectSelectionState)
        {
            var selection = objectSelectionState.CurrentSelection();
            if (selection.Count != 0)
                _commandFactory.Create<DeleteObjectsCommand>().Configure(x => x.Configure(selection)).BuildAndExecute();
        }

        public void DuplicateObject(ObjectSelectionState objectSelectionState)
        {
            if (objectSelectionState.CurrentSelection().Count != 0)
            {
                var objectsToCopy = objectSelectionState.CurrentSelection().Select(x => (ISceneNode)x).ToList();
                _commandFactory.Create<DuplicateObjectCommand>().Configure(x => x.Configure(objectsToCopy)).BuildAndExecute();
            }
        }

        public void DivideIntoSubmeshes(ObjectSelectionState objectSelectionState, bool combineOverlappingVertexes)
        {
            if (objectSelectionState.GetSingleSelectedObject() is IEditableGeometry drawableNode)
                _commandFactory.Create<DivideObjectIntoSubmeshesCommand>().Configure(x => x.Configure(drawableNode, combineOverlappingVertexes)).BuildAndExecute();
        }

        public bool CombineMeshes(ObjectSelectionState objectSelectionState, out ErrorList errorMessages)
        {
            var objs = objectSelectionState.SelectedObjects()
                .Cast<Rmv2MeshNode>()
                .Where(x => x != null)
                .ToList();

            var result = ModelCombiner.HasPotentialCombineMeshes(objs, out errorMessages);
            if (result)
            {
                errorMessages = new ErrorList();
                _commandFactory.Create<CombineMeshCommand>().Configure(x => x.Configure(objectSelectionState.SelectedObjects())).BuildAndExecute();
            }

            return result;
        }


        public void ReduceMesh(List<Rmv2MeshNode> meshNodes, float factor, bool undoable)
        {
            _commandFactory.Create<ReduceMeshCommand>()
                .Configure(x => x.Configure(meshNodes, factor))
                .IsUndoable(undoable)
                .BuildAndExecute();
        }

        public void ReduceMesh(Rmv2MeshNode meshNode, float factor, bool undoable) => ReduceMesh(new List<Rmv2MeshNode> { meshNode }, factor, undoable);

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
                _commandFactory.Create<UnGroupObjectsCommand>().Configure(x => x.Configure(parents.First().Parent, selectedObjects, groupNode)).BuildAndExecute();
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
                _commandFactory.Create<AddObjectsToGroupCommand>().Configure(x => x.Configure(groupParent, itemsToAdd)).BuildAndExecute();
                return;
            }


            // Default - Create a new group and add all to it
            var parent = selectionState.SelectedObjects().First().Parent;
            if (parent is GroupNode parentGroupNode && parentGroupNode.IsUngroupable)
                parent = parent.Parent;

            _commandFactory.Create<GroupObjectsCommand>().Configure(x => x.Configure(parent, selectionState.CurrentSelection())).BuildAndExecute();
        }

        public void SortMeshes(ISceneNode node)
        {
            var children = new List<ISceneNode>(node.Children);
            for (var i = 0; i < children.Count; i++)
                node.RemoveObject(children[i]);

            children.Sort((x, y) => x.Name.CompareTo(y.Name));

            for (var i = 0; i < children.Count; i++)
                node.AddObject(children[i]);

            for (var i = 0; i < children.Count; i++)
            {
                if (children[i] is GroupNode)
                    SortMeshes(children[i]);
            }
        }
    }
}
