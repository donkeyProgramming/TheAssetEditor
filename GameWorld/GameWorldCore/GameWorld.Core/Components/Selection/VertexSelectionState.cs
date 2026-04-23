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
            var geo = RenderObject.Geometry;
            var vertexArray = geo.VertexArray;
            var vertCount = vertexArray.Length;

            var selectedSet = new HashSet<int>(SelectedVertices);

            for (var i = 0; i < vertCount; i++)
                VertexWeights[i] = 0;

            if (SelectedVertices.Count == 0 || SelectedVertices.Count == vertCount || distanceOffset == 0)
            {
                foreach (var vert in SelectedVertices)
                    VertexWeights[vert] = 1.0f;
                return;
            }

            var selectedPositions = new Vector3[SelectedVertices.Count];
            for (int i = 0; i < SelectedVertices.Count; i++)
            {
                var pos = vertexArray[SelectedVertices[i]].Position;
                selectedPositions[i] = new Vector3(pos.X, pos.Y, pos.Z);
            }

            for (var i = 0; i < vertCount; i++)
            {
                if (selectedSet.Contains(i))
                {
                    VertexWeights[i] = 1.0f;
                }
                else
                {
                    var pos = vertexArray[i].Position;
                    var currentPos = new Vector3(pos.X, pos.Y, pos.Z);
                    var dist = GetClosestVertexDist(currentPos, selectedPositions);
                    if (dist <= distanceOffset)
                        VertexWeights[i] = 1 - dist / distanceOffset;
                }
            }
        }

        float GetClosestVertexDist(Vector3 currentPos, Vector3[] selectedPositions)
        {
            var closest = float.MaxValue;
            for (int i = 0; i < selectedPositions.Length; i++)
            {
                var dx = currentPos.X - selectedPositions[i].X;
                var dy = currentPos.Y - selectedPositions[i].Y;
                var dz = currentPos.Z - selectedPositions[i].Z;
                var distSq = dx * dx + dy * dy + dz * dz;
                if (distSq < closest)
                    closest = distSq;
            }
            return MathF.Sqrt(closest);
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
