using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using View3D.Rendering;

namespace View3D.Components.Component.Selection
{
    public class VertexSelectionState : ISelectionState
    {
        public GeometrySelectionMode Mode => GeometrySelectionMode.Vertex;
        public event SelectionStateChanged SelectionChanged;

        public RenderItem RenderObject { get; set; }
        public List<int> SelectedVertices { get; set; } = new List<int>();


        public void ModifySelection(int newSelectionItem)
        {
            if (SelectedVertices.Contains(newSelectionItem))
                SelectedVertices.Remove(newSelectionItem);
            else
                SelectedVertices.Add(newSelectionItem);

            SelectionChanged?.Invoke(this);
        }

        public List<int> CurrentSelection()
        {
            return SelectedVertices;
        }

        public void Clear()
        {
            SelectedVertices.Clear();
            SelectionChanged?.Invoke(this);
        }

        public void EnsureSorted()
        {
            SelectedVertices = SelectedVertices.Distinct().OrderBy(x => x).ToList();
        }

        public ISelectionState Clone()
        {
            return new VertexSelectionState()
            {
                RenderObject = RenderObject,
                SelectedVertices = new List<int>(SelectedVertices)
            };
        }

        public void Restore()
        {

        }

        public int SelectionCount()
        {
            return SelectedVertices.Count();
        }

        public RenderItem GetSingleSelectedObject()
        {
            return RenderObject;
        }
    }
}
