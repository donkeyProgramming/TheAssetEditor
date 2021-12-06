using Common;
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
                    var hack = _objectToSplit as Rmv2MeshNode;
                    var originalRmvModel = hack.RmvModel_depricated;

                    //var context = new GraphicsCardGeometry(_resourceLib.GraphicsDevice);
                    //var meshNode = new Rmv2MeshNode(hack.RmvModel_depricated.Clone(), _objectToSplit.Geometry.ParentSkeletonName,  context, _resourceLib, hack.AnimationPlayer, mesh);
                    //var mesh = MeshBuilderService.BuildMeshFromRmvModel(hack.RmvModel_depricated.Clone(), _objectToSplit.Geometry.ParentSkeletonName, context);

                    var meshNode = new Rmv2MeshNode(hack.RmvModel_depricated.Clone(), hack.AnimationPlayer, mesh);
                    meshNode.Initialize(_resourceLib);

                    




                    meshNode.Name = $"{_objectToSplit.Name}_submesh_{counter++}";

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
