using Filetypes.RigidModel;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace View3D.Rendering.Geometry
{
    public class Rmv2Geometry : IndexedMeshGeometry
    {
        VertexPositionNormalTextureCustom[] _vertexArray;

        public int WeightCount { get; set; } = 0;

        public Rmv2Geometry(RmvSubModel modelPart, GraphicsDevice device) : base(device)
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

            }

            CreateModelFromBuffers(device);
            CreateIndexFromBuffers(device);
            BuildBoundingBox();
        }

        public Rmv2Geometry(GraphicsDevice device) : base(device)
        { }


        void CreateModelFromBuffers(GraphicsDevice device)
        {
        
            _vertexBuffer = new VertexBuffer(device, _vertexDeclaration, _vertexArray.Length, BufferUsage.None);
            _vertexBuffer.SetData(_vertexArray);
        }

        public override IGeometry Clone()
        {
            var mesh = new Rmv2Geometry(_device);
            mesh.Pivot = Pivot;
            mesh._vertexDeclaration = _vertexDeclaration;
            mesh._boundingBox = BoundingBox;

            mesh._indexList = new ushort[_indexList.Length];
            _indexList.CopyTo(mesh._indexList, 0);

            mesh._indexBuffer = new IndexBuffer(_device, typeof(short), mesh._indexList.Length, BufferUsage.None);
            mesh._indexBuffer.SetData(mesh._indexList);

            mesh._vertexArray = new VertexPositionNormalTextureCustom[_vertexArray.Length];
            _vertexArray.CopyTo(mesh._vertexArray, 0);

            mesh._vertexBuffer = new VertexBuffer(_device, mesh._vertexDeclaration, mesh._vertexArray.Length, BufferUsage.None);
            mesh._vertexBuffer.SetData(mesh._vertexArray);

            return mesh;
        }




        public override Vector3 GetVertexByIndex(int index)
        {
            var vertIndex = index;// _indexList[index];
            return new Vector3(_vertexArray[vertIndex].Position.X, _vertexArray[vertIndex].Position.Y, _vertexArray[vertIndex].Position.Z);
        }

        public override Vector3 GetVertexById(int id)
        {
            return new Vector3(_vertexArray[id].Position.X, _vertexArray[id].Position.Y, _vertexArray[id].Position.Z);
        }

        public override int VertexCount()
        {
            return _vertexArray.Length;
        }


        public override void RemoveUnusedVertexes(ushort[] newIndexList)
        {
            var uniqeIndexes = newIndexList.Distinct().ToList();
            uniqeIndexes.Sort();

            List<VertexPositionNormalTextureCustom> newVertexList = new List<VertexPositionNormalTextureCustom>();
            Dictionary<ushort, ushort> remappingTable = new Dictionary<ushort, ushort>();
            for (ushort i = 0; i < _vertexArray.Length; i++)
            {
                if (uniqeIndexes.Contains(i))
                {
                    remappingTable[i] = (ushort)remappingTable.Count();
                    newVertexList.Add(_vertexArray[i]);
                }
            }

            for (int i = 0; i < newIndexList.Length; i++)
                newIndexList[i] = remappingTable[newIndexList[i]];

            _indexList = newIndexList;
             _indexBuffer = new IndexBuffer(_device, typeof(short), _indexList.Length, BufferUsage.None);
            _indexBuffer.SetData(_indexList);

            _vertexArray = newVertexList.ToArray();
            _vertexBuffer = new VertexBuffer(_device, _vertexDeclaration, _vertexArray.Length, BufferUsage.None);
            _vertexBuffer.SetData(_vertexArray);

            BuildBoundingBox();
        }

        public override void UpdateVertexPosition(int vertexId, Vector3 position)
        {
            _vertexArray[vertexId].Position = new Vector4(position, 1);
        }

        public override void RebuildVertexBuffer()
        {
            if (_vertexBuffer != null)
                _vertexBuffer.Dispose();

            _vertexBuffer = new VertexBuffer(_device, _vertexDeclaration, _vertexArray.Length, BufferUsage.None);
            _vertexBuffer.SetData(_vertexArray);
        }
    }
}
