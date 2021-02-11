using Filetypes.RigidModel;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using View3D.Components.Component;
using View3D.Utility;

namespace View3D.Rendering.Geometry
{
    public abstract class IndexedMeshGeometry : IGeometry
    {
        protected GraphicsDevice _device;
        protected VertexDeclaration _vertexDeclaration;
        protected VertexBuffer _vertexBuffer;
        protected IndexBuffer _indexBuffer;

        protected ushort[] _indexList;

        public Vector3 Pivot { get; set; }

        protected BoundingBox _boundingBox;
        public BoundingBox BoundingBox => _boundingBox;

        public abstract Vector3 GetVertexByIndex(int index);
        public abstract int VertexCount();
        public abstract IGeometry Clone();

        protected IndexedMeshGeometry(GraphicsDevice device)
        {
            _device = device;
        }

        protected void CreateIndexFromBuffers(GraphicsDevice device)
        {
            _indexBuffer = new IndexBuffer(device, typeof(short), _indexList.Length, BufferUsage.None);
            _indexBuffer.SetData(_indexList);
        }


        public void ApplyMesh(Effect effect, GraphicsDevice device)
        {
            device.Indices = _indexBuffer;
            device.SetVertexBuffer(_vertexBuffer);
            foreach (var pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, _indexBuffer.IndexCount);
            }
        }

        public void ApplyMeshPart(Effect effect, GraphicsDevice device, List<int> faceSelection)
        {
            device.Indices = _indexBuffer;
            device.SetVertexBuffer(_vertexBuffer);
            foreach (var pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                foreach (var item in faceSelection)
                    device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, item, 1);
            }
        }

        public int GetIndex(int i)
        {
            return _indexList[i];
        }
        public int GetIndexCount()
        {
            return _indexList.Length;
        }

        public List<ushort> GetIndexBuffer()
        {
            return _indexList.ToList();
        }

        protected void BuildBoundingBox()
        {
            var count = VertexCount();
            var points = new Vector3[count];
            for (int i = 0; i < count; i++)
                points[i] = GetVertexByIndex(i);
            _boundingBox = BoundingBox.CreateFromPoints(points);
        }

        public virtual void Dispose()
        {
            _vertexBuffer.Dispose();
            _indexBuffer.Dispose();
        }

        public void RemoveFaces(List<int> facesToDelete)
        {
            var newIndexList = new ushort[_indexList.Length - (facesToDelete.Count * 3)];
            var writeIndex = 0;
            for (ushort i = 0; i < _indexList.Length; )
            {
                if (facesToDelete.Contains(i) == false)
                    newIndexList[writeIndex++] = _indexList[i++];
                else
                    i+= 3;
            }

            RemoveUnusedVertexes(newIndexList);
           
        }

        public abstract void RemoveUnusedVertexes(ushort[] newIndexList);

        public abstract Vector3 GetVertexById(int id);

        public abstract void UpdateVertexPosition(int vertexId, Vector3 position);
        public abstract void RebuildVertexBuffer();
    }
}
