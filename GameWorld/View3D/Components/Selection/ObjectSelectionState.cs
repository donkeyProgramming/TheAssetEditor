using System.Collections.Generic;
using System.Linq;
using GameWorld.Core.SceneNodes;

namespace GameWorld.Core.Components.Selection
{
    public class ObjectSelectionState : ISelectionState
    {
        public event SelectionStateChanged SelectionChanged;
        public GeometrySelectionMode Mode => GeometrySelectionMode.Object;

        List<ISelectable> _selectionList { get; set; } = new List<ISelectable>();

        public void ModifySelectionSingleObject(ISelectable newSelectionItem, bool onlyRemove)
        {
            if (_selectionList.Contains(newSelectionItem))
                _selectionList.Remove(newSelectionItem);
            else if (!onlyRemove)
                _selectionList.Add(newSelectionItem);

            SelectionChanged?.Invoke(this, true);
        }

        public void ModifySelection(IEnumerable<ISelectable> newSelectionItems, bool onlyRemove)
        {
            if (onlyRemove)
            {
                foreach (var newSelectionItem in newSelectionItems)
                {
                    if (_selectionList.Contains(newSelectionItem))
                        _selectionList.Remove(newSelectionItem);
                }
            }
            else
            {
                foreach (var newSelectionItem in newSelectionItems)
                {
                    if (!_selectionList.Contains(newSelectionItem))
                        _selectionList.Add(newSelectionItem);
                }
            }

            SelectionChanged?.Invoke(this, true);
        }


        public List<ISelectable> CurrentSelection()
        {
            return _selectionList;
        }

        public void Clear()
        {
            if (_selectionList.Count != 0)
            {
                _selectionList.Clear();
                SelectionChanged?.Invoke(this, true);
            }
        }

        public ISelectionState Clone()
        {
            return new ObjectSelectionState()
            {
                _selectionList = new List<ISelectable>(_selectionList)
            };
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

        public List<ISelectable> SelectedObjects()
        {
            return _selectionList;
        }

        public List<T> SelectedObjects<T>() where T : class
        {
            return _selectionList
               .Where(x => x is T)
               .Select(x => x as T)
               .ToList();
        }

        public bool IsSelectionEqual(ObjectSelectionState state)
        {
            if (_selectionList.Count != state._selectionList.Count)
                return false;

            foreach (var item in _selectionList)
            {
                if (state._selectionList.Contains(item) == false)
                    return false;
            }

            return true;
        }
    }
}

