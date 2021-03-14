using System;
using System.Collections.Generic;
using System.Text;
using View3D.Components.Component.Selection;

namespace View3D.Commands.Face
{
   //public class DuplicateFacesCommand : CommandBase<DuplicateFacesCommand>
   //{
   //    SelectionManager _selectionManager;
   //
   //    ISelectionState _oldState;
   //    IGeometry _oldGeometry;
   //
   //    List<int> _facesToDelete;
   //    IGeometry _geo;
   //
   //    public DeleteFaceCommand(IGeometry geoObject, List<int> facesToDelete)
   //    {
   //        _facesToDelete = facesToDelete;
   //        _geo = geoObject;
   //    }
   //
   //    public override void Initialize(IComponentManager componentManager)
   //    {
   //        _selectionManager = componentManager.GetComponent<SelectionManager>();
   //    }
   //
   //    protected override void ExecuteCommand()
   //    {
   //        _oldState = _selectionManager.GetStateCopy();
   //
   //
   //
   //        var selectedFaceIndecies = new List<ushort>();
   //        var indexBuffer = faceSelectionState.RenderObject.Geometry.GetIndexBuffer();
   //        foreach (var face in faceSelectionState.SelectedFaces)
   //        {
   //            selectedFaceIndecies.Add(indexBuffer[face]);
   //            selectedFaceIndecies.Add(indexBuffer[face + 1]);
   //            selectedFaceIndecies.Add(indexBuffer[face + 2]);
   //        }
   //
   //        var duplicate = faceSelectionState.RenderObject.Geometry.Clone();
   //        duplicate.RemoveUnusedVertexes(selectedFaceIndecies.ToArray());
   //        faceSelectionState.RenderObject.Geometry = duplicate;
   //
   //
   //
   //
   //        var faceSelectionState = _selectionManager.GetState() as FaceSelectionState;
   //        faceSelectionState.Clear();
   //    }
   //
   //    protected override void UndoCommand()
   //    {
   //        _selectionManager.SetState(_oldState);
   //        var faceSelectionState = _selectionManager.GetState() as FaceSelectionState;
   //        faceSelectionState.RenderObject.Geometry = _oldGeometry;
   //    }
   //}
}
