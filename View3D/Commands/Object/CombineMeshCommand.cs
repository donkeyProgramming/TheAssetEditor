using CommonControls.Common;
using MonoGame.Framework.WpfInterop;
using System.Collections.Generic;
using System.Linq;
using View3D.Components.Component.Selection;
using View3D.Rendering.Geometry;
using View3D.SceneNodes;
using View3D.Utility;

namespace View3D.Commands.Object
{
    public class CombineMeshCommand : CommandBase<CombineMeshCommand>
    {
        List<ISelectable> _objectsToCombine;
        List<IEditableGeometry> _combinedMeshes = new List<IEditableGeometry>();

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
                var combinedMeshes = new List<IEditableGeometry>();

                var geometriesToCombine = _objectsToCombine
                    .Where(x => x is Rmv2MeshNode)
                    .Cast<Rmv2MeshNode>()
                    .ToList();
                var combineGroups = ModelCombiner.SortMeshesIntoCombinableGroups(geometriesToCombine);
                foreach (var currentGroup in combineGroups)
                {
                    if (currentGroup.Count != 1)
                    {
                        var combinedMesh = SceneNodeHelper.CloneNode(currentGroup.First()) as IEditableGeometry;
                        combinedMesh.Name = currentGroup.First().Name + "_Combined";

                        var newModel = currentGroup.First().Geometry.Clone();
                        var typedGeo = currentGroup.Select(x => x.Geometry);
                        combinedMesh.Geometry = newModel;

                        var geoList = currentGroup.Skip(1).Select(x => x.Geometry).ToList();
                        newModel.Merge(geoList);

                        _combinedMeshes.Add(combinedMesh);
                    }
                    else
                    {
                        _combinedMeshes.Add(currentGroup.First());
                    }
                }

                // Remove all
                foreach (var item in _objectsToCombine)
                    item.Parent.RemoveObject(item);

                // Add all
                foreach(var item in _combinedMeshes)
                    item.Parent.AddObject(item);

                // Select all new 
                var currentState = _selectionManager.GetState() as ObjectSelectionState;
                currentState.Clear();
                currentState.ModifySelection(_combinedMeshes.Cast<ISelectable>(), false);
            }
        }

        protected override void UndoCommand()
        {
            foreach (var item in _objectsToCombine)
                item.Parent.AddObject(item);

            foreach(var item in _combinedMeshes)
                item.Parent.RemoveObject(item);

            _selectionManager.SetState(_originalSelectionState);
        }
    }
}
