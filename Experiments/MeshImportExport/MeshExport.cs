using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Shared.GameFormats.RigidModel;
using SharpGLTF.Geometry.VertexTypes;
using SharpGLTF.Geometry;
using SharpGLTF.Materials;
using System.Numerics;

namespace MeshImportExport
{
    internal class MeshExport
    {
        public static MeshBuilder<VertexPositionNormalTangent, VertexTexture1, VertexJoints4> CreateMesh(RmvModel rmvMesh, MaterialBuilder material)
        {
            var mesh = new MeshBuilder<VertexPositionNormalTangent, VertexTexture1, VertexJoints4>(rmvMesh.Material.ModelName);
            mesh.VertexPreprocessor.SetValidationPreprocessors();
            var prim = mesh.UsePrimitive(material);

            var vertexList = new List<VertexBuilder<VertexPositionNormalTangent, VertexTexture1, VertexJoints4>>();
            foreach (var vertex in rmvMesh.Mesh.VertexList)
            {
                var glTfvertex = new VertexBuilder<VertexPositionNormalTangent, VertexTexture1, VertexJoints4>();
                glTfvertex.Geometry.Position = new Vector3(vertex.Position.X, vertex.Position.Y, vertex.Position.Z);
                glTfvertex.Geometry.Normal = new Vector3(vertex.Normal.X, vertex.Normal.Y, vertex.Normal.Z);
                glTfvertex.Geometry.Tangent = new Vector4(vertex.Tangent.X, vertex.Tangent.Y, vertex.Tangent.Z, 1);
                glTfvertex.Material.TexCoord = new Vector2(vertex.Uv.X, vertex.Uv.Y);

                if (vertex.WeightCount == 2)
                {
                    glTfvertex.Skinning.Weights = new Vector4(0, 1, 0, 0);
                    glTfvertex.Skinning.Joints = new Vector4(0, 1, 0, 0);

                    //if (vertex.BoneWeight[0] == 0)
                    //    vertex.BoneIndex[0] = 0;
                    //
                    //if (vertex.BoneWeight[1] == 0)
                    //    vertex.BoneIndex[1] = 0;
                    //
                    //glTfvertex.Skinning.Weights = new Vector4(vertex.BoneWeight[0], vertex.BoneWeight[1], 0, 0);
                    //glTfvertex.Skinning.Joints = new Vector4(vertex.BoneIndex[0], vertex.BoneIndex[1], 0, 0);
                }
                else if (vertex.WeightCount == 4)
                {
                    if (vertex.BoneWeight[0] == 0)
                        vertex.BoneIndex[0] = 0;

                    if (vertex.BoneWeight[1] == 0)
                        vertex.BoneIndex[1] = 0;

                    if (vertex.BoneWeight[2] == 0)
                        vertex.BoneIndex[2] = 0;

                    if (vertex.BoneWeight[3] == 0)
                        vertex.BoneIndex[3] = 0;

                    var sum = vertex.BoneWeight[0] + vertex.BoneWeight[1] + vertex.BoneWeight[2] + vertex.BoneWeight[3];
                    if (Math.Abs(sum - 1) > 4E-07)
                    {

                        vertex.BoneWeight[0] += Math.Abs(sum - 1);
                        // Values are within specified tolerance of each other....
                    }

                    glTfvertex.Skinning.Weights = new Vector4(vertex.BoneWeight[0], vertex.BoneWeight[1], vertex.BoneWeight[2], vertex.BoneWeight[3]);
                    glTfvertex.Skinning.Joints = new Vector4(vertex.BoneIndex[0], vertex.BoneIndex[1], vertex.BoneIndex[2], vertex.BoneIndex[3]);
                }
                else
                {
                    throw new Exception("Woops");
                    //glTfvertex.Skinning.Weights = new Vector4(0, 1, 0, 0);
                    //glTfvertex.Skinning.Joints = new Vector4(0, 1, 0, 0);
                }
               
                vertexList.Add(glTfvertex);
            }

            var triangleCount = rmvMesh.Mesh.IndexList.Length;
            for (var i = 0; i < triangleCount; i += 3)
            {
                var i0 = rmvMesh.Mesh.IndexList[i + 0];
                var i1 = rmvMesh.Mesh.IndexList[i + 1];
                var i2 = rmvMesh.Mesh.IndexList[i + 2];

                prim.AddTriangle(vertexList[i0], vertexList[i1], vertexList[i2]);
            }

            return mesh;

        }

      
    }
}
