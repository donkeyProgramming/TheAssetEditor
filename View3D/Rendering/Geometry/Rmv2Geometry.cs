using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;
using View3D.Components.Component;

namespace View3D.Rendering.Geometry
{
    class Rmv2Geometry : IGeometry
    {
        public VertexBuffer VertexBuffer => throw new NotImplementedException();

        public void ApplyMesh(Effect effect, GraphicsDevice device)
        {
            throw new NotImplementedException();
        }

        public void ApplyMeshPart(Effect effect, GraphicsDevice device, FaceSelection faceSelection)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public float? Intersect(Ray ray, Matrix modelMatrix)
        {
            throw new NotImplementedException();
        }

        public bool IntersectFace(Ray ray, Matrix modelMatrix, out FaceSelection face)
        {
            throw new NotImplementedException();
        }
    }
}
