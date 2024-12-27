using GameWorld.Core.Rendering.Geometry;
using Microsoft.Xna.Framework;

namespace Editors.KitbasherEditor.ChildEditors.PinTool
{
    public static class RegiggingHelper
    {
        public static (Vector3 Position, Vector4 Bones, Vector4 BlendWeights) FindClosestUV(Vector3 worldPosition, MeshObject mesh, Vector3 position)
        {
            var vertexList = mesh.VertexArray;
            var polygonCount = mesh.IndexArray.Length;


            Vector2 closestUV = Vector2.Zero;
            Vector4 closestBlendWeights = Vector4.Zero;
            Vector3 closestPos = Vector3.Zero;
            Vector4 closestBlendIndecies = Vector4.Zero;
            float minDistanceSquared = float.MaxValue;

            for (int i = 0; i < mesh.IndexArray.Count(); i += 3)
            {
                var v0 = vertexList[mesh.IndexArray[i]];
                var v1 = vertexList[mesh.IndexArray[i + 1]];
                var v2 = vertexList[mesh.IndexArray[i + 2]];

                var p0 = v0.Position3() + position;
                var p1 = v1.Position3() + position;
                var p2 = v2.Position3() + position;


                Vector3 closestPoint = ClosestPointOnTriangle(worldPosition, p0, p1, p2);
                float distanceSquared = Vector3.DistanceSquared(closestPoint, worldPosition);

                if (distanceSquared < minDistanceSquared)
                {
                    minDistanceSquared = distanceSquared;

                    // Calculate the barycentric coordinates of the closest point on the triangle
                    Vector3 barycentric = ComputeBarycentricCoordinates(closestPoint, v0.Position3(), v1.Position3(), v2.Position3());

                    // Interpolate the UVs using the barycentric coordinates
                    closestBlendWeights.X = barycentric.X * v0.BlendWeights.X + barycentric.Y * v1.BlendWeights.X + barycentric.Z * v2.BlendWeights.X;
                    closestBlendWeights.Y = barycentric.X * v0.BlendWeights.Y + barycentric.Y * v1.BlendWeights.Y + barycentric.Z * v2.BlendWeights.Y;
                    closestBlendWeights.Z = barycentric.X * v0.BlendWeights.Z + barycentric.Y * v1.BlendWeights.Z + barycentric.Z * v2.BlendWeights.Z;
                    closestBlendWeights.W = barycentric.X * v0.BlendWeights.W + barycentric.Y * v1.BlendWeights.W + barycentric.Z * v2.BlendWeights.W;

                    //closestBlendIndecies = v0.BlendIndices;

                    closestPos = closestPoint;// p0;//barycentric.X * p0 + barycentric.Y * p1+ barycentric.Z * p2;

                    // Resolve bone indices (take indices from the vertex with the highest barycentric coordinate)

                    int maxIndex = barycentric.X > barycentric.Y
               ? (barycentric.X > barycentric.Z ? 0 : 2)
               : (barycentric.Y > barycentric.Z ? 1 : 2);

                    closestBlendIndecies = maxIndex switch
                    {
                        0 => v0.BlendIndices,
                        1 => v1.BlendIndices,
                        2 => v2.BlendIndices,
                        _ => closestBlendIndecies
                    };

                }
            }

            return (closestPos, closestBlendIndecies, closestBlendWeights);
        }

        private static Vector3 ClosestPointOnTriangle(Vector3 p, Vector3 a, Vector3 b, Vector3 c)
        {
            // Compute vectors
            Vector3 ab = b - a;
            Vector3 ac = c - a;
            Vector3 ap = p - a;

            // Project point onto the triangle plane
            float d1 = Vector3.Dot(ab, ap);
            float d2 = Vector3.Dot(ac, ap);
            if (d1 <= 0 && d2 <= 0) return a; // Closest to vertex A

            // Check if within AB edge region
            Vector3 bp = p - b;
            float d3 = Vector3.Dot(ab, bp);
            float d4 = Vector3.Dot(ac, bp);
            if (d3 >= 0 && d4 <= d3) return b; // Closest to vertex B

            // Check if within AC edge region
            float vc = d1 * d4 - d3 * d2;
            if (vc <= 0 && d1 >= 0 && d3 <= 0)
            {
                float v = d1 / (d1 - d3);
                return a + v * ab; // Closest to edge AB
            }

            // Check if within BC edge region
            Vector3 cp = p - c;
            float d5 = Vector3.Dot(ab, cp);
            float d6 = Vector3.Dot(ac, cp);
            if (d6 >= 0 && d5 <= d6) return c; // Closest to vertex C

            // Check if within AC edge region
            float vb = d5 * d2 - d1 * d6;
            if (vb <= 0 && d2 >= 0 && d6 <= 0)
            {
                float w = d2 / (d2 - d6);
                return a + w * ac; // Closest to edge AC
            }

            // Check if within BC edge region
            float va = d3 * d6 - d5 * d4;
            if (va <= 0 && (d4 - d3) >= 0 && (d5 - d6) >= 0)
            {
                float u = (d4 - d3) / ((d4 - d3) + (d5 - d6));
                return b + u * (c - b); // Closest to edge BC
            }

            // Closest to the triangle interior
            float denom = 1.0f / (va + vb + vc);
            float vInterior = vb * denom;
            float wInterior = vc * denom;
            return a + ab * vInterior + ac * wInterior;
        }

        private static Vector3 ComputeBarycentricCoordinates(Vector3 point, Vector3 a, Vector3 b, Vector3 c)
        {
            Vector3 v0 = b - a, v1 = c - a, v2 = point - a;
            float d00 = Vector3.Dot(v0, v0);
            float d01 = Vector3.Dot(v0, v1);
            float d11 = Vector3.Dot(v1, v1);
            float d20 = Vector3.Dot(v2, v0);
            float d21 = Vector3.Dot(v2, v1);
            float denom = d00 * d11 - d01 * d01;

            float v = (d11 * d20 - d01 * d21) / denom;
            float w = (d00 * d21 - d01 * d20) / denom;
            float u = 1.0f - v - w;

            return new Vector3(u, v, w);
        }
    }
}
