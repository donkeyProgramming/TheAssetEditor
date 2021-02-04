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
        // Intersection - Picking
        float? IntersectObject(Ray ray, Matrix modelMatrix);
        public float? IntersectFace(Ray ray, Matrix modelMatrix, out int? face);

        // Intersection - Multiselect
        bool IntersectObject(BoundingFrustum boundingFrustum, Matrix modelMatrix);


        public void ApplyMesh(Effect effect, GraphicsDevice device);
        public void ApplyMeshPart(Effect effect, GraphicsDevice device, List<int> faceSelection);

        
        public Vector3 GetVertex(int index);
        public int VertexCount();

        BoundingBox BoundingBox { get; }
    }
}
