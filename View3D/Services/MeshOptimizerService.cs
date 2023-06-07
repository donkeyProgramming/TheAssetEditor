using MeshDecimator;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using View3D.Rendering;
using View3D.Rendering.Geometry;
using Simplygon;
using System.Windows.Interop;
using SharpDX.Direct3D9;
using System.Reflection;
using System.Windows.Controls;

namespace View3D.Services
{
    public class MeshOptimizerService
    {
        public static MeshObject CreatedReducedCopyOld(MeshObject original, float factor)
        {
            var quality = factor;
            var sourceVertices = original.VertexArray.Select(x => new MeshDecimator.Math.Vector3d(x.Position.X, x.Position.Y, x.Position.Z)).ToArray();
            var sourceSubMeshIndices = original.IndexArray.Select(x => (int)x).ToArray();

            var sourceMesh = new MeshDecimator.Mesh(sourceVertices, sourceSubMeshIndices);
            sourceMesh.Normals = original.VertexArray.Select(x => new MeshDecimator.Math.Vector3(x.Normal.X, x.Normal.Y, x.Normal.Z)).ToArray();
            sourceMesh.Tangents = original.VertexArray.Select(x => new MeshDecimator.Math.Vector4(x.Tangent.X, x.Tangent.Y, x.Tangent.Z, 0)).ToArray(); // Should last 0 be 1?
            sourceMesh.SetUVs(0, original.VertexArray.Select(x => new MeshDecimator.Math.Vector2(x.TextureCoordinate.X, x.TextureCoordinate.Y)).ToArray());

            if (original.WeightCount == 4)
            {
                sourceMesh.BoneWeights = original.VertexArray.Select(x => new BoneWeight(
                    (int)x.BlendIndices.X, (int)x.BlendIndices.Y, (int)x.BlendIndices.Z, (int)x.BlendIndices.W,
                    x.BlendWeights.X, x.BlendWeights.Y, x.BlendWeights.Z, x.BlendWeights.W)).ToArray();
            }
            else if (original.WeightCount == 2)
            {
                sourceMesh.BoneWeights = original.VertexArray.Select(x => new BoneWeight(
                      (int)x.BlendIndices.X, (int)x.BlendIndices.Y, 0, 0,
                      x.BlendWeights.X, x.BlendWeights.Y, 0, 0)).ToArray();
            }
            else if (original.WeightCount == 0)
            {
                sourceMesh.BoneWeights = original.VertexArray.Select(x => new BoneWeight(
                      0, 0, 0, 0,
                      0, 0, 0, 0)).ToArray();
            }

            int currentTriangleCount = sourceSubMeshIndices.Length / 3;
            int targetTriangleCount = (int)Math.Ceiling(currentTriangleCount * quality);

            var algorithm = MeshDecimation.CreateAlgorithm(Algorithm.Default);
            algorithm.Verbose = true;
            MeshDecimator.Mesh destMesh = MeshDecimation.DecimateMesh(algorithm, sourceMesh, targetTriangleCount);

            var destVertices = destMesh.Vertices;
            var destNormals = destMesh.Normals;
            var destIndices = destMesh.GetSubMeshIndices();

            var outputVerts = new VertexPositionNormalTextureCustom[destVertices.Length];

            for (int i = 0; i < outputVerts.Length; i++)
            {
                var pos = destMesh.Vertices[i];
                var norm = destMesh.Normals[i];
                var tangents = destMesh.Tangents[i];
                var uv = destMesh.UV1[i];
                var boneWeight = destMesh.BoneWeights[i];

                Vector3 normal = new Vector3(norm.x, norm.y, norm.z);
                Vector3 tangent = new Vector3(tangents.x, tangents.y, tangents.z);
                var binormal = Vector3.Normalize(Vector3.Cross(normal, tangent));// * sign

                var vert = new VertexPositionNormalTextureCustom();
                vert.Position = new Vector4((float)pos.x, (float)pos.y, (float)pos.z, 1);
                vert.Normal = new Vector3(norm.x, norm.y, norm.z);
                vert.Tangent = new Vector3(tangents.x, tangents.y, tangents.z);
                vert.BiNormal = new Vector3(binormal.X, binormal.Y, binormal.Z);
                vert.TextureCoordinate = new Vector2(uv.x, uv.y);

                if (original.WeightCount == 4)
                {
                    vert.BlendIndices = new Vector4(boneWeight.boneIndex0, boneWeight.boneIndex1, boneWeight.boneIndex2, boneWeight.boneIndex3);
                    vert.BlendWeights = new Vector4(boneWeight.boneWeight0, boneWeight.boneWeight1, boneWeight.boneWeight2, boneWeight.boneWeight3);
                }
                else if (original.WeightCount == 2)
                {
                    vert.BlendIndices = new Vector4(boneWeight.boneIndex0, boneWeight.boneIndex1, 0, 0);
                    vert.BlendWeights = new Vector4(boneWeight.boneWeight0, boneWeight.boneWeight1, 0, 0);
                }
                else if (original.WeightCount == 0)
                {
                    vert.BlendIndices = new Vector4(0, 0, 0, 0);
                    vert.BlendWeights = new Vector4(0, 0, 0, 0);
                }

                if ((vert.BlendWeights.X + vert.BlendWeights.Y + vert.BlendWeights.Z + vert.BlendWeights.W) == 0)
                    vert.BlendWeights.X = 1;

                outputVerts[i] = vert;
            }

            var clone = original.Clone(false);
            clone.IndexArray = destIndices[0].Select(x => (ushort)x).ToArray();
            clone.VertexArray = outputVerts;

            clone.RebuildIndexBuffer();
            clone.RebuildVertexBuffer();

            return clone;
        }

        public static MeshObject CreatedReducedCopy(MeshObject originalMesh, float factor)
        {
            MeshObject newMesh = originalMesh.Clone(false);
            using (ISimplygon sg = Simplygon.Loader.InitSimplygon(out var simplygonErrorCode, out var simplygonErrorMessage))
            {
                if (simplygonErrorCode == Simplygon.EErrorCodes.NoError)
                {
                    return GetReducedMeshSimplygon(sg, originalMesh, factor);

                }
                else // simplygon failed to initializes, use old mesh reducer
                {
                    return CreatedReducedCopyOld(originalMesh, factor);
                }
            }
        } 

        private static MeshObject GetReducedMeshSimplygon(ISimplygon sg, MeshObject originalMesh, float factor)
        {
            SimplygonHelpers.InitSGGeometryDataObject(sg, out var pGeometryData, originalMesh);
            SimplygonHelpers.FillSGVertices(ref pGeometryData, originalMesh);
            SimplygonHelpers.FillSGTriangles(ref pGeometryData, originalMesh);
            SimplygonHelpers.ReduceSGMesh(sg, pGeometryData, out var newPackedDataSG, factor);

            var reducedMesh = originalMesh.Clone(false);
            reducedMesh.VertexArray = new VertexPositionNormalTextureCustom[newPackedDataSG.GetVertexCount()];
            reducedMesh.IndexArray = new ushort[newPackedDataSG.GetVertexIds().GetItemCount()];

            SimplygonHelpers.CopySGIndicesToMesh(newPackedDataSG, ref reducedMesh);
            SimplygonHelpers.CopySGVerticesToMesh(newPackedDataSG, ref reducedMesh);

            reducedMesh.RebuildIndexBuffer();
            reducedMesh.RebuildVertexBuffer();

            return reducedMesh;
        }

    }
}
