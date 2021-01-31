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
        float? Intersect(Matrix modelMatrix, Ray ray);
    }
}
