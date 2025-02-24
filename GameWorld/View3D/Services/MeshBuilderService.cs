using System;
using System.Linq;
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

        public RmvMesh CreateRmvMeshFromGeometry(MeshObject geometry)
        {
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
    }
}
