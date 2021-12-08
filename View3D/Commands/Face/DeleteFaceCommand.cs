using System.Collections.Generic;
using View3D.Components.Component.Selection;
using View3D.Rendering.Geometry;

namespace View3D.Commands.Face
{
    public class DeleteFaceCommand : CommandBase<DeleteFaceCommand>
    {
        FaceSelectionState _originalSelectionState;
        MeshObject _originalGeometry;

        List<int> _facesToDelete;
        MeshObject _geo;

        public DeleteFaceCommand(MeshObject geoObject, List<int> facesToDelete)
        {
            _facesToDelete = facesToDelete;
            _geo = geoObject;
        }

        public override string GetHintText() => "Delete Faces";

        protected override void ExecuteCommand()
        {
            // Create undo state
            _originalSelectionState = _componentManager.GetComponent<SelectionManager>().GetStateCopy<FaceSelectionState>();
            _originalGeometry = _geo.Clone();

            // Execute
            _geo.RemoveFaces(_facesToDelete);
            _componentManager.GetComponent<SelectionManager>().GetState<FaceSelectionState>().Clear();
        }

        protected override void UndoCommand()
        {
            _originalSelectionState.RenderObject.Geometry = _originalGeometry;
            _componentManager.GetComponent<SelectionManager>().SetState(_originalSelectionState);
        }
    }
}
