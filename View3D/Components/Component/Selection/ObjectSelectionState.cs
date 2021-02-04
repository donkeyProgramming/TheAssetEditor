using System.Collections.Generic;
using View3D.Rendering;

namespace View3D.Components.Component.Selection
{
    public class ObjectSelectionState : ISelectionState
    {
        public event SelectionStateChanged SelectionChanged;
        public GeometrySelectionMode Mode => GeometrySelectionMode.Object;

        List<RenderItem> _selectionList { get; set; } = new List<RenderItem>();

        public void ModifySelection(RenderItem newSelectionItem)
        {
            if (_selectionList.Contains(newSelectionItem))
                _selectionList.Remove(newSelectionItem);
            else
                _selectionList.Add(newSelectionItem);

            SelectionChanged?.Invoke(this);
        }

        public List<RenderItem> CurrentSelection() 
        { 
            return _selectionList; 
        }

        public void Clear()
        {
            _selectionList.Clear();
            SelectionChanged?.Invoke(this);
        }

        public ISelectionState Clone()
        {
            return new ObjectSelectionState()
            {
                _selectionList = new List<RenderItem>(_selectionList)
            };
        }

        public void Restore()
        {
            SelectionChanged?.Invoke(this);
        }

        public int SelectionCount()
        {
            return _selectionList.Count;
        }
    }
}

