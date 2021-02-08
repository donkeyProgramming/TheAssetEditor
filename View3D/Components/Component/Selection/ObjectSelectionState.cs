using System.Collections.Generic;
using View3D.Rendering;

namespace View3D.Components.Component.Selection
{
    public class ObjectSelectionState : ISelectionState
    {
        public event SelectionStateChanged SelectionChanged;
        public GeometrySelectionMode Mode => GeometrySelectionMode.Object;

        List<ISelectable> _selectionList { get; set; } = new List<ISelectable>();

        public void ModifySelection(ISelectable newSelectionItem)
        {
            if (_selectionList.Contains(newSelectionItem))
                _selectionList.Remove(newSelectionItem);
            else
                _selectionList.Add(newSelectionItem);

            SelectionChanged?.Invoke(this);
        }

        public List<ISelectable> CurrentSelection() 
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
                _selectionList = new List<ISelectable>(_selectionList)
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

        public ISelectable GetSingleSelectedObject()
        {
            if (_selectionList.Count != 1)
                return null;
            return _selectionList[0];
        }
    }
}

