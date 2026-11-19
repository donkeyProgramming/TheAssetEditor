using GameWorld.Core.Rendering;
using GameWorld.Core.Rendering.Geometry;
using MeshDecimator;
using MeshDecimator.Algorithms;
using Microsoft.Xna.Framework;

namespace GameWorld.Core.Services.SceneSaving.Lod.MeshDecimatorIntegration
{
    public class DecimatorMeshOptimizer
    {
        public static MeshObject GetReducedMeshCopy(MeshObject original, float factor) => GetReducedMeshCopies([original], factor)[0];

        /// <summary>
        /// Reduces a whole model in one pass: all meshes are decimated together against one triangle
        /// target, so the cheapest reductions anywhere in the model go first. A dense mesh can lose more
        /// than the factor while a sparse one (or a large feather card) loses less or nothing. The
        /// result has one mesh per input mesh, in the same order; a mesh can end up with no triangles.
        /// </summary>
        public static List<MeshObject> GetReducedMeshCopies(IReadOnlyList<MeshObject> originals, float factor)
        {
            var totalVertices = originals.Sum(x => x.VertexArray.Length);
            var positions = new MeshDecimator.Math.Vector3d[totalVertices];
            var normals = new MeshDecimator.Math.Vector3[totalVertices];
            var tangents = new MeshDecimator.Math.Vector4[totalVertices];
            var uv0 = new MeshDecimator.Math.Vector2[totalVertices];
            var uv1 = new MeshDecimator.Math.Vector2[totalVertices];
            var boneWeights = new BoneWeight[totalVertices];
            var subMeshIndices = new int[originals.Count][];

            // Each mesh is a sub-mesh of one decimator mesh, with its own range of vertices
            var vertexOffset = 0;
            for (var meshIndex = 0; meshIndex < originals.Count; meshIndex++)
            {
                var original = originals[meshIndex];
                var weightCount = original.WeightCount;
                for (var i = 0; i < original.VertexArray.Length; i++)
                {
                    var v = original.VertexArray[i];
                    var dst = vertexOffset + i;
                    positions[dst] = new MeshDecimator.Math.Vector3d(v.Position.X, v.Position.Y, v.Position.Z);
                    normals[dst] = new MeshDecimator.Math.Vector3(v.Normal.X, v.Normal.Y, v.Normal.Z);
                    tangents[dst] = new MeshDecimator.Math.Vector4(v.Tangent.X, v.Tangent.Y, v.Tangent.Z, 0); // Should last 0 be 1?
                    uv0[dst] = new MeshDecimator.Math.Vector2(v.TextureCoordinate.X, v.TextureCoordinate.Y);
                    uv1[dst] = new MeshDecimator.Math.Vector2(v.TextureCoordinate1.X, v.TextureCoordinate1.Y);
                    boneWeights[dst] = weightCount switch
                    {
                        4 => new BoneWeight((int)v.BlendIndices.X, (int)v.BlendIndices.Y, (int)v.BlendIndices.Z, (int)v.BlendIndices.W,
                                            v.BlendWeights.X, v.BlendWeights.Y, v.BlendWeights.Z, v.BlendWeights.W),
                        2 => new BoneWeight((int)v.BlendIndices.X, (int)v.BlendIndices.Y, 0, 0, v.BlendWeights.X, v.BlendWeights.Y, 0, 0),
                        _ => new BoneWeight(0, 0, 0, 0, 0, 0, 0, 0),
                    };
                }

                subMeshIndices[meshIndex] = original.IndexArray.Select(x => x + vertexOffset).ToArray();
                vertexOffset += original.VertexArray.Length;
            }

            var sourceMesh = new Mesh(positions, subMeshIndices)
            {
                Normals = normals,
                Tangents = tangents,
                BoneWeights = boneWeights,
            };
            sourceMesh.SetUVs(0, uv0);
            sourceMesh.SetUVs(1, uv1);

            // The factor is the fraction of the model's triangles to keep
            var currentTriangleCount = originals.Sum(x => x.IndexArray.Length / 3);
            var targetTriangleCount = (int)Math.Ceiling(currentTriangleCount * factor);

            var algorithm = (FastQuadricMeshSimplification)MeshDecimation.CreateAlgorithm(Algorithm.FastQuadricMesh);
            algorithm.PreserveBorders = true;
            algorithm.RemoveParts = true;
            var destMesh = MeshDecimation.DecimateMesh(algorithm, sourceMesh, targetTriangleCount);

            var destIndices = destMesh.GetSubMeshIndices();
            var output = new List<MeshObject>(originals.Count);
            for (var meshIndex = 0; meshIndex < originals.Count; meshIndex++)
                output.Add(CreateMesh(originals[meshIndex], destMesh, destIndices[meshIndex]));
            return output;
        }

