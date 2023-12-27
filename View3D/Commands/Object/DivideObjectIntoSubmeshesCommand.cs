using System.Collections.Generic;
using System.Linq;
using CommonControls.Common;
using Serilog;
using View3D.Commands.Face;
using View3D.Components.Component.Selection;
using View3D.Components.Rendering;
using View3D.Rendering.Shading;
using View3D.SceneNodes;
using View3D.Services;
using View3D.Utility;

namespace View3D.Commands.Object
{
    public class DivideObjectIntoSubmeshesCommand : ICommand
    {
        ILogger _logger = Logging.Create<FaceSelectionCommand>();

        IEditableGeometry _objectToSplit;
        bool _combineOverlappingVertexes;

        private readonly List<GroupNode> _newGroupNodes = new List<GroupNode>();
        private readonly SelectionManager _selectionManager;
        ISelectionState _originalSelectionState;
        private readonly ResourceLibary _resourceLib;
        private readonly RenderEngineComponent _renderEngineComponent;

        public string HintText { get => "Divide Object"; }
        public bool IsMutation { get => true; }

        public DivideObjectIntoSubmeshesCommand( SelectionManager selectionManager, ResourceLibary resourceLibary, RenderEngineComponent renderEngineComponent)
        {
            _selectionManager = selectionManager;
            _resourceLib = resourceLibary;
            _renderEngineComponent = renderEngineComponent;
        }

        public void Configure(IEditableGeometry objectToSplit, bool combineOverlappingVertexes)
        {
            _objectToSplit = objectToSplit;
            _combineOverlappingVertexes = combineOverlappingVertexes;
        }

        public void Execute()
        {
            _originalSelectionState = _selectionManager.GetStateCopy();

            using (new WaitCursor())
            {
                var meshService = new MeshSplitterService();
                var newSplitMeshes = meshService.SplitMesh(_objectToSplit.Geometry, _combineOverlappingVertexes);
                var sortedNewMeshes = newSplitMeshes.OrderBy(x => x.VertexCount()).ToList();

                _logger.Here().Information($"{newSplitMeshes.Count} meshes generated from splitting");

                var parent = _objectToSplit.Parent;
                if (parent is GroupNode groupNode && groupNode.IsUngroupable)
                    parent = parent.Parent;

                GroupNode currentGroupNode = null;

                int counter = 0;
                List<Rmv2MeshNode> createdMeshes = new List<Rmv2MeshNode>();

                foreach (var mesh in newSplitMeshes)
                {
                    if (counter % 500 == 0)
                    {
                        if (currentGroupNode != null)
                        {
                            currentGroupNode.IsVisible = false;
                            currentGroupNode.IsExpanded = false;
                            currentGroupNode.Name += "_500";
                        }

                        currentGroupNode = parent.AddObject(new GroupNode(_objectToSplit.Name + "_Collection") { IsSelectable = true, IsUngroupable = true, IsLockable = true });
                        _newGroupNodes.Add(currentGroupNode);
                    }

                    var typedObject = _objectToSplit as Rmv2MeshNode;
                    var shader = typedObject.Effect.Clone() as PbrShader;
                    var meshNode = new Rmv2MeshNode(typedObject.CommonHeader, mesh, typedObject.Material.Clone(), typedObject.AnimationPlayer, _renderEngineComponent, shader);
                    meshNode.Initialize(_resourceLib);
                    meshNode.IsVisible = true;

                    var meshName = $"{_objectToSplit.Name}_submesh_{counter++}";
                    meshNode.Name = meshName;
                    meshNode.Material.ModelName = meshName;

                    createdMeshes.Add(meshNode);
                    currentGroupNode.AddObject(meshNode);
                }

                _objectToSplit.Parent.RemoveObject(_objectToSplit as SceneNode);

                var newState = (ObjectSelectionState)_selectionManager.CreateSelectionSate(GeometrySelectionMode.Object, null);
                if (_newGroupNodes.Count == 1)
                    newState.ModifySelection(createdMeshes, false);
            }
        }

        public void Undo()
        {
            foreach (var item in _newGroupNodes)
                item.Parent.RemoveObject(item);

            _objectToSplit.Parent.AddObject(_objectToSplit as SceneNode);

            _selectionManager.SetState(_originalSelectionState);
        }
    }
}
