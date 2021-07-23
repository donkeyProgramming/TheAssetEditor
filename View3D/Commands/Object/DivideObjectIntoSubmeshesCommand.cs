using Common;
using CommonControls.Common;
using Filetypes.RigidModel;
using Microsoft.Xna.Framework;
using MonoGame.Framework.WpfInterop;
using Serilog;
using System;
using System.Collections.Generic;
using System.Text;
using View3D.Components;
using View3D.Components.Component;
using View3D.Components.Component.Selection;
using View3D.Rendering;
using View3D.Rendering.Geometry;
using View3D.Rendering.Shading;
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
                _newGroupNode = (GroupNode)parent.AddObject(new GroupNode(_objectToSplit.Name + "_Collection") { IsSelectable = true, IsUngroupable = true, IsLockable=true});

                int counter = 0;
                List<Rmv2MeshNode> createdMeshes = new List<Rmv2MeshNode>();

                foreach (var mesh in splitMeshes)
                {
                    var hack = _objectToSplit as Rmv2MeshNode;
                    var originalRmvModel = hack.MeshModel;

                    var context = new GeometryGraphicsContext(_resourceLib.GraphicsDevice);
                    var meshNode = new Rmv2MeshNode(hack.MeshModel.Clone(), context, _resourceLib, hack.AnimationPlayer, mesh);
                    meshNode.Name = $"{_objectToSplit.Name}_submesh_{counter++}";
                    createdMeshes.Add(meshNode);
                    _newGroupNode.AddObject(meshNode);
                }

                _objectToSplit.Parent.RemoveObject(_objectToSplit as SceneNode);

                var newState = (ObjectSelectionState)_selectionManager.CreateSelectionSate(GeometrySelectionMode.Object, null);
                foreach (var node in createdMeshes)
                    newState.ModifySelection(node, false);
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
