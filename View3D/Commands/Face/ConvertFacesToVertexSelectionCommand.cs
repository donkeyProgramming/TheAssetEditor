using MonoGame.Framework.WpfInterop;
using System.Collections.Generic;
using System.Linq;
using View3D.Components.Component.Selection;

namespace View3D.Commands.Face
{
    public class ConvertFacesToVertexSelectionCommand : CommandBase<DeleteFaceCommand>
    {
        SelectionManager _selectionManager;

        FaceSelectionState _selectionState;

        public ConvertFacesToVertexSelectionCommand(FaceSelectionState selectionState)
        {
            _selectionState = selectionState;
        }

        public override void Initialize(IComponentManager componentManager)
        {
            _selectionManager = componentManager.GetComponent<SelectionManager>();
        }

        public override string GetHintText()
        {
            return "Convert Faces To Vertex";
        }

        protected override void ExecuteCommand()
        {
            var renderObject = _selectionState.RenderObject;
            var geometry = renderObject.Geometry;

            var selectedFaceIndecies = new List<int>();
            var indexBuffer = geometry.GetIndexBuffer();
            foreach (var face in _selectionState.SelectedFaces)
            {
                selectedFaceIndecies.Add(indexBuffer[face]);
                selectedFaceIndecies.Add(indexBuffer[face + 1]);
                selectedFaceIndecies.Add(indexBuffer[face + 2]);
            }

            var distinctValues = selectedFaceIndecies.Distinct();
            var vertexState = new VertexSelectionState(renderObject, 0);
            vertexState.ModifySelection(distinctValues, false);
            _selectionManager.SetState(vertexState);
        }

        protected override void UndoCommand()
        {
            _selectionManager.SetState(_selectionState);
        }
    }
}
