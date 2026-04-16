using System.Collections.Generic;
using GameWorld.Core.Commands;
using GameWorld.Core.Components.Selection;
using GameWorld.Core.Rendering.Geometry;

namespace GameWorld.Core.Commands.Face
{
    public class DeleteFaceCommand : ICommand
    {
        FaceSelectionState _originalSelectionState;
        MeshObject _originalGeometry;

        List<int> _facesToDelete;
        MeshObject _geo;
        private readonly SelectionManager _selectionManager;


        public string HintText { get => "Delete Faces"; }
        public bool IsMutation { get => true; }

        public DeleteFaceCommand(SelectionManager selectionManager)
        {
            _selectionManager = selectionManager;
        }

        public void Configure(MeshObject geoObject, List<int> facesToDelete)
        {
            _facesToDelete = facesToDelete;
            _geo = geoObject;
        }

        public void Execute()
        {
            // Create undo state
            _originalSelectionState = _selectionManager.GetStateCopy<FaceSelectionState>();
            _originalGeometry = _geo.Clone();

            // Execute
            _geo.RemoveFaces(_facesToDelete);
            _selectionManager.GetState<FaceSelectionState>().Clear();
        }

        public void Undo()
        {
            _originalSelectionState.RenderObject.Geometry = _originalGeometry;
            _selectionManager.SetState(_originalSelectionState);
        }
    }
}
