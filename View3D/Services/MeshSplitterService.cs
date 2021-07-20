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
        public List<IGeometry> SplitMesh(IGeometry geometry, bool combineOverlappingVertexes)
        {
            List<IGeometry> output = new List<IGeometry>();
            var vertList = geometry.GetVertexList();
            var subModels = SplitIntoSubModels(geometry.GetIndexBuffer(), vertList, combineOverlappingVertexes);
            foreach (var subModel in subModels)
            {
                var duplicate = geometry.CloneSubMesh(subModel.ToArray());
                output.Add(duplicate);
            }

            return output;
        }

        List<List<ushort>> SplitIntoSubModels(List<ushort> indexList, List<Vector3> vertextes, bool combineOverlappingVertexes)
        {
           var selectedSubMeshes = ConverteToSubFaceObject(indexList, vertextes, combineOverlappingVertexes);

            var output = new List<List<ushort>>();
           foreach (var item in selectedSubMeshes)
           {
                var faces = item.GetFaces();
                var meshIndexes = new List<ushort>(faces.Count);
                foreach (var face in faces)
                {
                    meshIndexes.Add(indexList[face + 0]);
                    meshIndexes.Add(indexList[face + 1]);
                    meshIndexes.Add(indexList[face + 2]);
                }

                output.Add(meshIndexes);

           }
           return output;
        }

        public List<int> GrowFaceSelection(IGeometry geometry, List<ushort> initialSelectedIndexes, bool combineOverlappingVertexes)
        {
            var vertextes = geometry.GetVertexList();
            var indexList = geometry.GetIndexBuffer();
            var selectedSubMeshes = ConverteToSubFaceObject(indexList, vertextes, combineOverlappingVertexes);

            var outputFaceList = new List<int>();
            foreach (var item in selectedSubMeshes)
            {
                if (item.ContainsAtLeastOneIndex(initialSelectedIndexes))
                    outputFaceList.AddRange(item.GetFaces());
            }

            return outputFaceList.Distinct().ToList();
        }


        List<SubFaceObject> ConverteToSubFaceObject(List<ushort> indexList, List<Vector3> vertextes, bool combineOverlappingVertexes)
        {
            var subMeshList = new List<SubFaceObject>();

            for (int i = 3; i < indexList.Count; i += 3)
            {
                bool isContainedInExistingObject = false;
                foreach (var currentObject in subMeshList)
                {
                    if (currentObject.IsConnectedToFace(i))
                    {
                        currentObject.AddFace(i);
                        isContainedInExistingObject = true;
                    };
                }

                if (isContainedInExistingObject == false)
                {
                    var newItem = new SubFaceObject(indexList, vertextes);
                    newItem.AddFace(i);
                    subMeshList.Add(newItem);
                }
            }

            foreach (var mesh in subMeshList)
                mesh.ComputeBoundingBox();

            if (combineOverlappingVertexes == false)
                return subMeshList;

            return MergeSubMeshes(subMeshList);
        }

        List<SubFaceObject> MergeSubMeshes(List<SubFaceObject> list)
        {
            for (int i = 0; i < list.Count; i++)
            {
                for (int j = 0; j < list.Count; j++)
                {
                    if (i == j)
                        continue;

                    var meshA = list[i];
                    var meshB = list[j];

                    if (meshA.Overlaps(meshB))
                        meshA.Merge(meshB);
                }
            }

            return list.Where(x => x.GetFaceCount() != 0).ToList();
        }



        class SubFaceObject
        {
            //List<int> FaceIndexes;
            //List<int> VertexIndexes; // FaceIndexes * 3
            //BoundingBox BB;
            //List<Vector3> Points;

            List<ushort> _meshTotalIndexList;
            List<Vector3> _meshVertexList;

            List<ushort> _subMeshDistinctIndexList;
            List<int> _faceList = new List<int>();
            BoundingBox _bb;

            public SubFaceObject(List<ushort> meshTotalIndexList, List<Vector3> meshVertexList)
            {
                _meshTotalIndexList = meshTotalIndexList;
                _meshVertexList = meshVertexList;
                _subMeshDistinctIndexList = new List<ushort>(meshTotalIndexList.Count() / 10);
            }

            public bool IsConnectedToFace(int faceIndex)
            {
                var index = faceIndex;

                if (_subMeshDistinctIndexList.Contains(_meshTotalIndexList[index + 0]))
                    return true;
                if (_subMeshDistinctIndexList.Contains(_meshTotalIndexList[index + 1]))
                    return true;
                if (_subMeshDistinctIndexList.Contains(_meshTotalIndexList[index + 2]))
                    return true;

                return false;
            }

            public void AddFace(int faceIndex)
            {
                _faceList.Add(faceIndex);

                var index = faceIndex;
                if (!_subMeshDistinctIndexList.Contains(_meshTotalIndexList[index + 0]))
                    _subMeshDistinctIndexList.Add(_meshTotalIndexList[index + 0]);
                if (!_subMeshDistinctIndexList.Contains(_meshTotalIndexList[index + 1]))
                    _subMeshDistinctIndexList.Add(_meshTotalIndexList[index + 1]);
                if (!_subMeshDistinctIndexList.Contains(_meshTotalIndexList[index + 2]))
                    _subMeshDistinctIndexList.Add(_meshTotalIndexList[index + 2]);
            }

            public bool ContainsAtLeastOneIndex(List<ushort> indexList)
            {
                foreach (var index in indexList)
                {
                    if (_subMeshDistinctIndexList.Contains(index))
                        return true;
                }
                return false;
            }

            public List<int> GetFaces()
            {
                return _faceList.Distinct().ToList();
            }

            public int GetFaceCount()
            {
                return _faceList.Count();
            }


            public void ComputeBoundingBox()
            {
                var vertList = new Vector3[_subMeshDistinctIndexList.Count()];
                for (int i = 0; i < _subMeshDistinctIndexList.Count; i++)
                    vertList[i] = _meshVertexList[_subMeshDistinctIndexList[i]];
                _bb = BoundingBox.CreateFromPoints(vertList);
            }

            public bool Overlaps(SubFaceObject other)
            {
                if (_bb.Intersects(other._bb))
                {
                    var selfIndexes = _subMeshDistinctIndexList;
                    var otherIndexes = other._subMeshDistinctIndexList;

                    foreach (var otherVertIndex in otherIndexes)
                    {

                        float tolerance = 0.0001f * 0.0001f;
                        if (ContainsVertex(selfIndexes, otherVertIndex, _meshVertexList, tolerance))
                            return true;
                    }
                }

                return false;
            }

            public void Merge(SubFaceObject other)
            {
                _faceList.AddRange(other._faceList);
                _faceList = _faceList.Distinct().ToList();

                _subMeshDistinctIndexList.AddRange(other._subMeshDistinctIndexList);
                _subMeshDistinctIndexList = _subMeshDistinctIndexList.Distinct().ToList();

                ComputeBoundingBox();

                other._faceList.Clear();
                other._subMeshDistinctIndexList.Clear();
                other._bb.Max = new Vector3(99999, 99999, 99999);
                other._bb.Min = new Vector3(-99999, -99999, -99999);
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
        }
    }
}
