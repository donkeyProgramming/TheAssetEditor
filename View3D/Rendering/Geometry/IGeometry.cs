using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;

namespace View3D.Rendering.Geometry
{
    public interface IGeometry : IDisposable
    {
        public VertexBuffer VertexBuffer { get; }
        float? Intersect(Ray ray, Matrix modelMatrix);
        public bool IntersectFace(Ray ray, Matrix modelMatrix, out int faceIndex);
    }
}
