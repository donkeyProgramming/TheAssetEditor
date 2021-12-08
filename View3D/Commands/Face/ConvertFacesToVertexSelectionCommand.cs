using MonoGame.Framework.WpfInterop;
using System.Collections.Generic;
using System.Linq;
using View3D.Components.Component.Selection;

namespace View3D.Commands.Face
{
    public class ConvertFacesToVertexSelectionCommand : CommandBase<DeleteFaceCommand>
    {
        SelectionManager _selectionManager;
        FaceSelectionState _originalSelectionState;

        public ConvertFacesToVertexSelectionCommand(FaceSelectionState currentSelectionState)
        {
            _originalSelectionState = currentSelectionState;
        }

        public override string GetHintText() => "Convert Faces To Vertex";

        protected override void ExecuteCommand()
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
            _componentManager.GetComponent<SelectionManager>().SetState(vertexState);
        }

        protected override void UndoCommand()
        {
            _componentManager.GetComponent<SelectionManager>().SetState(_originalSelectionState);
        }
    }
}
