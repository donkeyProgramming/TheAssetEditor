using Common;
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
        List<Rmv2MeshNode> _newMeshes = new List<Rmv2MeshNode>();

        IEditableMeshResolver _editableMeshResolver;
        SceneManager _sceneManager;
        SelectionManager _selectionManager;
        ISelectionState _originalSelectionState;
        ResourceLibary _resourceLib;

        public DivideObjectIntoSubmeshesCommand(IEditableGeometry objectToSplit)
        {
            _objectToSplit = objectToSplit;
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

            var meshService = new MeshSplitterService();
            var newMeshes = meshService.SplitMesh(_objectToSplit.Geometry);

            _logger.Here().Information($"{newMeshes.Count} meshes generated from splitting");

            int counter = 0;
            foreach (var mesh in newMeshes)
            {
                var hack = _objectToSplit as Rmv2MeshNode;
                var meshNode = new Rmv2MeshNode(mesh, $"{_objectToSplit.Name}_submesh_{counter++}", hack.AnimationPlayer, hack.Effect.Clone() as PbrShader); 
                _newMeshes.Add(meshNode);
                _editableMeshResolver.GetEditableMeshNode().AddObject(meshNode);
            }

            _objectToSplit.Parent.RemoveObject(_objectToSplit as SceneNode);

            var newState = (ObjectSelectionState)_selectionManager.CreateSelectionSate(GeometrySelectionMode.Object);
            foreach (var node in _newMeshes)
                newState.ModifySelection(node);
        }

        protected override void UndoCommand()
        {
            foreach (var item in _newMeshes)
            {
                if (item.Parent != null)
                    item.Parent.RemoveObject(item);
            }

            _objectToSplit.Parent.AddObject(_objectToSplit as SceneNode);

            _selectionManager.SetState(_originalSelectionState);
        }
    }
}
