using GameWorld.Core.Rendering;
using GameWorld.Core.Rendering.Geometry;
using GameWorld.Core.SceneNodes;
using Microsoft.Xna.Framework;

namespace Editors.KitbasherEditor.ChildEditors.PinTool
{
    public record struct BoneWeightResult(float DistanceSquared, Vector4 BoneIndices, Vector4 BlendWeights);

    public static class RegiggingHelper
    {
        public static BoneWeightResult FindClosestBoneWeights(Vector3 worldPosition, MeshObject mesh, Vector3 meshPosition, int maxBoneInfluences)
        {
            var vertexList = mesh.VertexArray;

            Vector4 closestBlendWeights = Vector4.Zero;
            Vector4 closestBoneIndices = Vector4.Zero;
            float minDistanceSquared = float.MaxValue;

            for (var i = 0; i < mesh.IndexArray.Length; i += 3)
            {
                var v0 = vertexList[mesh.IndexArray[i]];
                var v1 = vertexList[mesh.IndexArray[i + 1]];
                var v2 = vertexList[mesh.IndexArray[i + 2]];

                var p0 = v0.Position3() + meshPosition;
                var p1 = v1.Position3() + meshPosition;
                var p2 = v2.Position3() + meshPosition;

                var closestPoint = ClosestPointOnTriangle(worldPosition, p0, p1, p2);
                var distanceSquared = Vector3.DistanceSquared(closestPoint, worldPosition);

                if (distanceSquared < minDistanceSquared)
                {
                    minDistanceSquared = distanceSquared;

                    var barycentric = ComputeBarycentricCoordinates(closestPoint, p0, p1, p2);
                    MergeBoneWeights(v0, v1, v2, barycentric, maxBoneInfluences, out closestBoneIndices, out closestBlendWeights);
                }
            }

            return new BoneWeightResult(minDistanceSquared, closestBoneIndices, closestBlendWeights);
        }

        public static BoneWeightResult FindClosestBoneWeightsMultiMesh(Vector3 worldPosition, List<Rmv2MeshNode> sourceMeshes, int maxBoneInfluences)
        {
            var bestResult = new BoneWeightResult(float.MaxValue, Vector4.Zero, Vector4.Zero);

            foreach (var sourceMesh in sourceMeshes)
            {
                var result = FindClosestBoneWeights(worldPosition, sourceMesh.Geometry, sourceMesh.Position, maxBoneInfluences);
                if (result.DistanceSquared < bestResult.DistanceSquared)
                    bestResult = result;
            }

            return bestResult;
        }

        private static void MergeBoneWeights(
            VertexPositionNormalTextureCustom v0,
            VertexPositionNormalTextureCustom v1,
            VertexPositionNormalTextureCustom v2,
            Vector3 barycentric,
            int maxBoneInfluences,
            out Vector4 boneIndices,
            out Vector4 blendWeights)
        {
            // Collect all (boneIndex, interpolatedWeight) contributions from the 3 vertices
            var weightMap = new Dictionary<int, float>();

            AccumulateVertexWeights(weightMap, v0, barycentric.X);
            AccumulateVertexWeights(weightMap, v1, barycentric.Y);
            AccumulateVertexWeights(weightMap, v2, barycentric.Z);

            // Sort by weight descending and keep top N influences
            var sorted = weightMap
                .Where(kv => kv.Value > 0)
                .OrderByDescending(kv => kv.Value)
                .Take(maxBoneInfluences)
                .ToList();

            // Normalize weights to sum to 1.0
            var totalWeight = sorted.Sum(kv => kv.Value);
            var normFactor = totalWeight > 0 ? 1.0f / totalWeight : 0;

            boneIndices = Vector4.Zero;
            blendWeights = Vector4.Zero;

            for (var i = 0; i < sorted.Count && i < 4; i++)
            {
                var idx = (float)sorted[i].Key;
                var wt = sorted[i].Value * normFactor;

                switch (i)
                {
                    case 0: boneIndices.X = idx; blendWeights.X = wt; break;
                    case 1: boneIndices.Y = idx; blendWeights.Y = wt; break;
                    case 2: boneIndices.Z = idx; blendWeights.Z = wt; break;
                    case 3: boneIndices.W = idx; blendWeights.W = wt; break;
                }
            }
        }

        private static void AccumulateVertexWeights(Dictionary<int, float> weightMap, VertexPositionNormalTextureCustom vertex, float barycentricWeight)
        {
            AddWeight(weightMap, (int)vertex.BlendIndices.X, vertex.BlendWeights.X * barycentricWeight);
            AddWeight(weightMap, (int)vertex.BlendIndices.Y, vertex.BlendWeights.Y * barycentricWeight);
            AddWeight(weightMap, (int)vertex.BlendIndices.Z, vertex.BlendWeights.Z * barycentricWeight);
            AddWeight(weightMap, (int)vertex.BlendIndices.W, vertex.BlendWeights.W * barycentricWeight);
        }

        private static void AddWeight(Dictionary<int, float> weightMap, int boneIndex, float weight)
        {
            if (weight <= 0)
                return;

            if (weightMap.TryGetValue(boneIndex, out var existing))
                weightMap[boneIndex] = existing + weight;
            else
                weightMap[boneIndex] = weight;
        }

        private static Vector3 ClosestPointOnTriangle(Vector3 p, Vector3 a, Vector3 b, Vector3 c)
        {
            Vector3 ab = b - a;
            Vector3 ac = c - a;
            Vector3 ap = p - a;

            float d1 = Vector3.Dot(ab, ap);
            float d2 = Vector3.Dot(ac, ap);
            if (d1 <= 0 && d2 <= 0) return a;

            Vector3 bp = p - b;
            float d3 = Vector3.Dot(ab, bp);
            float d4 = Vector3.Dot(ac, bp);
            if (d3 >= 0 && d4 <= d3) return b;

            float vc = d1 * d4 - d3 * d2;
            if (vc <= 0 && d1 >= 0 && d3 <= 0)
            {
                float v = d1 / (d1 - d3);
                return a + v * ab;
            }

            Vector3 cp = p - c;
            float d5 = Vector3.Dot(ab, cp);
            float d6 = Vector3.Dot(ac, cp);
            if (d6 >= 0 && d5 <= d6) return c;

            float vb = d5 * d2 - d1 * d6;
            if (vb <= 0 && d2 >= 0 && d6 <= 0)
            {
                float w = d2 / (d2 - d6);
                return a + w * ac;
            }

            float va = d3 * d6 - d5 * d4;
            if (va <= 0 && (d4 - d3) >= 0 && (d5 - d6) >= 0)
            {
                float u = (d4 - d3) / ((d4 - d3) + (d5 - d6));
                return b + u * (c - b);
            }

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

            if (MathF.Abs(denom) < 1e-10f)
                return new Vector3(1, 0, 0);

            float v = (d11 * d20 - d01 * d21) / denom;
            float w = (d00 * d21 - d01 * d20) / denom;
            float u = 1.0f - v - w;

            return new Vector3(u, v, w);
        }
    }
}
