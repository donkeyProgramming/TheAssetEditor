using System.Collections.Generic;
using System.Linq;
using GameWorld.Core.SceneNodes;

namespace GameWorld.Core.Components.Selection
{
    public class EdgeSelectionState : ISelectionState
    {
        public event SelectionStateChanged SelectionChanged;

        public GeometrySelectionMode Mode => GeometrySelectionMode.Edge;
        public ISelectable RenderObject { get; set; }

        private readonly HashSet<(int v0, int v1)> _selectedEdges = new();

        public IReadOnlyCollection<(int v0, int v1)> SelectedEdges => _selectedEdges;

        public void ModifySelection(IEnumerable<(int v0, int v1)> edges, bool onlyRemove)
        {
            if (onlyRemove)
            {
                foreach (var edge in edges)
                    _selectedEdges.Remove(edge);
            }
            else
            {
                foreach (var edge in edges)
                    _selectedEdges.Add(edge);
            }

            SelectionChanged?.Invoke(this, true);
        }

        public List<int> GetSelectedVertexIndices()
        {
            var set = new HashSet<int>();
            foreach (var (v0, v1) in _selectedEdges)
            {
                set.Add(v0);
                set.Add(v1);
            }
            return set.ToList();
        }

        public ISelectionState Clone()
        {
            var clone = new EdgeSelectionState { RenderObject = RenderObject };
            foreach (var edge in _selectedEdges)
                clone._selectedEdges.Add(edge);
            return clone;
        }

        public void Clear()
        {
            _selectedEdges.Clear();
            SelectionChanged?.Invoke(this, true);
        }

        public int SelectionCount() => _selectedEdges.Count;

        public ISelectable GetSingleSelectedObject() => RenderObject;

        public List<ISelectable> SelectedObjects()
        {
            if (RenderObject != null)
                return new List<ISelectable> { RenderObject };
            return new List<ISelectable>();
        }
    }
}
