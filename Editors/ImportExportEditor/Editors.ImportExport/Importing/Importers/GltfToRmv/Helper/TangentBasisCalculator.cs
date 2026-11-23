using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Shared.GameFormats.RigidModel;

namespace Editors.ImportExport.Importing.Importers.GltfToRmv.Helper
{
    public class TangentBasisCalculator
    {
        public static void CalculateForRmv2Mesh(RmvMesh rmv2Mesh)
        {
            for (var i = 0; i < rmv2Mesh.IndexList.Length; i += 3)
            {
                var i0 = rmv2Mesh.IndexList[i];
                var i1 = rmv2Mesh.IndexList[i + 1];
                var i2 = rmv2Mesh.IndexList[i + 2];

                var v0 = rmv2Mesh.VertexList[i0];
                var v1 = rmv2Mesh.VertexList[i1];
                var v2 = rmv2Mesh.VertexList[i2];

                // Calculate the edges of the triangle
                var edge1 = v1.Position - v0.Position;
                var edge2 = v2.Position - v0.Position;

                // Calculate the differences in UV coordinates
                var deltaUV1 = v1.Uv - v0.Uv;
                var deltaUV2 = v2.Uv - v0.Uv;

                var determinant = deltaUV1.X * deltaUV2.Y - deltaUV2.X * deltaUV1.Y;
                if (Math.Abs(determinant) <= 0.000001f)
                    continue;

                // Calculate the tangent and bitangent
                float f = 1.0f / determinant;

                var tangent = new Vector3(
                    f * (deltaUV2.Y * edge1.X - deltaUV1.Y * edge2.X),
                    f * (deltaUV2.Y * edge1.Y - deltaUV1.Y * edge2.Y),
                    f * (deltaUV2.Y * edge1.Z - deltaUV1.Y * edge2.Z)
                );               

                var bitangent = new Vector3(
                    f * (-deltaUV2.X * edge1.X + deltaUV1.X * edge2.X),
                    f * (-deltaUV2.X * edge1.Y + deltaUV1.X * edge2.Y),
                    f * (-deltaUV2.X * edge1.Z + deltaUV1.X * edge2.Z)
                );

                // Add to existing vectors, has the effect of a "weighted average"
                v0.Tangent += tangent;
                v1.Tangent += tangent;
                v2.Tangent += tangent;

                v0.BiNormal += bitangent;
                v1.BiNormal += bitangent;
                v2.BiNormal += bitangent;
            }
            
            // Normalize the averaged vectors and provide a stable basis for
            // triangles whose UVs do not define a tangent direction.
            foreach (var vertex in rmv2Mesh.VertexList)
            {
                var normal = NormalizeOrDefault(vertex.Normal, Vector3.UnitZ);
                var tangentFallbackAxis = Math.Abs(normal.Z) < 0.999f
                    ? Vector3.UnitZ
                    : Vector3.UnitY;
                var tangentFallback = Vector3.Cross(tangentFallbackAxis, normal);

                vertex.Tangent = NormalizeOrDefault(vertex.Tangent, tangentFallback);
                vertex.BiNormal = NormalizeOrDefault(
                    vertex.BiNormal,
                    Vector3.Cross(normal, vertex.Tangent));
            }
        }

        private static Vector3 NormalizeOrDefault(Vector3 value, Vector3 fallback)
        {
            if (IsFinite(value) && value.LengthSquared() > 0.000000000001f)
                return Vector3.Normalize(value);

            return Vector3.Normalize(fallback);
        }

        private static bool IsFinite(Vector3 value) =>
            float.IsFinite(value.X) &&
            float.IsFinite(value.Y) &&
            float.IsFinite(value.Z);
    }
}
