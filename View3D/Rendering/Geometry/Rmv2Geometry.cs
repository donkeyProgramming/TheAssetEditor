using Filetypes.RigidModel;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace View3D.Rendering.Geometry
{
    class Rmv2Geometry : IndexedMeshGeometry
    {
        VertexDeclaration _vertexDeclaration;
        VertexPositionNormalTextureCustom[] _vertexArray;

        public int WeightCount { get; set; } = 0;

        public Rmv2Geometry(RmvSubModel modelPart, GraphicsDevice device)
        {
            Pivot = new Vector3(modelPart.Header.Transform.Pivot.X, modelPart.Header.Transform.Pivot.Y, modelPart.Header.Transform.Pivot.Z);

            _vertexDeclaration = VertexPositionNormalTextureCustom.VertexDeclaration;
            _vertexArray = new VertexPositionNormalTextureCustom[modelPart.Mesh.VertexList.Length];
            _indexList = (ushort[])modelPart.Mesh._indexList.Clone();

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
                else
                {
                    throw new Exception("Unknown vertex format");
                }
            }

            CreateModelFromBuffers(device);
        }

        void CreateModelFromBuffers(GraphicsDevice device)
        {
            _indexBuffer = new IndexBuffer(device, typeof(short), _indexList.Length, BufferUsage.None);
            _indexBuffer.SetData(_indexList);

            _vertexBuffer = new VertexBuffer(device, _vertexDeclaration, _vertexArray.Length, BufferUsage.None);
            _vertexBuffer.SetData(_vertexArray);
        }

        public override Vector3 GetVertex(int index)
        {
            var vertIndex = _indexList[index];
            return new Vector3(_vertexArray[vertIndex].Position.X, _vertexArray[vertIndex].Position.Y, _vertexArray[vertIndex].Position.Z);
        }
    }
}
