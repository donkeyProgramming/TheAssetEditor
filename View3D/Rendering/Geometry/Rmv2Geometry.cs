using Filetypes.RigidModel;
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

        public Rmv2Geometry(RmvSubModel modelPart, GraphicsDevice device) : base(device)
        {
            Pivot = new Vector3(modelPart.Header.Transform.Pivot.X, modelPart.Header.Transform.Pivot.Y, modelPart.Header.Transform.Pivot.Z);

            _vertexDeclaration = VertexPositionNormalTextureCustom.VertexDeclaration;
            _vertexArray = new VertexPositionNormalTextureCustom[modelPart.Mesh.VertexList.Length];
            _indexList = (ushort[])modelPart.Mesh.IndexList.Clone();

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

                if (modelPart.Header.VertextType == VertexFormat.Cinematic)
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
                else if (modelPart.Header.VertextType == VertexFormat.Weighted)
                {
                    _vertexArray[i].BlendIndices.X = vertex.BoneIndex[0];
                    _vertexArray[i].BlendWeights.X = vertex.BoneWeight[0];
                    WeightCount = 1;
                }
            }

            RebuildVertexBuffer();
            CreateIndexFromBuffers();
        }

        protected Rmv2Geometry(GraphicsDevice device) : base(device)
        { }

        public override IGeometry Clone()
        {
            var mesh = new Rmv2Geometry(_device);
            CopyInto(mesh);
            mesh.WeightCount = WeightCount;
            return mesh;
        }

        public override Vector3 GetVertexById(int id)
        {
            return new Vector3(_vertexArray[id].Position.X, _vertexArray[id].Position.Y, _vertexArray[id].Position.Z);
        }

        public override void UpdateVertexPosition(int vertexId, Vector3 position)
        {
            _vertexArray[vertexId].Position = new Vector4(position, 1);
        }

        public override List<byte> GetUniqeBlendIndices()
        {
            if (WeightCount == 0)
            {
                return new List<byte>();
            }
            else if (WeightCount == 1 || WeightCount == 4)
            {
                var output = new List<byte>();
                for (int i = 0; i < _vertexArray.Count(); i++)
                {
                    if (WeightCount == 1)
                        output.Add((byte)_vertexArray[i].BlendIndices.X);
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
            for(int i = 0; i < _vertexArray.Length; i++)
            {
                _vertexArray[i].BlendIndices.X = GetValue((byte)_vertexArray[i].BlendIndices.X, remapping);
                _vertexArray[i].BlendIndices.Y = GetValue((byte)_vertexArray[i].BlendIndices.Y, remapping);
                _vertexArray[i].BlendIndices.Z = GetValue((byte)_vertexArray[i].BlendIndices.Z, remapping);
                _vertexArray[i].BlendIndices.W = GetValue((byte)_vertexArray[i].BlendIndices.W, remapping);
            }

            RebuildVertexBuffer();
        }

        byte GetValue(byte currentValue, List<IndexRemapping> remappingList)
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

            CreateIndexFromBuffers();
            RebuildVertexBuffer();
        }
    }
}
