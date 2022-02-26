using CommonControls.Common;
using MonoGame.Framework.WpfInterop;
using System.Collections.Generic;
using View3D.Components.Component;
using View3D.Components.Component.Selection;
using View3D.SceneNodes;
using View3D.Services;
using View3D.Utility;

namespace View3D.Commands.Object
{
    class DivideObjectIntoSubmeshesCommand : CommandBase<DivideObjectIntoSubmeshesCommand>
    {
        IEditableGeometry _objectToSplit;
        bool _combineOverlappingVertexes;

        GroupNode _newGroupNode;

        IEditableMeshResolver _editableMeshResolver;
        SceneManager _sceneManager;
        SelectionManager _selectionManager;
        ISelectionState _originalSelectionState;
        ResourceLibary _resourceLib;
        IComponentManager _componentManager;

        public DivideObjectIntoSubmeshesCommand(IEditableGeometry objectToSplit, bool combineOverlappingVertexes)
        {
            _objectToSplit = objectToSplit;
            _combineOverlappingVertexes = combineOverlappingVertexes;
        }

        public override string GetHintText()
        {
            return "Divide Object";
        }

        public override void Initialize(IComponentManager componentManager)
        {
            _componentManager = componentManager;
            _editableMeshResolver = componentManager.GetComponent<IEditableMeshResolver>();
            _sceneManager = componentManager.GetComponent<SceneManager>();
            _selectionManager = componentManager.GetComponent<SelectionManager>();
            _resourceLib = componentManager.GetComponent<ResourceLibary>();
        }

        protected override void ExecuteCommand()
        {
            _originalSelectionState = _selectionManager.GetStateCopy();

            using (new WaitCursor())
            {
                var meshService = new MeshSplitterService();
                var splitMeshes = meshService.SplitMesh(_objectToSplit.Geometry, _combineOverlappingVertexes);
                _logger.Here().Information($"{splitMeshes.Count} meshes generated from splitting");

                var parent = _objectToSplit.Parent;
                if (parent is GroupNode groupNode && groupNode.IsUngroupable)
                    parent = parent.Parent;
                _newGroupNode = parent.AddObject(new GroupNode(_objectToSplit.Name + "_Collection") { IsSelectable = true, IsUngroupable = true, IsLockable=true});

                int counter = 0;
                List<Rmv2MeshNode> createdMeshes = new List<Rmv2MeshNode>();

                foreach (var mesh in splitMeshes)
                {
                    var typedObject = _objectToSplit as Rmv2MeshNode;
                    var meshNode = new Rmv2MeshNode(typedObject.CommonHeader, mesh, typedObject.Material.Clone(), typedObject.AnimationPlayer, _componentManager);
                    meshNode.Initialize(_resourceLib);
                    
                    var meshName = $"{_objectToSplit.Name}_submesh_{counter++}";
                    meshNode.Name = meshName;
                    meshNode.Material.ModelName = meshName;

                    createdMeshes.Add(meshNode);
                    _newGroupNode.AddObject(meshNode);
                }

                _objectToSplit.Parent.RemoveObject(_objectToSplit as SceneNode);

                var newState = (ObjectSelectionState)_selectionManager.CreateSelectionSate(GeometrySelectionMode.Object, null);
                newState.ModifySelection(createdMeshes, false);
            }
        }

        protected override void UndoCommand()
        {
            _newGroupNode.Parent.RemoveObject(_newGroupNode);
            _objectToSplit.Parent.AddObject(_objectToSplit as SceneNode);

            _selectionManager.SetState(_originalSelectionState);
        }
    }
}
