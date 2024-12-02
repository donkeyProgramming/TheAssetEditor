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

                // Calculate the tangent and bitangent
                float f = 1.0f / (deltaUV1.X * deltaUV2.Y - deltaUV2.X * deltaUV1.Y);

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
            
            // normalize the "averaged" vectors
            foreach (var vertex in rmv2Mesh.VertexList)
            { 
               // TODO: orthogonalize the tangents and bitangents?
                vertex.Tangent = Vector3.Normalize(vertex.Tangent);
                vertex.BiNormal = Vector3.Normalize(vertex.BiNormal);
            }
        }
    }
}
