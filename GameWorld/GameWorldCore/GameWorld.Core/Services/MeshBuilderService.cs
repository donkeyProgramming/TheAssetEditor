using GameWorld.Core.Rendering;
using GameWorld.Core.Rendering.Geometry;
using Microsoft.Xna.Framework;
using Shared.GameFormats.RigidModel;
using Shared.GameFormats.RigidModel.Vertex;

namespace GameWorld.Core.Services
{
    public class MeshBuilderService
    {
        private readonly IGeometryGraphicsContextFactory _contextFactory;

        public MeshBuilderService(IGeometryGraphicsContextFactory context)
        {
            _contextFactory = context;
        }

        public MeshObject BuildMeshFromRmvModel(RmvModel rmvModel, string skeletonName)
        {
            var vertexFormat = ModelMaterialEnumHelper.GetToolVertexFormat(rmvModel.Material.BinaryVertexFormat);

            var mesh = new MeshObject(_contextFactory.Create(), skeletonName);
            mesh.ChangeVertexType(vertexFormat, false);
            mesh.VertexArray = new VertexPositionNormalTextureCustom[rmvModel.Mesh.VertexList.Length];
            mesh.IndexArray = (ushort[])rmvModel.Mesh.IndexList.Clone();

            for (var i = 0; i < rmvModel.Mesh.VertexList.Length; i++)
            {
                var vertex = rmvModel.Mesh.VertexList[i];
                mesh.VertexArray[i].Position = vertex.Position;
                mesh.VertexArray[i].Normal = vertex.Normal;
                mesh.VertexArray[i].BiNormal = vertex.BiNormal;
                mesh.VertexArray[i].Tangent = vertex.Tangent;
                mesh.VertexArray[i].TextureCoordinate = vertex.Uv;
                mesh.VertexArray[i].TextureCoordinate1 = vertex.Uv1;

                if (mesh.VertexFormat == UiVertexFormat.Static)
                {
                    mesh.VertexArray[i].BlendIndices = Vector4.Zero;
                    mesh.VertexArray[i].BlendWeights = Vector4.Zero;
                }
                else if (mesh.VertexFormat == UiVertexFormat.Weighted)
                {
                    mesh.VertexArray[i].BlendIndices = new Vector4(vertex.BoneIndex[0], vertex.BoneIndex[1], 0, 0);
                    mesh.VertexArray[i].BlendWeights = new Vector4(vertex.BoneWeight[0], vertex.BoneWeight[1], 0, 0);
                }
                else if (mesh.VertexFormat == UiVertexFormat.Cinematic)
                {
                    mesh.VertexArray[i].BlendIndices = new Vector4(vertex.BoneIndex[0], vertex.BoneIndex[1], vertex.BoneIndex[2], vertex.BoneIndex[3]);
                    mesh.VertexArray[i].BlendWeights = new Vector4(vertex.BoneWeight[0], vertex.BoneWeight[1], vertex.BoneWeight[2], vertex.BoneWeight[3]);
                }
                else
                    throw new Exception("Unknown vertex format");
            }

            mesh.RebuildVertexBuffer();
            mesh.RebuildIndexBuffer();
            mesh.BuildBoundingBox();
            return mesh;
        }

        public RmvMesh CreateRmvMeshFromGeometry(MeshObject geometry, int lodIndex, int meshId, string meshName)
        {
            AssertVertexWeightsAreNormalized(geometry, lodIndex, meshId, meshName);

            // Ensure normalized
            for (var i = 0; i < geometry.VertexArray.Length; i++)
            {
                geometry.VertexArray[i].Normal = Vector3.Normalize(geometry.VertexArray[i].Normal);
                geometry.VertexArray[i].BiNormal = Vector3.Normalize(geometry.VertexArray[i].BiNormal);
                geometry.VertexArray[i].Tangent = Vector3.Normalize(geometry.VertexArray[i].Tangent);
            }

            var mesh = new RmvMesh();
            mesh.IndexList = geometry.GetIndexBuffer().ToArray();
            mesh.VertexList = geometry.VertexArray.
                Select(x => new CommonVertex()
                {
                    Position = x.Position,
                    Normal = x.Normal,
                    BiNormal = x.BiNormal,
                    Tangent = x.Tangent,

                    Colour = new Vector4(0, 0, 0, 1),
                    Uv = x.TextureCoordinate,
                    Uv1 = x.TextureCoordinate1,

                    BoneIndex = x.GetBoneIndexs().Take(geometry.WeightCount).Select(x => (byte)x).ToArray(),
                    BoneWeight = x.GetBoneWeights().Take(geometry.WeightCount).ToArray(),
                    WeightCount = geometry.WeightCount
                }).ToArray();

            return mesh;
        }

        /// <summary>
        /// Normalizes bone weights for every vertex in <paramref name="geometry"/> so that the
        /// active weights (2 for Weighted, 4 for Cinematic) always sum to exactly 1.
        /// Weights beyond the active count are zeroed out. Static meshes are skipped.
        /// </summary>
        public static void NormalizeBoneWeights(MeshObject geometry)
        {
            var weightCount = geometry.WeightCount;
            if (weightCount == 0)
                return;

            for (var i = 0; i < geometry.VertexArray.Length; i++)
            {
                var w = geometry.VertexArray[i].BlendWeights;

                if (weightCount == 2)
                {
                    w.Z = 0f;
                    w.W = 0f;
                }

                var total = w.X + w.Y + w.Z + w.W;
                if (total > 0f)
                {
                    w.X /= total;
                    w.Y /= total;
                    w.Z /= total;
                    w.W /= total;
                }
                else
                {
                    w.X = 1f;
                    w.Y = 0f;
                    w.Z = 0f;
                    w.W = 0f;
                }

                geometry.VertexArray[i].BlendWeights = w;
            }
        }

        static void AssertVertexWeightsAreNormalized(MeshObject geometry, int lodIndex, int meshId, string meshName)
        {
            if (geometry.WeightCount == 0)
                return;

            const float tolerance = 0.1f;
            for (var vertexIndex = 0; vertexIndex < geometry.VertexArray.Length; vertexIndex++)
            {
                var totalWeight = geometry.VertexArray[vertexIndex]
                    .GetBoneWeights()
                    .Take(geometry.WeightCount)
                    .Sum();

                if (MathF.Abs(totalWeight - 1.0f) > tolerance)
                {
                    throw new InvalidOperationException($"Unable to save rmv2 mesh - vertex not normalized. LodIndex:{lodIndex}, MeshId:{meshId}, MeshName:'{meshName}', Vertex:{vertexIndex}, TotalBoneWeight:{totalWeight}.");
                }
            }
        }
    }
}
