using System.Collections.Generic;
using View3D.Components.Component.Selection;
using View3D.SceneNodes;

namespace View3D.Commands.Face
{
    public class DuplicateFacesCommand : CommandBase<DuplicateFacesCommand>
    {
        SelectionManager _selectionManager;

        // Undo variables
        ISelectionState _oldState;
        ISelectable _newObject;

        // Input variables
        List<int> _facesToDelete;
        ISelectable _inputNode;
        bool _deleteOriginal;

        public DuplicateFacesCommand(SelectionManager selectionManager)
        {
            _selectionManager = selectionManager;
        }

        public void Configure(ISelectable geoObject, List<int> facesToDelete, bool deleteOriginal)
        {
            _facesToDelete = facesToDelete;
            _inputNode = geoObject;
            _deleteOriginal = deleteOriginal;
        }

        public override string GetHintText() => "Duplicate Faces";

        protected override void ExecuteCommand()
        {
            _oldState = _selectionManager.GetStateCopy();

            // Clone the object
            _newObject = SceneNodeHelper.CloneNode(_inputNode);

            // Add to the scene
            if (!_deleteOriginal)
                _newObject.Name += "_copy";
            _newObject.Parent.AddObject(_newObject);

            var selectedFaceIndecies = new List<ushort>();
            var indexBuffer = _newObject.Geometry.GetIndexBuffer();
            foreach (var face in _facesToDelete)
            {
                selectedFaceIndecies.Add(indexBuffer[face]);
                selectedFaceIndecies.Add(indexBuffer[face + 1]);
                selectedFaceIndecies.Add(indexBuffer[face + 2]);
            }

            _newObject.Geometry.RemoveUnusedVertexes(selectedFaceIndecies.ToArray());

            if (_deleteOriginal)
                _inputNode.Parent.RemoveObject(_inputNode);

            // Object state
            var objectState = new ObjectSelectionState();
            objectState.ModifySelectionSingleObject(_newObject, false);
            _selectionManager.SetState(objectState);
        }

        protected override void UndoCommand()
        {
            _newObject.Parent.RemoveObject(_newObject);

            if (_deleteOriginal)
                _inputNode.Parent.AddObject(_inputNode);

            _selectionManager.SetState(_oldState);
        }
    }
}