        // Builds one mesh from its sub-mesh of the decimated result, with only the vertices it uses
        static MeshObject CreateMesh(MeshObject original, Mesh destMesh, int[] destIndices)
        {
            var localIndexOf = new Dictionary<int, int>();
            var vertices = new List<VertexPositionNormalTextureCustom>();
            var indices = new ushort[destIndices.Length];
            for (var i = 0; i < destIndices.Length; i++)
            {
                if (!localIndexOf.TryGetValue(destIndices[i], out var local))
                {
                    local = vertices.Count;
                    localIndexOf[destIndices[i]] = local;
                    vertices.Add(CreateVertex(destMesh, destIndices[i], original.WeightCount));
                }
                indices[i] = (ushort)local;
            }

            var clone = original.Clone(false);
            clone.IndexArray = indices;
            clone.VertexArray = vertices.ToArray();

            clone.RebuildIndexBuffer();
            clone.RebuildVertexBuffer();

            return clone;
        }

        static VertexPositionNormalTextureCustom CreateVertex(Mesh destMesh, int index, int weightCount)
        {
            var pos = destMesh.Vertices[index];
            var norm = destMesh.Normals[index];
            var tangents = destMesh.Tangents[index];
            var uv = destMesh.UV1[index];
            var uv1 = destMesh.UV2[index];
            var boneWeight = destMesh.BoneWeights[index];

            var normal = new Vector3(norm.x, norm.y, norm.z);
            var tangent = new Vector3(tangents.x, tangents.y, tangents.z);
            var binormal = Vector3.Normalize(Vector3.Cross(normal, tangent));// * sign

            var vert = new VertexPositionNormalTextureCustom();
            vert.Position = new Vector4((float)pos.x, (float)pos.y, (float)pos.z, 1);
            vert.Normal = new Vector3(norm.x, norm.y, norm.z);
            vert.Tangent = new Vector3(tangents.x, tangents.y, tangents.z);
            vert.BiNormal = new Vector3(binormal.X, binormal.Y, binormal.Z);
            vert.TextureCoordinate = new Vector2(uv.x, uv.y);
            vert.TextureCoordinate1 = new Vector2(uv1.x, uv1.y);

            if (weightCount == 4)
            {
                vert.BlendIndices = new Vector4(boneWeight.boneIndex0, boneWeight.boneIndex1, boneWeight.boneIndex2, boneWeight.boneIndex3);
                vert.BlendWeights = new Vector4(boneWeight.boneWeight0, boneWeight.boneWeight1, boneWeight.boneWeight2, boneWeight.boneWeight3);
            }
            else if (weightCount == 2)
            {
                vert.BlendIndices = new Vector4(boneWeight.boneIndex0, boneWeight.boneIndex1, 0, 0);
                vert.BlendWeights = new Vector4(boneWeight.boneWeight0, boneWeight.boneWeight1, 0, 0);
            }
            else if (weightCount == 0)
            {
                vert.BlendIndices = new Vector4(0, 0, 0, 0);
                vert.BlendWeights = new Vector4(0, 0, 0, 0);
            }

            if (vert.BlendWeights.X + vert.BlendWeights.Y + vert.BlendWeights.Z + vert.BlendWeights.W == 0)
                vert.BlendWeights.X = 1;

            return vert;
        }
    }
}
