using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using View3D.Rendering.Geometry;

namespace View3D.Services
{
    public class MeshSplitterService
    {
        public List<IGeometry> SplitMesh(IGeometry geometry)
        {
            List<IGeometry> output = new List<IGeometry>();
            var vertList = geometry.GetVertexList();
            var subModels = SplitIntoSubModels(geometry.GetIndexBuffer(), vertList);
            foreach (var subModel in subModels)
            {
                var duplicate = geometry.Clone();
                duplicate.RemoveUnusedVertexes(subModel.ToArray());
                output.Add(duplicate);
            }

            return output;
        }

        public List<List<ushort>> SplitIntoSubModels(List<ushort> indexList, List<Vector3> vertextes)
        {
            if (indexList.Count == 0)
                return null;

            List<List<ushort>> newObjects = new List<List<ushort>>();
            newObjects.Add(new List<ushort>(indexList.Count) { indexList[0], indexList[1], indexList[2] });

            for (int i = 3; i < indexList.Count; i += 3)
            {
                bool isContainedInExistingObject = false;
                foreach (var currentObject in newObjects)
                {
                    if (IsFaceInside(indexList, i, currentObject, vertextes))
                    {
                        isContainedInExistingObject = true;
                        currentObject.Add(indexList[i + 0]);
                        currentObject.Add(indexList[i + 1]);
                        currentObject.Add(indexList[i + 2]);
                        break;
                    }
                }

                if (isContainedInExistingObject == false)
                    newObjects.Add(new List<ushort>(indexList.Count) { indexList[i + 0], indexList[i + 1], indexList[i + 2] });
            }


            // Check if any of the submeshes are connected
            var objectList = newObjects.Select(x => new TempMesh(x, vertextes)).ToList();
            while (CombineSubmeshes(ref objectList, vertextes)) { }
            objectList = objectList.OrderByDescending(x => x.Size()).ToList();
            CombineMeshesBasedOnVertex(ref objectList, vertextes);

            return objectList.Select(x => x.IndexList).ToList();
        }

        public List<int> GrowSelection(IGeometry geometry, List<ushort> initialSelectedIndexes)
        {
            var vertextes = geometry.GetVertexList();
            var indexList = geometry.GetIndexBuffer();

            List<int> newSelection = new List<int>();
            List<ushort> activeIndexList = new List<ushort>(initialSelectedIndexes);
           
            bool foundSomething = true;
            while (foundSomething)
            {
                foundSomething = false;
                for (int i = 0; i < indexList.Count; i += 3)
                {
                    var index0 = indexList[i+0];
                    var index1 = indexList[i+1];
                    var index2 = indexList[i+2];

                    if (newSelection.Contains(i) == false)
                    {
                        if (IsFaceInside(indexList, i, activeIndexList, vertextes))
                        {
                            newSelection.Add(i);
                            foundSomething = true;
                            activeIndexList.Add(index0);
                            activeIndexList.Add(index1);
                            activeIndexList.Add(index2);
                        }
                    }
                }
            }

            return newSelection;
        }

        bool IsFaceInside(List<ushort> indexBuffer, int faceId, List<ushort> currentSelection, List<Vector3> vertextes)
        {
            var index0 = indexBuffer[faceId + 0];
            var index1 = indexBuffer[faceId + 1];
            var index2 = indexBuffer[faceId + 2];

            if (currentSelection.Contains(index0) || (currentSelection.Contains(index1)) || (currentSelection.Contains(index2)))
                return true;

            float tolerance = 0.0001f * 0.0001f;
            if (ContainsVertex(currentSelection, index0, index1, index2, vertextes, tolerance))
                return true;

            return false;
        }

        bool CombineSubmeshes(ref List<TempMesh> meshs, List<Vector3> vertextes)
        {
            foreach (var outerMesh in meshs)
            {
                var outerMeshDistinctDistinct = outerMesh.IndexList.Distinct();
                var outerMeshDistinctLookup = outerMeshDistinctDistinct.ToLookup(x=>x);
                foreach (var innerMesh in meshs)
                {
                    if (innerMesh == outerMesh)
                        continue;

                    for (int i = 0; i < innerMesh.IndexList.Count(); i++)
                    {
                        if(outerMeshDistinctLookup.Contains(innerMesh.IndexList[i]))
                        {
                            outerMesh.IndexList.AddRange(innerMesh.IndexList);
                            meshs.Remove(innerMesh);
                            outerMesh.CreateBoundingBox(vertextes);
                            return true;
                        }
                    }
                }
            }
            return false;
        
        }

        void CombineMeshesBasedOnVertex(ref List<TempMesh> meshs, List<Vector3> vertextes)
        {
            for(int outerMeshIndex = 0; outerMeshIndex < meshs.Count; outerMeshIndex++)
            {
                var outerMesh = meshs[outerMeshIndex];

                var outerMeshDistinctDistinct = outerMesh.IndexList.Distinct();
                for(int innerMeshIndex = 0; innerMeshIndex < meshs.Count; innerMeshIndex++)
                {
                    var innerMesh = meshs[innerMeshIndex];
                    if (outerMeshIndex == innerMeshIndex)
                        continue;
                    if (innerMesh.Box.Intersects(outerMesh.Box))
                    {
                        var innerMeshDistinct = innerMesh.IndexList.Distinct();
                        foreach (var innerIndex in innerMeshDistinct)
                        {
                            if (ContainsVertex(outerMeshDistinctDistinct, innerIndex, vertextes, 0.0001f * 0.0001f))
                            {
                                outerMesh.IndexList.AddRange(innerMesh.IndexList);
                                meshs.Remove(innerMesh);
                                outerMesh.CreateBoundingBox(vertextes);
                                innerMeshIndex--;
                                break;
                            }
                        }
                    }
                }
            }
        }

        bool ContainsVertex(List<ushort> mesh, ushort possibleVertexIndex0, ushort possibleVertexIndex1, ushort possibleVertexIndex2, List<Vector3> vertextes, float tolerance = 0.0000001f )
        {
            foreach (var index in mesh)
            {
                var vert = vertextes[index];

                var length0 = (vert - vertextes[possibleVertexIndex0]).LengthSquared();
                if (length0 < tolerance)
                    return true;

                var length1 = (vert - vertextes[possibleVertexIndex1]).LengthSquared();
                if (length1 < tolerance)
                    return true;

                var length2 = (vert - vertextes[possibleVertexIndex2]).LengthSquared();
                if (length2 < tolerance)
                    return true;
            }
            return false;
        }

        bool ContainsVertex(IEnumerable<ushort> mesh, ushort possibleVertexIndex0, List<Vector3> vertextes, float tolerance = 0.0000001f)
        {
            foreach (var index in mesh)
            {
                var vert = vertextes[index];
                var length0 = (vert - vertextes[possibleVertexIndex0]).LengthSquared();
                if (length0 < tolerance)
                    return true;
            }
            return false;
        }

            class TempMesh
        {
            public BoundingBox Box;
            public List<ushort> IndexList;

            public TempMesh(List<ushort> mesh, List<Vector3> vertextes)
            {
                IndexList = mesh;
                CreateBoundingBox(vertextes);
            }

            public void CreateBoundingBox(List<Vector3> vertextes)
            {
                var objectVertList = new Vector3[IndexList.Count];
                for (int i = 0; i < IndexList.Count; i++)
                    objectVertList[i] = vertextes[IndexList[i]];
                Box = BoundingBox.CreateFromPoints(objectVertList);
            }

            public float Size()
            {
                var size = Box.Max - Box.Min;
                return size.X * size.X + size.Y * size.Y + size.Z * size.Z;
            }
        }

    }
}
