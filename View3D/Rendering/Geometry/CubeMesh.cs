using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using View3D.Components.Component;
using View3D.Utility;

namespace View3D.Rendering.Geometry
{
    public class CubeMesh : IndexedMeshGeometry
    {
        VertexPositionNormalTexture[] _vertexArray;

        public CubeMesh(GraphicsDevice device, bool createDefaultMesh = true) : base(device)
        {
            if (createDefaultMesh == false)
                return;

            _vertexDeclaration = new VertexDeclaration(
             new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
             new VertexElement(12, VertexElementFormat.Vector3, VertexElementUsage.Normal, 0),
             new VertexElement(24, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0)
                );

            _vertexArray = new VertexPositionNormalTexture[36];

            Vector3 topLeftFront = new Vector3(-1.0f, 1.0f, 1.0f);
            Vector3 bottomLeftFront = new Vector3(-1.0f, -1.0f, 1.0f);
            Vector3 topRightFront = new Vector3(1.0f, 1.0f, 1.0f);
            Vector3 bottomRightFront = new Vector3(1.0f, -1.0f, 1.0f);
            Vector3 topLeftBack = new Vector3(-1.0f, 1.0f, -1.0f);
            Vector3 topRightBack = new Vector3(1.0f, 1.0f, -1.0f);
            Vector3 bottomLeftBack = new Vector3(-1.0f, -1.0f, -1.0f);
            Vector3 bottomRightBack = new Vector3(1.0f, -1.0f, -1.0f);

            Vector2 textureTopLeft = new Vector2(0.0f, 0.0f);
            Vector2 textureTopRight = new Vector2(1.0f, 0.0f);
            Vector2 textureBottomLeft = new Vector2(0.0f, 1.0f);
            Vector2 textureBottomRight = new Vector2(1.0f, 1.0f);

            Vector3 frontNormal = new Vector3(0.0f, 0.0f, 1.0f);
            Vector3 backNormal = new Vector3(0.0f, 0.0f, -1.0f);
            Vector3 topNormal = new Vector3(0.0f, 1.0f, 0.0f);
            Vector3 bottomNormal = new Vector3(0.0f, -1.0f, 0.0f);
            Vector3 leftNormal = new Vector3(-1.0f, 0.0f, 0.0f);
            Vector3 rightNormal = new Vector3(1.0f, 0.0f, 0.0f);

            _vertexArray = new VertexPositionNormalTexture[36];

            // Front face.
            _vertexArray[0] = new VertexPositionNormalTexture(topLeftFront, frontNormal, textureTopLeft);
            _vertexArray[1] = new VertexPositionNormalTexture(bottomLeftFront, frontNormal, textureBottomLeft);
            _vertexArray[2] = new VertexPositionNormalTexture(topRightFront, frontNormal, textureTopRight);
            _vertexArray[3] = new VertexPositionNormalTexture(bottomLeftFront, frontNormal, textureBottomLeft);
            _vertexArray[4] = new VertexPositionNormalTexture(bottomRightFront, frontNormal, textureBottomRight);
            _vertexArray[5] = new VertexPositionNormalTexture(topRightFront, frontNormal, textureTopRight);

            // Back face.
            _vertexArray[6] = new VertexPositionNormalTexture(topLeftBack, backNormal, textureTopRight);
            _vertexArray[7] = new VertexPositionNormalTexture(topRightBack, backNormal, textureTopLeft);
            _vertexArray[8] = new VertexPositionNormalTexture(bottomLeftBack, backNormal, textureBottomRight);
            _vertexArray[9] = new VertexPositionNormalTexture(bottomLeftBack, backNormal, textureBottomRight);
            _vertexArray[10] = new VertexPositionNormalTexture(topRightBack, backNormal, textureTopLeft);
            _vertexArray[11] = new VertexPositionNormalTexture(bottomRightBack, backNormal, textureBottomLeft);

            // Top face.
            _vertexArray[12] = new VertexPositionNormalTexture(topLeftFront, topNormal, textureBottomLeft);
            _vertexArray[13] = new VertexPositionNormalTexture(topRightBack, topNormal, textureTopRight);
            _vertexArray[14] = new VertexPositionNormalTexture(topLeftBack, topNormal, textureTopLeft);
            _vertexArray[15] = new VertexPositionNormalTexture(topLeftFront, topNormal, textureBottomLeft);
            _vertexArray[16] = new VertexPositionNormalTexture(topRightFront, topNormal, textureBottomRight);
            _vertexArray[17] = new VertexPositionNormalTexture(topRightBack, topNormal, textureTopRight);

            // Bottom face.
            _vertexArray[18] = new VertexPositionNormalTexture(bottomLeftFront, bottomNormal, textureTopLeft);
            _vertexArray[19] = new VertexPositionNormalTexture(bottomLeftBack, bottomNormal, textureBottomLeft);
            _vertexArray[20] = new VertexPositionNormalTexture(bottomRightBack, bottomNormal, textureBottomRight);
            _vertexArray[21] = new VertexPositionNormalTexture(bottomLeftFront, bottomNormal, textureTopLeft);
            _vertexArray[22] = new VertexPositionNormalTexture(bottomRightBack, bottomNormal, textureBottomRight);
            _vertexArray[23] = new VertexPositionNormalTexture(bottomRightFront, bottomNormal, textureTopRight);

            // Left face.
            _vertexArray[24] = new VertexPositionNormalTexture(topLeftFront, leftNormal, textureTopRight);
            _vertexArray[25] = new VertexPositionNormalTexture(bottomLeftBack, leftNormal, textureBottomLeft);
            _vertexArray[26] = new VertexPositionNormalTexture(bottomLeftFront, leftNormal, textureBottomRight);
            _vertexArray[27] = new VertexPositionNormalTexture(topLeftBack, leftNormal, textureTopLeft);
            _vertexArray[28] = new VertexPositionNormalTexture(bottomLeftBack, leftNormal, textureBottomLeft);
            _vertexArray[29] = new VertexPositionNormalTexture(topLeftFront, leftNormal, textureTopRight);

            // Right face.
            _vertexArray[30] = new VertexPositionNormalTexture(topRightFront, rightNormal, textureTopLeft);
            _vertexArray[31] = new VertexPositionNormalTexture(bottomRightFront, rightNormal, textureBottomLeft);
            _vertexArray[32] = new VertexPositionNormalTexture(bottomRightBack, rightNormal, textureBottomRight);
            _vertexArray[33] = new VertexPositionNormalTexture(topRightBack, rightNormal, textureTopRight);
            _vertexArray[34] = new VertexPositionNormalTexture(topRightFront, rightNormal, textureTopLeft);
            _vertexArray[35] = new VertexPositionNormalTexture(bottomRightBack, rightNormal, textureBottomRight);

            _indexList = new ushort[36];
            for (ushort i = 0; i < 36; i++)
                _indexList[i] = i;

            CreateModelFromBuffers(device);
            CreateIndexFromBuffers(device);
            BuildBoundingBox();
        }

