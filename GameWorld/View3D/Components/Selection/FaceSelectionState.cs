using System.Collections.Generic;
using System.Linq;
using GameWorld.Core.SceneNodes;

namespace GameWorld.Core.Components.Selection
{
    public class FaceSelectionState : ISelectionState
    {
        public GeometrySelectionMode Mode => GeometrySelectionMode.Face;
        public event SelectionStateChanged SelectionChanged;

        public ISelectable RenderObject { get; set; }
        public List<int> SelectedFaces { get; set; } = new List<int>();

        public void ModifySelection(IEnumerable<int> newSelectionItems, bool onlyRemove)
        {
            if (onlyRemove)
            {
                foreach (var newSelectionItem in newSelectionItems)
                {
                    if (SelectedFaces.Contains(newSelectionItem))
                        SelectedFaces.Remove(newSelectionItem);
                }
            }
            else
            {
                foreach (var newSelectionItem in newSelectionItems)
                {
                    if (!SelectedFaces.Contains(newSelectionItem))
                        SelectedFaces.Add(newSelectionItem);
                }
            }
            SelectionChanged?.Invoke(this, true);
        }


        public List<int> CurrentSelection()
        {
            return SelectedFaces;
        }

        public void Clear()
        {
            SelectedFaces.Clear();
            SelectionChanged?.Invoke(this, true);
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

