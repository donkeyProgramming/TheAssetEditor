using MonoGame.Framework.WpfInterop;
using System;
using System.Collections.Generic;
using View3D.Rendering;
using View3D.Scene;

namespace View3D.Scene
{
    public delegate void SelectionChangedDelegate(IEnumerable<RenderItem> items);
    public class SelectionManager : BaseComponent
    {
        public event SelectionChangedDelegate SelectionChanged;
        List<RenderItem> _selectionList = new List<RenderItem>();

        public SelectionManager(WpfGame game ) : base(game)
        {

        }

        public List<RenderItem> CurrentSelection()
        {
            return new List<RenderItem>(_selectionList);
        }

        public void SetCurrentSelection(List<RenderItem> renderItems)
        {
            _selectionList = new List<RenderItem>(renderItems);
            SelectionChanged?.Invoke(_selectionList);
        }

        internal void ClearSelection()
        {
            _selectionList.Clear();
            SelectionChanged?.Invoke(_selectionList);
        }

        internal void AddToSelection(RenderItem newSelectionItem)
        {
            _selectionList.Add(newSelectionItem);
            SelectionChanged?.Invoke(_selectionList);
        }

        internal void ModifySelection(RenderItem newSelectionItem)
        {
            if (_selectionList.Contains(newSelectionItem))
                _selectionList.Remove(newSelectionItem);
            else
                _selectionList.Add(newSelectionItem);

            SelectionChanged?.Invoke(_selectionList);
        }
    }
}

