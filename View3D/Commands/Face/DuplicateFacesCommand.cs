using MonoGame.Framework.WpfInterop;
using System;
using System.Collections.Generic;
using System.Text;
using View3D.Components.Component.Selection;
using View3D.SceneNodes;

namespace View3D.Commands.Face
{
   public class DuplicateFacesCommand : CommandBase<DuplicateFacesCommand>
   {
       SelectionManager _selectionManager;
   
        ISelectionState _oldState;
 
   
        List<int> _facesToDelete;
        ISelectable _inputNode;

        ISelectable _newObject;
        bool _deleteOriginal;

       public DuplicateFacesCommand(ISelectable geoObject, List<int> facesToDelete, bool deleteOriginal)
       {
           _facesToDelete = facesToDelete;
           _inputNode = geoObject;
            _deleteOriginal = deleteOriginal;
       }

        public override string GetHintText()
        {
            return "Duplicate Faces";
        }

        public override void Initialize(IComponentManager componentManager)
       {
           _selectionManager = componentManager.GetComponent<SelectionManager>();
       }
   
       protected override void ExecuteCommand()
       {
           _oldState = _selectionManager.GetStateCopy();

            // Clone the object
            _newObject = _inputNode.Clone() as ISelectable;

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
            objectState.ModifySelection(_newObject, false);
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
