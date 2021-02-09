using System.Collections.Generic;
using System.Linq;
using View3D.Rendering;

namespace View3D.Components.Component.Selection
{
    public class FaceSelectionState : ISelectionState
    {
        public GeometrySelectionMode Mode => GeometrySelectionMode.Face;
        public event SelectionStateChanged SelectionChanged;

        public ISelectable RenderObject { get; set; }
        public List<int> SelectedFaces { get; set; } = new List<int>();


        public void ModifySelection(int newSelectionItem)
        {
            if (SelectedFaces.Contains(newSelectionItem))
                SelectedFaces.Remove(newSelectionItem);
            else
                SelectedFaces.Add(newSelectionItem);

            SelectionChanged?.Invoke(this);
        }

        public List<int> CurrentSelection()
        {
            return SelectedFaces;
        }

        public void Clear()
        {
            SelectedFaces.Clear();
            SelectionChanged?.Invoke(this);
        }


        public void EnsureSorted()
        {
            SelectedFaces = SelectedFaces.Distinct().OrderBy(x => x).ToList();
        }


        public ISelectionState Clone()
        {
            return new FaceSelectionState()
            {
                RenderObject = RenderObject,
                SelectedFaces = new List<int>(SelectedFaces)
            };
        }

        public void Restore()
        {
            
        }

        public int SelectionCount()
        {
            return SelectedFaces.Count();
        }

        public ISelectable GetSingleSelectedObject()
        {
            return RenderObject;
        }

        public List<ISelectable> SelectedObjects()
        {
            return new List<ISelectable>() { RenderObject };
        }
    }
}

