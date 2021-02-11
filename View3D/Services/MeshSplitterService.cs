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

            var subModels = SlitIntoSubModels(geometry.GetIndexBuffer(), geometry);
            foreach (var subModel in subModels)
            {
                var duplicate = geometry.Clone();
                duplicate.RemoveUnusedVertexes(subModel.ToArray());
                output.Add(duplicate);
            }

            return output;

        }

        public List<List<ushort>> SlitIntoSubModels(List<ushort> indexList, IGeometry geo)
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
                    if (currentObject.Contains(indexList[i+0]) || currentObject.Contains(indexList[i+1]) || currentObject.Contains(indexList[i+2]))
                    {
                        isContainedInExistingObject = true;
                        currentObject.Add(indexList[i+0]);
                        currentObject.Add(indexList[i+1]);
                        currentObject.Add(indexList[i + 2]);
                        break;
                    }

                    float tolerance = 0.0001f;
                    if (ContainsVertex(currentObject, indexList[i + 0], geo, tolerance) 
                        || ContainsVertex(currentObject, indexList[i + 1], geo, tolerance) 
                        || ContainsVertex(currentObject, indexList[i + 2], geo, tolerance))
                    {
                        isContainedInExistingObject = true;
                        currentObject.Add(indexList[i + 0]);
                        currentObject.Add(indexList[i + 1]);
                        currentObject.Add(indexList[i + 2]);
                    }

                }

                if(isContainedInExistingObject == false)
                    newObjects.Add(new List<ushort>() { indexList[i+0], indexList[i+1], indexList[i+2] });
            }

            var totalCount0 = newObjects.SelectMany(x => x).ToList().Count;

            // Check if any of the submeshes are connected
            while (CombineSubmeshes(ref newObjects)) { }

            var totalCount1 = newObjects.SelectMany(x => x).ToList().Count;
            return newObjects;
        
        }


        bool CombineSubmeshes(ref List<List<ushort>> meshs)
        {
            foreach (var outerMesh in meshs)
            {
                var destinct = outerMesh.Distinct();
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

        bool ContainsVertex(List<ushort> mesh, ushort possibleVertexIndex, IGeometry geo, float tolerance = 0.0000001f )
        {
            var possibleVertex = geo.GetVertexByIndex(possibleVertexIndex);
            var uniqeIndexes = mesh.Distinct();
            foreach (var index in uniqeIndexes)
            {
                var vert = geo.GetVertexByIndex(index);
                if ( (Math.Abs(vert.X - possibleVertex.X) < tolerance)  && 
                     (Math.Abs(vert.Y - possibleVertex.Y) < tolerance) &&
                    (Math.Abs(vert.Z - possibleVertex.Z) < tolerance) )
                    return true;


            }
            return false;
        }
    }
}
