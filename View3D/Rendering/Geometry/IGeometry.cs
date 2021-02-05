using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;
using View3D.Components.Component;

namespace View3D.Rendering.Geometry
{
    public interface IGeometry : IDisposable
    {
        public void ApplyMesh(Effect effect, GraphicsDevice device);
        public void ApplyMeshPart(Effect effect, GraphicsDevice device, List<int> faceSelection);

        public Vector3 GetVertex(int index);
        public int VertexCount();

        public int GetIndex(int i);
        public int GetIndexCount();

        public List<ushort> GetIndexBuffer();
        public void SetIndexBufferAndRebuild(List<ushort> buffer);

        BoundingBox BoundingBox { get; }

        IGeometry Clone();
    }
}
