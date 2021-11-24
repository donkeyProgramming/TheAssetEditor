using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using View3D.Rendering.Geometry;

namespace View3D.Utility
{
    public class MeshDistanceCalculator
    {
        public MeshObject Mesh { get; private set; }
        public MeshDistanceCalculator(MeshObject mesh)
        {
            Mesh = mesh;
        }

        public ushort FindClosestVertexIndex(Vector3 point, out float distance)
        {
            var closestDist = float.PositiveInfinity;
            int bestVertexIndex = -1;

            for (int i = 0; i < Mesh.VertexArray.Length; i++)
            {
                var dist = (point - Mesh.VertexArray[i].Position3()).LengthSquared();
                if (dist < closestDist)
                {
                    closestDist = dist;
                    bestVertexIndex = i;
                }
            }

            distance = closestDist;
            return (ushort)bestVertexIndex;
        }
    }
}
