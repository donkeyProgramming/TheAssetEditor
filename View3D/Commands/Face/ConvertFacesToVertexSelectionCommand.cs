using Common;
using MonoGame.Framework.WpfInterop;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using View3D.Components.Component.Selection;
using View3D.Rendering;
using View3D.Rendering.Geometry;

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
            var vertexState = new VertexSelectionState()
            {
                RenderObject = renderObject,
                SelectedVertices = distinctValues.ToList()
            };

            _selectionManager.SetState(vertexState);
        }

        protected override void UndoCommand()
        {
            _selectionManager.SetState(_selectionState);
        }
    }
}
