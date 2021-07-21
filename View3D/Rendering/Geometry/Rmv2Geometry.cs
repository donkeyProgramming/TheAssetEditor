using Filetypes.RigidModel;
using Filetypes.RigidModel.Transforms;
using Filetypes.RigidModel.Vertex;
using MeshDecimator;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace View3D.Rendering.Geometry
{
    public class Rmv2Geometry : IndexedMeshGeometry<VertexPositionNormalTextureCustom>
    {
        public int WeightCount { get; set; } = 0;
        VertexFormat _vertedFormat;

        public Rmv2Geometry(RmvSubModel modelPart, IGeometryGraphicsContext context) : base(VertexPositionNormalTextureCustom.VertexDeclaration, context)
        {
            Pivot = new Vector3(modelPart.Header.Transform.Pivot.X, modelPart.Header.Transform.Pivot.Y, modelPart.Header.Transform.Pivot.Z);

            _vertexArray = new VertexPositionNormalTextureCustom[modelPart.Mesh.VertexList.Length];
            _indexList = (ushort[])modelPart.Mesh.IndexList.Clone();
            _vertedFormat = modelPart.Header.VertextType;

            for (int i = 0; i < modelPart.Mesh.VertexList.Length; i++)
            {
                var vertex = modelPart.Mesh.VertexList[i];
                _vertexArray[i].Position = new Vector4(vertex.Postition.X, vertex.Postition.Y, vertex.Postition.Z, 1);

                _vertexArray[i].Normal = new Vector3(vertex.Normal.X, vertex.Normal.Y, vertex.Normal.Z);
                _vertexArray[i].BiNormal = new Vector3(vertex.BiNormal.X, vertex.BiNormal.Y, vertex.BiNormal.Z);
                _vertexArray[i].Tangent = new Vector3(vertex.Tangent.X, vertex.Tangent.Y, vertex.Tangent.Z);
                _vertexArray[i].TextureCoordinate = new Vector2(vertex.Uv.X, vertex.Uv.Y);

                _vertexArray[i].BlendIndices = Vector4.Zero;
                _vertexArray[i].BlendWeights = Vector4.Zero;

                if (_vertedFormat == VertexFormat.Cinematic)
                {
                    _vertexArray[i].BlendIndices.X = vertex.BoneIndex[0];
                    _vertexArray[i].BlendIndices.Y = vertex.BoneIndex[1];
                    _vertexArray[i].BlendIndices.Z = vertex.BoneIndex[2];
                    _vertexArray[i].BlendIndices.W = vertex.BoneIndex[3];

                    _vertexArray[i].BlendWeights.X = vertex.BoneWeight[0];
                    _vertexArray[i].BlendWeights.Y = vertex.BoneWeight[1];
                    _vertexArray[i].BlendWeights.Z = vertex.BoneWeight[2];
                    _vertexArray[i].BlendWeights.W = vertex.BoneWeight[3];

                    WeightCount = 4;
                }
                else if (_vertedFormat == VertexFormat.Weighted)
                {
                    _vertexArray[i].BlendIndices.X = vertex.BoneIndex[0];
                    _vertexArray[i].BlendWeights.X = vertex.BoneWeight[0];

                    _vertexArray[i].BlendIndices.Y = vertex.BoneIndex[1];
                    _vertexArray[i].BlendWeights.Y = vertex.BoneWeight[1];
                    WeightCount = 2;
                }
            }

            RebuildVertexBuffer();
            RebuildIndexBuffer();
        }

        protected Rmv2Geometry(IGeometryGraphicsContext context) : base(VertexPositionNormalTextureCustom.VertexDeclaration, context)
        { }


        public RmvMesh CreateRmvMesh()
        {
            RmvMesh mesh = new RmvMesh();
            mesh.IndexList = GetIndexBuffer().ToArray();

            if (_vertedFormat == VertexFormat.Default)
            {
                mesh.VertexList = new DefaultVertex[VertexCount()];

                for (int i = 0; i < mesh.VertexList.Length; i++)
                {
                    _vertexArray[i].Normal = Vector3.Normalize(_vertexArray[i].Normal);
                    _vertexArray[i].BiNormal = Vector3.Normalize(_vertexArray[i].BiNormal);
                    _vertexArray[i].Tangent = Vector3.Normalize(_vertexArray[i].Tangent);

                    mesh.VertexList[i] = new DefaultVertex(
                        new RmvVector4(_vertexArray[i].Position.X, _vertexArray[i].Position.Y, _vertexArray[i].Position.Z, 1),
                        new RmvVector2(_vertexArray[i].TextureCoordinate.X, _vertexArray[i].TextureCoordinate.Y),
                        new RmvVector3(_vertexArray[i].Normal.X, _vertexArray[i].Normal.Y, _vertexArray[i].Normal.Z),
                        new RmvVector3(_vertexArray[i].BiNormal.X, _vertexArray[i].BiNormal.Y, _vertexArray[i].BiNormal.Z),
                        new RmvVector3(_vertexArray[i].Tangent.X, _vertexArray[i].Tangent.Y, _vertexArray[i].Tangent.Z)
                      );
                }
            }
            else if (_vertedFormat == VertexFormat.Weighted)
            {
                mesh.VertexList = new WeightedVertex[VertexCount()];
                for (int i = 0; i < mesh.VertexList.Length; i++)
                {
                    _vertexArray[i].Normal = Vector3.Normalize(_vertexArray[i].Normal);
                    _vertexArray[i].BiNormal = Vector3.Normalize(_vertexArray[i].BiNormal);
                    _vertexArray[i].Tangent = Vector3.Normalize(_vertexArray[i].Tangent);

                    mesh.VertexList[i] = new WeightedVertex(
                        new RmvVector4(_vertexArray[i].Position.X, _vertexArray[i].Position.Y, _vertexArray[i].Position.Z, 1),
                        new RmvVector2(_vertexArray[i].TextureCoordinate.X, _vertexArray[i].TextureCoordinate.Y),
                        new RmvVector3(_vertexArray[i].Normal.X, _vertexArray[i].Normal.Y, _vertexArray[i].Normal.Z),
                        new RmvVector3(_vertexArray[i].BiNormal.X, _vertexArray[i].BiNormal.Y, _vertexArray[i].BiNormal.Z),
                        new RmvVector3(_vertexArray[i].Tangent.X, _vertexArray[i].Tangent.Y, _vertexArray[i].Tangent.Z),
                        new BaseVertex.BoneInformation[2]
                        {
                            new BaseVertex.BoneInformation( (byte)_vertexArray[i].BlendIndices.X, _vertexArray[i].BlendWeights.X),
                            new BaseVertex.BoneInformation( (byte)_vertexArray[i].BlendIndices.Y, _vertexArray[i].BlendWeights.Y),
                        });
                }
            }
            else if (_vertedFormat == VertexFormat.Cinematic)
            {
                mesh.VertexList = new CinematicVertex[VertexCount()];
                for (int i = 0; i < mesh.VertexList.Length; i++)
                {
                    _vertexArray[i].Normal = Vector3.Normalize(_vertexArray[i].Normal);
                    _vertexArray[i].BiNormal = Vector3.Normalize(_vertexArray[i].BiNormal);
                    _vertexArray[i].Tangent = Vector3.Normalize(_vertexArray[i].Tangent);

                    mesh.VertexList[i] = new CinematicVertex(
                        new RmvVector4(_vertexArray[i].Position.X, _vertexArray[i].Position.Y, _vertexArray[i].Position.Z, 1),
                        new RmvVector2(_vertexArray[i].TextureCoordinate.X, _vertexArray[i].TextureCoordinate.Y),
                        new RmvVector3(_vertexArray[i].Normal.X, _vertexArray[i].Normal.Y, _vertexArray[i].Normal.Z),
                        new RmvVector3(_vertexArray[i].BiNormal.X, _vertexArray[i].BiNormal.Y, _vertexArray[i].BiNormal.Z),
                        new RmvVector3(_vertexArray[i].Tangent.X, _vertexArray[i].Tangent.Y, _vertexArray[i].Tangent.Z),
                        new BaseVertex.BoneInformation[4]
                        {
                            new BaseVertex.BoneInformation( (byte)_vertexArray[i].BlendIndices.X, _vertexArray[i].BlendWeights.X),
                            new BaseVertex.BoneInformation( (byte)_vertexArray[i].BlendIndices.Y, _vertexArray[i].BlendWeights.Y),
                            new BaseVertex.BoneInformation( (byte)_vertexArray[i].BlendIndices.Z, _vertexArray[i].BlendWeights.Z),
                            new BaseVertex.BoneInformation( (byte)_vertexArray[i].BlendIndices.W, _vertexArray[i].BlendWeights.W)
                        });
                }
            }
            else
            {
                throw new Exception("Unknown vertex format");
            }

            return mesh;
        }

        public override IGeometry Clone(bool includeMesh = true)
        {
            var mesh = new Rmv2Geometry(Context);
            CopyInto(mesh, includeMesh);
            mesh.WeightCount = WeightCount;
            mesh._vertedFormat = _vertedFormat;
            return mesh;
        }

        public override Vector3 GetVertexById(int id)
        {
            return new Vector3(_vertexArray[id].Position.X, _vertexArray[id].Position.Y, _vertexArray[id].Position.Z);
        }

        public override List<Vector3> GetVertexList()
        {
            var vertCount = VertexCount();
            List<Vector3> output = new List<Vector3>(vertCount);
            for (int i = 0; i < vertCount; i++)
                output.Add(GetVertexById(i));
            return output;
        }

        public override void TransformVertex(int vertexId, Matrix transform)
        {
            _vertexArray[vertexId].Position = Vector4.Transform(_vertexArray[vertexId].Position, transform);

            _vertexArray[vertexId].Normal = Vector3.TransformNormal(_vertexArray[vertexId].Normal, transform);
            _vertexArray[vertexId].BiNormal = Vector3.TransformNormal(_vertexArray[vertexId].BiNormal, transform);
            _vertexArray[vertexId].Tangent = Vector3.TransformNormal(_vertexArray[vertexId].Tangent, transform);
        }

        public override void SetTransformVertex(int vertexId, Matrix transform)
        {
            _vertexArray[vertexId].Position = Vector4.Transform(new Vector4(0,0,0,1), transform);
        }

        public override void SetVertexWeights(int index, Vector4 newWeights)
        {
            if (_vertedFormat == VertexFormat.Weighted)
            {
                newWeights.X = newWeights.X + newWeights.Z;
                newWeights.Y = newWeights.Y + newWeights.W;
            }

            _vertexArray[index].BlendWeights = newWeights;
        }

        public override void SetVertexBlendIndex(int index, Vector4 blendIndex)
        {
            if (_vertedFormat == VertexFormat.Weighted)
            {
                blendIndex.Z = 0;
                blendIndex.W = 0;
            }

            _vertexArray[index].BlendIndices = blendIndex;
        }

        public override List<byte> GetUniqeBlendIndices()
        {
            if (WeightCount == 0)
            {
                return new List<byte>();
            }
            else if (WeightCount == 2 || WeightCount == 4)
            {
                var output = new List<byte>();
                for (int i = 0; i < _vertexArray.Count(); i++)
                {
                    if (WeightCount == 2)
                    {
                        output.Add((byte)_vertexArray[i].BlendIndices.X);
                        output.Add((byte)_vertexArray[i].BlendIndices.Y);
                    }
                    else if (WeightCount == 4)
                    {
                        output.Add((byte)_vertexArray[i].BlendIndices.X);
                        output.Add((byte)_vertexArray[i].BlendIndices.Y);
                        output.Add((byte)_vertexArray[i].BlendIndices.Z);
                        output.Add((byte)_vertexArray[i].BlendIndices.W);
                    }
                    else
                        throw new Exception("Unknown weight count");
                }

                return output.Distinct().ToList();
            }
            else
                throw new Exception("Unknown weight count");

        }

        public override void UpdateAnimationIndecies(List<IndexRemapping> remapping)
        {
            for (int i = 0; i < _vertexArray.Length; i++)
            {
                _vertexArray[i].BlendIndices.X = GetMappedBlendIndex((byte)_vertexArray[i].BlendIndices.X, remapping);
                _vertexArray[i].BlendIndices.Y = GetMappedBlendIndex((byte)_vertexArray[i].BlendIndices.Y, remapping);
                _vertexArray[i].BlendIndices.Z = GetMappedBlendIndex((byte)_vertexArray[i].BlendIndices.Z, remapping);
                _vertexArray[i].BlendIndices.W = GetMappedBlendIndex((byte)_vertexArray[i].BlendIndices.W, remapping);

                var totalBlendWeight = _vertexArray[i].BlendWeights.X + _vertexArray[i].BlendWeights.Y + _vertexArray[i].BlendWeights.Z + _vertexArray[i].BlendWeights.W;
                if ( (1 - totalBlendWeight) >= float.Epsilon)
                {
                    var diff = 1 - totalBlendWeight;
                    float diffPart = diff / WeightCount;

                    _vertexArray[i].BlendWeights.X += diffPart;
                    _vertexArray[i].BlendWeights.Y += diffPart;
                    _vertexArray[i].BlendWeights.Z += diffPart;
                    _vertexArray[i].BlendWeights.W += diffPart;
                }
                var totalBlendWeight2 = _vertexArray[i].BlendWeights.X + _vertexArray[i].BlendWeights.Y + _vertexArray[i].BlendWeights.Z + _vertexArray[i].BlendWeights.W;
            }

            RebuildVertexBuffer();
        }

        byte GetMappedBlendIndex(byte currentValue, List<IndexRemapping> remappingList)
        {
            var remappingItem = remappingList.FirstOrDefault(x => x.OriginalValue == currentValue);
            if (remappingItem != null)
                return remappingItem.NewValue;
            return currentValue;
        }

        public void Merge(List<Rmv2Geometry> others)
        {
            var newVertexBufferSize = others.Sum(x => x.VertexCount()) + VertexCount();
            var newVertexArray = new VertexPositionNormalTextureCustom[newVertexBufferSize];

            // Copy current vertex buffer
            int currentVertexIndex = 0;
            for (; currentVertexIndex < VertexCount(); currentVertexIndex++)
                newVertexArray[currentVertexIndex] = _vertexArray[currentVertexIndex];

            // Index buffers
            var newIndexBufferSize = others.Sum(x => x.GetIndexCount()) + GetIndexCount();
            var newIndexArray = new ushort[newIndexBufferSize];

            // Copy current index buffer
            int currentIndexIndex = 0;
            for (; currentIndexIndex < GetIndexCount(); currentIndexIndex++)
                newIndexArray[currentIndexIndex] = _indexList[currentIndexIndex];

            // Copy others into main
            foreach (var geo in others)
            {
                ushort geoOffset = (ushort)(currentVertexIndex);
                int geoVertexIndex = 0;
                for (; geoVertexIndex < geo.VertexCount();)
                    newVertexArray[currentVertexIndex++] = geo._vertexArray[geoVertexIndex++];

                int geoIndexIndex = 0;
                for (; geoIndexIndex < geo.GetIndexCount();)
                    newIndexArray[currentIndexIndex++] = (ushort)(geo._indexList[geoIndexIndex++] + geoOffset); ;
            }


            _vertexArray = newVertexArray;
            _indexList = newIndexArray;

            RebuildIndexBuffer();
            RebuildVertexBuffer();
        }

        public Rmv2Geometry CreatedReducedCopy(float factor)
        {
            var quality = factor;
            // ObjMesh sourceObjMesh = new ObjMesh();
            // sourceObjMesh.ReadFile(sourcePath);
            var sourceVertices = _vertexArray.Select(x => new MeshDecimator.Math.Vector3d(x.Position.X, x.Position.Y, x.Position.Z)).ToArray();


            // var sourceTexCoords3D = sourceObjMesh.TexCoords3D;
            var sourceSubMeshIndices = _indexList.Select(x => (int)x).ToArray();

            var sourceMesh = new Mesh(sourceVertices, sourceSubMeshIndices);
            sourceMesh.Normals = _vertexArray.Select(x => new MeshDecimator.Math.Vector3(x.Normal.X, x.Normal.Y, x.Normal.Z)).ToArray();
            sourceMesh.Tangents = _vertexArray.Select(x => new MeshDecimator.Math.Vector4(x.Tangent.X, x.Tangent.Y, x.Tangent.Z, 0)).ToArray(); // Should last 0 be 1?
            sourceMesh.SetUVs(0, _vertexArray.Select(x => new MeshDecimator.Math.Vector2(x.TextureCoordinate.X, x.TextureCoordinate.Y)).ToArray());

            if (WeightCount == 4)
            {
                sourceMesh.BoneWeights = _vertexArray.Select(x => new BoneWeight(
                    (int)x.BlendIndices.X, (int)x.BlendIndices.Y, (int)x.BlendIndices.Z, (int)x.BlendIndices.W,
                    x.BlendWeights.X, x.BlendWeights.Y, x.BlendWeights.Z, x.BlendWeights.W)).ToArray();
            }
            else if (WeightCount == 2)
            {
                sourceMesh.BoneWeights = _vertexArray.Select(x => new BoneWeight(
                      (int)x.BlendIndices.X, (int)x.BlendIndices.Y, 0, 0,
                      x.BlendWeights.X, x.BlendWeights.Y, 0, 0)).ToArray();
            }
            else if (WeightCount == 0)
            {
                sourceMesh.BoneWeights = _vertexArray.Select(x => new BoneWeight(
                      0, 0, 0, 0,
                      0, 0, 0, 0)).ToArray();
            }

            int currentTriangleCount = sourceSubMeshIndices.Length / 3;
            int targetTriangleCount = (int)Math.Ceiling(currentTriangleCount * quality);




            var algorithm = MeshDecimation.CreateAlgorithm(Algorithm.Default);
            algorithm.Verbose = true;
            Mesh destMesh = MeshDecimation.DecimateMesh(algorithm, sourceMesh, targetTriangleCount);

            var destVertices = destMesh.Vertices;
            var destNormals = destMesh.Normals;
            var destIndices = destMesh.GetSubMeshIndices();

            VertexPositionNormalTextureCustom[] outputVerts = new VertexPositionNormalTextureCustom[destVertices.Length];

            for (int i = 0; i < outputVerts.Length; i++)
            {
                var pos = destMesh.Vertices[i];
                var norm = destMesh.Normals[i];
                var tangents = destMesh.Tangents[i];
                var uv = destMesh.UV1[i];
                var boneWeight = destMesh.BoneWeights[i];

                Vector3 normal = new Vector3(norm.x, norm.y, norm.z);
                Vector3 tangent = new Vector3(tangents.x, tangents.y, tangents.z);
                var binormal = Vector3.Normalize(Vector3.Cross(normal, tangent));// * sign

                var vert = new VertexPositionNormalTextureCustom();
                vert.Position = new Vector4((float)pos.x, (float)pos.y, (float)pos.z, 1);
                vert.Normal = new Vector3(norm.x, norm.y, norm.z);
                vert.Tangent = new Vector3(tangents.x, tangents.y, tangents.z);
                vert.BiNormal = new Vector3(binormal.X, binormal.Y, binormal.Z);
                vert.TextureCoordinate = new Vector2(uv.x, uv.y);

                if (WeightCount == 4)
                {
                    vert.BlendIndices = new Vector4(boneWeight.boneIndex0, boneWeight.boneIndex1, boneWeight.boneIndex2, boneWeight.boneIndex3);
                    vert.BlendWeights = new Vector4(boneWeight.boneWeight0, boneWeight.boneWeight1, boneWeight.boneWeight2, boneWeight.boneWeight3);
                }
                else if (WeightCount == 2)
                {
                    vert.BlendIndices = new Vector4(boneWeight.boneIndex0, boneWeight.boneIndex1, 0, 0);
                    vert.BlendWeights = new Vector4(boneWeight.boneWeight0, boneWeight.boneWeight1, 0, 0);
                }
                else if (WeightCount == 0)
                {
                    vert.BlendIndices = new Vector4(0, 0, 0, 0);
                    vert.BlendWeights = new Vector4(0, 0, 0, 0);
                }

                if ((vert.BlendWeights.X + vert.BlendWeights.Y + vert.BlendWeights.Z + vert.BlendWeights.W) == 0)
                    vert.BlendWeights.X = 1;

                outputVerts[i] = vert;
            }

            var clone = Clone(false) as Rmv2Geometry;
            clone._indexList = destIndices[0].Select(x => (ushort)x).ToArray();
            clone._vertexArray = outputVerts;

            clone.RebuildIndexBuffer();
            clone.RebuildVertexBuffer();

            return clone;
        }

        public override void ChangeVertexType(VertexFormat newFormat)
        {
            if (!(newFormat == VertexFormat.Weighted || newFormat == VertexFormat.Default || newFormat == VertexFormat.Cinematic))
                throw new Exception("Not able to change vertex format into this");

            if (newFormat == VertexFormat.Weighted)
            {
                for (int i = 0; i < _vertexArray.Length; i++)
                {
                    if (_vertedFormat == VertexFormat.Default)
                    {
                        _vertexArray[i].BlendIndices = new Vector4(0, 0, 0, 0);
                        _vertexArray[i].BlendWeights = new Vector4(1, 0, 0, 0);
                    }

                    if (_vertedFormat == VertexFormat.Cinematic)
                    {
                        // Find most active weight
                        float highestValue = -1;
                        int currentIndex = 0;

                        if (_vertexArray[i].BlendWeights.X > highestValue)
                        {
                            highestValue = _vertexArray[i].BlendWeights.X;
                            currentIndex = (int)_vertexArray[i].BlendIndices.X;
                        }

                        if (_vertexArray[i].BlendWeights.Y > highestValue)
                        {
                            highestValue = _vertexArray[i].BlendWeights.Y;
                            currentIndex = (int)_vertexArray[i].BlendIndices.Y;
                        }

                        if (_vertexArray[i].BlendWeights.Z > highestValue)
                        {
                            highestValue = _vertexArray[i].BlendWeights.Z;
                            currentIndex = (int)_vertexArray[i].BlendIndices.Z;
                        }

                        if (_vertexArray[i].BlendWeights.W > highestValue)
                        {
                            highestValue = _vertexArray[i].BlendWeights.W;
                            currentIndex = (int)_vertexArray[i].BlendIndices.W;
                        }

                        _vertexArray[i].BlendIndices = new Vector4(currentIndex, 0, 0, 0);
                        _vertexArray[i].BlendWeights = new Vector4(1, 0, 0, 0);
                    }
                }

                WeightCount = 2;
                _vertedFormat = VertexFormat.Weighted;
            }
            else if (newFormat == VertexFormat.Cinematic)
            {
                for (int i = 0; i < _vertexArray.Length; i++)
                {
                    if (_vertedFormat == VertexFormat.Default)
                    {
                        _vertexArray[i].BlendIndices = new Vector4(0, 0, 0, 0);
                        _vertexArray[i].BlendWeights = new Vector4(1, 0, 0, 0);
                    }

                    if (_vertedFormat == VertexFormat.Weighted)
                    {
                        // Find most active weight
                        float highestValue = -1;
                        int currentIndex = 0;

                        if (_vertexArray[i].BlendWeights.X > highestValue)
                        {
                            highestValue = _vertexArray[i].BlendWeights.X;
                            currentIndex = (int)_vertexArray[i].BlendIndices.X;
                        }

                        if (_vertexArray[i].BlendWeights.Y > highestValue)
                        {
                            highestValue = _vertexArray[i].BlendWeights.Y;
                            currentIndex = (int)_vertexArray[i].BlendIndices.Y;
                        }

                        if (_vertexArray[i].BlendWeights.Z > highestValue)
                        {
                            highestValue = _vertexArray[i].BlendWeights.Z;
                            currentIndex = (int)_vertexArray[i].BlendIndices.Z;
                        }

                        if (_vertexArray[i].BlendWeights.W > highestValue)
                        {
                            highestValue = _vertexArray[i].BlendWeights.W;
                            currentIndex = (int)_vertexArray[i].BlendIndices.W;
                        }

                        _vertexArray[i].BlendIndices = new Vector4(currentIndex, 0, 0, 0);
                        _vertexArray[i].BlendWeights = new Vector4(1, 0, 0, 0);
                    }
                }

                WeightCount = 4;
                _vertedFormat = VertexFormat.Cinematic;
            }
            else if (newFormat == VertexFormat.Default)
            {
                for (int i = 0; i < _vertexArray.Length; i++)
                {
                    _vertexArray[i].BlendIndices = new Vector4(0, 0, 0, 0);
                    _vertexArray[i].BlendWeights = new Vector4(0, 0, 0, 0);
                }

                WeightCount = 0;
                _vertedFormat = VertexFormat.Default;
            }

            RebuildVertexBuffer();
        }

        public bool ContainsAnimationBone(int[] boneIndexList)
        {
            if (_vertedFormat == VertexFormat.Default)
                return false;

            for(int i = 0; i < _vertexArray.Length; i++)
            {
                if (DoesVertUseBoneIndex(i, boneIndexList))
                    return true;
            }

            return false;
        }

        public void RemoveAllVertexesNotUsedByBones(int[] boneIndexList)
        {
            //var faceCount = _indexList.Length / 3;
            List<int> facesToRemove = new List<int>();
            for (int f = 0; f < _indexList.Length; f+=3)
            {
                var vert0 = DoesVertUseBoneIndex(_indexList[f + 0], boneIndexList);
                var vert1 = DoesVertUseBoneIndex(_indexList[f+ 1], boneIndexList);
                var vert2 = DoesVertUseBoneIndex(_indexList[f + 2], boneIndexList);

                if (!(vert0 && vert1 && vert2))
                    facesToRemove.Add(f);
            }

            if (facesToRemove.Count != 0)
            {
         
                    RemoveFaces(facesToRemove);
            }
        }

        bool DoesVertUseBoneIndex(int vertIndex, int[] boneIndexList)
        {
            var vertex = _vertexArray[vertIndex];
            foreach (var boneIndex in boneIndexList)
            {
                if (vertex.BlendIndices.X == boneIndex)
                    return true;
                if (vertex.BlendIndices.Y == boneIndex)
                    return true;
                if (vertex.BlendIndices.Z == boneIndex)
                    return true;
                if (vertex.BlendIndices.W == boneIndex)
                    return true;
            }
            return false;
        }


        public VertexPositionNormalTextureCustom GetVertexExtented(int index)
        {
            return _vertexArray[index];
        }
    }
}
