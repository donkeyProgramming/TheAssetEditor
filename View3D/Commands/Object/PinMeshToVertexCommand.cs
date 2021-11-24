using Microsoft.Xna.Framework;
using MonoGame.Framework.WpfInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using View3D.Animation;
using View3D.Components.Component.Selection;
using View3D.Rendering.Geometry;
using View3D.SceneNodes;

namespace View3D.Commands.Object
{
    public class PinMeshToVertexCommand : CommandBase<PinMeshToVertexCommand>
    {
        ISelectionState _selectionOldState;
        SelectionManager _selectionManager;

        List<MeshObject> _originalGeos;
        List<string> _originalSkeletonNames;

        List<Rmv2MeshNode> _meshesToPin;
        Rmv2MeshNode _source;
        int _vertexId;


        public PinMeshToVertexCommand(IEnumerable<Rmv2MeshNode> meshesToPin, Rmv2MeshNode source, int vertexId)
        {
            _meshesToPin = meshesToPin.ToList();
            _source = source;
            _vertexId = vertexId;
        }

        public override string GetHintText()
        {
            return "Pin meshes to vertex";
        }

        public override void Initialize(IComponentManager componentManager)
        {
            _selectionManager = componentManager.GetComponent<SelectionManager>();
            base.Initialize(componentManager);
        }

        protected override void ExecuteCommand()
        {
            // Create undo state
            _originalGeos = _meshesToPin.Select(x => x.Geometry.Clone()).ToList();
            _originalSkeletonNames = _meshesToPin.Select(x => x.Geometry.ParentSkeletonName).ToList();
            _selectionOldState = _selectionManager.GetStateCopy();

            // Update the meshes
            var sourceVert = _source.Geometry.GetVertexExtented(_vertexId);
            foreach (var currentMesh in _meshesToPin)
            {
                currentMesh.Geometry.ParentSkeletonName = _source.Geometry.ParentSkeletonName;

                for (int i = 0; i < currentMesh.Geometry.VertexCount(); i++)
                {
                    currentMesh.Geometry.SetVertexBlendIndex(i, sourceVert.BlendIndices);
                    currentMesh.Geometry.SetVertexWeights(i, sourceVert.BlendWeights);
                }

                currentMesh.Geometry.RebuildVertexBuffer();
            }
        }

        protected override void UndoCommand()
        {
            for (int i = 0; i < _meshesToPin.Count; i++)
            {
                _meshesToPin[i].Geometry = _originalGeos[i];
                _meshesToPin[i].Geometry.ParentSkeletonName = _originalSkeletonNames[i];
            }

            _selectionManager.SetState(_selectionOldState);
        }
    }
}
