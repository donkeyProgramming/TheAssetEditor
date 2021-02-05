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
        protected VertexBuffer _vertexBuffer;
        protected IndexBuffer _indexBuffer;

        protected ushort[] _indexList;

        public Vector3 Pivot { get; set; }

        BoundingBox _boundingBox;
        public BoundingBox BoundingBox => _boundingBox;

        public abstract Vector3 GetVertex(int index);
        public abstract int VertexCount();

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

        protected void BuildBoundingBox()
        {
            var count = VertexCount();
            var points = new Vector3[count];
            for (int i = 0; i < count; i++)
                points[i] = GetVertex(i);
            _boundingBox = BoundingBox.CreateFromPoints(points);
        }

        public virtual void Dispose()
        {
            _vertexBuffer.Dispose();
            _indexBuffer.Dispose();
        }
    }
}
