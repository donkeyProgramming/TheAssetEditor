using CommonControls.Common;
using MonoGame.Framework.WpfInterop;
using System.Collections.Generic;
using System.Linq;
using View3D.Components.Component.Selection;
using View3D.Rendering.Geometry;
using View3D.SceneNodes;

namespace View3D.Commands.Object
{
    public class CombineMeshCommand : CommandBase<CombineMeshCommand>
    {
        List<ISelectable> _objectsToCombine;

        IEditableGeometry _combinedMesh;

        SelectionManager _selectionManager;
        ISelectionState _originalSelectionState;


        public CombineMeshCommand(List<ISelectable> objectsToCombine)
        {
            _objectsToCombine = new List<ISelectable>(objectsToCombine);
        }

        public override string GetHintText()
        {
            return "Combine Objects";
        }

        public override void Initialize(IComponentManager componentManager)
        {
            _selectionManager = componentManager.GetComponent<SelectionManager>();
        }

        protected override void ExecuteCommand()
        {
            _originalSelectionState = _selectionManager.GetStateCopy();

            using (new WaitCursor())
            {
                _combinedMesh = SceneNodeHelper.CloneNode(_objectsToCombine.First()) as IEditableGeometry;
                _combinedMesh.Name = _objectsToCombine.First().Name + "_Combined";
                _combinedMesh.Parent.AddObject(_combinedMesh);

                // Combine
                var editableGoes = _objectsToCombine.Where(x => x is IEditableGeometry).Select(x =>(IEditableGeometry)x);
                var newModel = editableGoes.First().Geometry.Clone() as MeshObject;
                var typedGeo = _objectsToCombine.Select(x => (MeshObject)x.Geometry);
                newModel.Merge(typedGeo.Skip(1).Take(typedGeo.Count() -1).ToList());
                _combinedMesh.Geometry = newModel;

                // Remove
                foreach (var item in _objectsToCombine)
                    item.Parent.RemoveObject(item);

                // Select
                var currentState = _selectionManager.GetState() as ObjectSelectionState;
                currentState.Clear();
                currentState.ModifySelectionSingleObject(_combinedMesh as ISelectable, false);
            }
        }

        protected override void UndoCommand()
        {
            foreach (var item in _objectsToCombine)
                _combinedMesh.Parent.AddObject(item);

            _combinedMesh.Parent.RemoveObject(_combinedMesh);
            _selectionManager.SetState(_originalSelectionState);
        }
    }
}
