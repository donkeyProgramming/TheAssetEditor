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
using View3D.Services;

namespace View3D.Commands.Object
{
    class DivideObjectIntoSubmeshesCommand : CommandBase<DivideObjectIntoSubmeshesCommand>
    {
        IDrawableNode _objectToSplit;
        List<MeshNode> _newMeshes = new List<MeshNode>();

        IEditableMeshResolver _editableMeshResolver;
        SceneManager _sceneManager;
        SelectionManager _selectionManager;
        ISelectionState _originalSelectionState;

        public DivideObjectIntoSubmeshesCommand(IDrawableNode objectToSplit)
        {
            _objectToSplit = objectToSplit;
        }

        public override void Initialize(IComponentManager componentManager)
        {
            _editableMeshResolver = componentManager.GetComponent<IEditableMeshResolver>();
            _sceneManager = componentManager.GetComponent<SceneManager>();
            _selectionManager = componentManager.GetComponent<SelectionManager>();
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
                var meshNode = RenderItemHelper.CreateRenderItem(mesh, new Vector3(0, 0, 0), new Vector3(1), $"{_objectToSplit.Name}_submesh_{counter++}", _sceneManager.GraphicsDevice);
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