        void CreateModelFromBuffers(GraphicsDevice device)
        {
            _vertexBuffer = new VertexBuffer(device, _vertexDeclaration, _vertexArray.Length, BufferUsage.None);
            _vertexBuffer.SetData(_vertexArray);
        }

        public override IGeometry Clone()
        {
            var mesh = new CubeMesh(_device, false);
            mesh.Pivot = this.Pivot;
            mesh._vertexDeclaration = _vertexDeclaration;
            mesh._boundingBox = BoundingBox;

            mesh._indexList = new ushort[_indexList.Length];
            _indexList.CopyTo(mesh._indexList, 0);

            mesh._indexBuffer = new IndexBuffer(_device, typeof(short), mesh._indexList.Length, BufferUsage.None);
            mesh._indexBuffer.SetData(mesh._indexList);

            mesh._vertexArray = new VertexPositionNormalTexture[_vertexArray.Length];
            _vertexArray.CopyTo(mesh._vertexArray, 0);

            mesh._vertexBuffer = new VertexBuffer(_device, mesh._vertexDeclaration, mesh._vertexArray.Length, BufferUsage.None);
            mesh._vertexBuffer.SetData(mesh._vertexArray);

            return mesh;
        }

        public override Vector3 GetVertexById(int id)
        {
            return _vertexArray[id].Position;
        }


        public override int VertexCount()
        {
            return _vertexArray.Length;
        }


        public override void RemoveUnusedVertexes(ushort[] newIndexList)
        {
            var uniqeIndexes = newIndexList.Distinct().ToList();
            uniqeIndexes.Sort();

            var newVertexList = new List<VertexPositionNormalTexture>();
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
            _vertexArray[vertexId].Position = position;
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
