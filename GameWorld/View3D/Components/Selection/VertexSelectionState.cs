using GameWorld.Core.SceneNodes;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Linq;

namespace GameWorld.Core.Components.Selection
{
    public class VertexSelectionState : ISelectionState
    {
        public GeometrySelectionMode Mode => GeometrySelectionMode.Vertex;
        public event SelectionStateChanged SelectionChanged;

        public ISelectable RenderObject { get; set; }
        public List<int> SelectedVertices { get; set; } = new List<int>();
        public List<float> VertexWeights { get; set; } = new List<float>();

        float _selectionDistanceFallof;

        public VertexSelectionState(ISelectable renderObj, float vertexSelectionFallof)
        {
            RenderObject = renderObj;
            VertexWeights = Enumerable.Repeat(0.0f, RenderObject.Geometry.VertexCount()).ToList();
            _selectionDistanceFallof = vertexSelectionFallof;
        }

        public void ModifySelection(IEnumerable<int> newSelectionItems, bool onlyRemove)
        {
            if (onlyRemove)
            {
                foreach (var newSelectionItem in newSelectionItems)
                {
                    if (SelectedVertices.Contains(newSelectionItem))
                        SelectedVertices.Remove(newSelectionItem);
                }
            }
            else
            {
                foreach (var newSelectionItem in newSelectionItems)
                {
                    if (!SelectedVertices.Contains(newSelectionItem))
                        SelectedVertices.Add(newSelectionItem);
                }
            }

            UpdateWeights(_selectionDistanceFallof);
            SelectionChanged?.Invoke(this, true);
        }

        public void UpdateWeights(float distanceOffset)
        {
            _selectionDistanceFallof = distanceOffset;
            var vertexList = RenderObject.Geometry.GetVertexList();
            var vertListLength = vertexList.Count;

            // Clear all
            for (var currentVertIndex = 0; currentVertIndex < vertexList.Count; currentVertIndex++)
                VertexWeights[currentVertIndex] = 0;

            // Compute new
            if (SelectedVertices.Count == 0 || SelectedVertices.Count == vertexList.Count || distanceOffset == 0)
            {
                foreach (var vert in SelectedVertices)
                    VertexWeights[vert] = 1.0f;
            }
            else
            {
                var vertsInUse = SelectedVertices.Select(x => vertexList[x]);
                for (var currentVertIndex = 0; currentVertIndex < vertexList.Count; currentVertIndex++)
                {
                    var currentVertPos = vertexList[currentVertIndex];
                    if (SelectedVertices.Contains(currentVertIndex))
                    {
                        VertexWeights[currentVertIndex] = 1.0f;
                    }
                    else
                    {
                        var dist = GetClosestVertexDist(currentVertPos, vertsInUse);
                        if (dist <= distanceOffset)
                            VertexWeights[currentVertIndex] = 1 - dist / distanceOffset;
                    }
                }
            }
        }


        float GetClosestVertexDist(Vector3 currentPos, IEnumerable<Vector3> vertList)
        {
            var closest = float.MaxValue;
            foreach (var vert in vertList)
            {
                var dist = Vector3.Distance(vert, currentPos);
                if (dist < closest)
                    closest = dist;
            }
            return closest;
        }

        public List<int> CurrentSelection()
        {
            return SelectedVertices;
        }

        public void Clear()
        {
            SelectedVertices.Clear();
            SelectionChanged?.Invoke(this, true);
        }

        public void EnsureSorted()
        {
            SelectedVertices = SelectedVertices.Distinct().OrderBy(x => x).ToList();
        }

        public ISelectionState Clone()
        {
            return new VertexSelectionState(RenderObject, _selectionDistanceFallof)
            {
                SelectedVertices = new List<int>(SelectedVertices),
                VertexWeights = new List<float>(VertexWeights),
            };
        }

        public int SelectionCount()
        {
            return SelectedVertices.Count();
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
