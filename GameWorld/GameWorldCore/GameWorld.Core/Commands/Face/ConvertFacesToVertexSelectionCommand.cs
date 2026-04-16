using System.Collections.Generic;
using System.Linq;
using GameWorld.Core.Components.Selection;

namespace GameWorld.Core.Commands.Face
{
    public class ConvertFacesToVertexSelectionCommand : ICommand
    {
        private readonly SelectionManager _selectionManager;
        FaceSelectionState _originalSelectionState;

        public string HintText { get => "Convert Faces To Vertex"; }
        public bool IsMutation { get => true; }

        public ConvertFacesToVertexSelectionCommand(SelectionManager selectionManager)
        {
            _selectionManager = selectionManager;
        }

        public void Configure(FaceSelectionState currentSelectionState)
        {
            _originalSelectionState = currentSelectionState;
        }

        public void Execute()
        {
            var renderObject = _originalSelectionState.RenderObject;
            var geometry = renderObject.Geometry;

            var selectedFaceIndecies = new List<int>();
            var indexBuffer = geometry.GetIndexBuffer();
            foreach (var face in _originalSelectionState.SelectedFaces)
            {
                selectedFaceIndecies.Add(indexBuffer[face]);
                selectedFaceIndecies.Add(indexBuffer[face + 1]);
                selectedFaceIndecies.Add(indexBuffer[face + 2]);
            }

            var vertexState = new VertexSelectionState(renderObject, 0);
            vertexState.ModifySelection(selectedFaceIndecies.Distinct(), false);
            _selectionManager.SetState(vertexState);
        }

        public void Undo()
        {
            _selectionManager.SetState(_originalSelectionState);
        }
    }
}
