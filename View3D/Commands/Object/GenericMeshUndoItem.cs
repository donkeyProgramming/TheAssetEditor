using MonoGame.Framework.WpfInterop;
using System;
using System.Collections.Generic;
using System.Text;
using View3D.Components.Component.Selection;
using View3D.SceneNodes;

namespace View3D.Commands.Object
{

    public class GenericMeshUndoItem : CommandBase<GenericMeshUndoItem>
    {
        List<Rmv2MeshNode> _objectsToCombine;
        List<Rmv2MeshNode> _clonedObjects = new List<Rmv2MeshNode>();

        string _description;

        SelectionManager _selectionManager;
        ISelectionState _originalSelectionState;


        public GenericMeshUndoItem(List<Rmv2MeshNode> objectsToCombine, string description)
        {
            _objectsToCombine = new List<Rmv2MeshNode>(objectsToCombine);
            _description = description;
        }

        public override string GetHintText()
        {
            return _description;
        }

        public override void Initialize(IComponentManager componentManager)
        {
            _selectionManager = componentManager.GetComponent<SelectionManager>();
        }

        protected override void ExecuteCommand()
        {
           /* _originalSelectionState = _selectionManager.GetStateCopy();

            foreach (var item in _objectsToCombine)
            {
                var clone = item.Clone();
            }



            using (new WaitCursor())
            {
                _combinedMesh = _objectsToCombine.First().Clone() as IEditableGeometry;
                _combinedMesh.Name = _objectsToCombine.First().Name + "_Combined";
                _combinedMesh.Parent.AddObject(_combinedMesh);

                // Combine
                var editableGoes = _objectsToCombine.Where(x => x is IEditableGeometry).Select(x => (IEditableGeometry)x);
                var newModel = editableGoes.First().Geometry.Clone() as Rmv2Geometry;
                var typedGeo = _objectsToCombine.Select(x => (Rmv2Geometry)x.Geometry);
                newModel.Merge(typedGeo.Skip(1).Take(typedGeo.Count() - 1).ToList());
                _combinedMesh.Geometry = newModel;

                // Remove
                foreach (var item in _objectsToCombine)
                    item.Parent.RemoveObject(item);

                // Select
                var currentState = _selectionManager.GetState() as ObjectSelectionState;
                currentState.Clear();
                currentState.ModifySelection(_combinedMesh as ISelectable, false);
            }*/
        }

        protected override void UndoCommand()
        {
           /* foreach (var item in _objectsToCombine)
                _combinedMesh.Parent.AddObject(item);

            _combinedMesh.Parent.RemoveObject(_combinedMesh);
            _selectionManager.SetState(_originalSelectionState);*/
        }
    }
}
 