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

        public List<int> GrowSelection(IGeometry geometry, List<ushort> initialSelectedIndexes)
        {

            var vertList = geometry.GetVertexList();

            List<int> newSelection = new List<int>();
            List<ushort> activeIndexList = new List<ushort>(initialSelectedIndexes);
            var indexBuffer = geometry.GetIndexBuffer();

            bool foundSomething = true;
            while (foundSomething)
            {
                foundSomething = false;
                for (int i = 0; i < indexBuffer.Count; i += 3)
                {
                    var index0 = indexBuffer[i+0];
                    var index1 = indexBuffer[i+1];
                    var index2 = indexBuffer[i+2];

                    if (newSelection.Contains(i) == false)
                    {
                        if (IsFaceInside(indexBuffer, i, activeIndexList, vertList))
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

            float tolerance = 0.0001f;
            if (ContainsVertex(currentSelection, index0, vertextes, tolerance)
                || ContainsVertex(currentSelection, index1, vertextes, tolerance)
                || ContainsVertex(currentSelection, index2, vertextes, tolerance))
            {
                return true;
            }

            return false;
        }

        public List<List<ushort>> SplitIntoSubModels(List<ushort> indexList, List<Vector3> vertextes)
        {
            if (indexList.Count == 0)
                return null;

            List<List<ushort>> newObjects = new List<List<ushort>>();
            newObjects.Add(new List<ushort>() { indexList[0], indexList[1] , indexList[2] });

            for (ushort i = 3; i < indexList.Count; i+=3)
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

                if(isContainedInExistingObject == false)
                    newObjects.Add(new List<ushort>() { indexList[i+0], indexList[i+1], indexList[i+2] });
            }

            // Check if any of the submeshes are connected
            while (CombineSubmeshes(ref newObjects)) { }

            var totalCount1 = newObjects.SelectMany(x => x).ToList().Count;
            return newObjects;
        
        }


        bool CombineSubmeshes(ref List<List<ushort>> meshs)
        {
            foreach (var outerMesh in meshs)
            {
                var destinct = outerMesh.Distinct().ToLookup(x=>x);
                foreach (var innerMesh in meshs)
                {
                    if (innerMesh == outerMesh)
                        continue;

                    for (int i = 0; i < innerMesh.Count(); i++)
                    {
                        if(destinct.Contains(innerMesh[i]))
                        {
                            outerMesh.AddRange(innerMesh);
                            meshs.Remove(innerMesh);
                            return true;
                        }
                    }
                }
            }
            return false;
        
        }

        bool ContainsVertex(List<ushort> mesh, ushort possibleVertexIndex, List<Vector3> vertextes, float tolerance = 0.0000001f )
        {
            var possibleVertex = vertextes[possibleVertexIndex];
            //var uniqeIndexes = mesh.Distinct().ToList();
            foreach (var index in mesh)
            {
                var vert = vertextes[index];
                if ( (Math.Abs(vert.X - possibleVertex.X) < tolerance)  && 
                     (Math.Abs(vert.Y - possibleVertex.Y) < tolerance) &&
                    (Math.Abs(vert.Z - possibleVertex.Z) < tolerance) )
                    return true;


            }
            return false;
        }
    }
}
