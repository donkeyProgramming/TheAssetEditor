using CommonControls.FileTypes.RigidModel.LodHeader;
using CommonControls.FileTypes.RigidModel.MaterialHeaders;
using CommonControls.FileTypes.RigidModel;
using System.Collections.Generic;
using System.Text;
using CommonControls.FileTypes.RigidModel.Vertex;
using System.Linq;
using Microsoft.Xna.Framework;
using AssetManagement.GenericFormats.Unmanaged;
using CommonControls.FileTypes.RigidModel.Types;
using AssetManagement.GenericFormats.Managed;
using VertexFormat = CommonControls.FileTypes.RigidModel.VertexFormat;
using CommonControls.FileTypes.Animation;
using AssetManagement.MeshHandling;
using System.Runtime.CompilerServices;

namespace AssetManagement.GenericFormats
{
    public class RmvFileBuilder
    {
        /// <summary>
        /// Converts "native structs" meshes to AE internal RMV2 
        /// </summary>
        /// <param name="packedMeshes">Input "native meshes"</param>
        /// <param name="skeletonName">Skeleton name string to put in RMV2, if RMV2 with be "static"</param>        
        /// <returns>Internal RMV2 file class</returns>
        public static RmvFile ConvertToRmv2(List<PackedMesh> packedMeshes, AnimationFile skeletonFile)
        {
            MaterialFactory materialFactory = MaterialFactory.Create();

            var lodCount = 4; // make 4 identical LODs for compatibility reasons

            RmvFile outputFile = new RmvFile()
            {
                Header = new RmvFileHeader()
                {
                    _fileType = Encoding.ASCII.GetBytes("RMV2"),
                    SkeletonName = (skeletonFile != null) ? skeletonFile.Header.SkeletonName : "",
                    Version = RmvVersionEnum.RMV2_V6,
                    LodCount = (uint)lodCount,
                },
                LodHeaders = new RmvLodHeader[lodCount],
            };

            for (int i = 0; i < lodCount; i++)
            {
                outputFile.LodHeaders[i] = new Rmv2LodHeader_V6() { QualityLvl = 0, LodCameraDistance = 0 };
            }

            outputFile.ModelList = new RmvModel[lodCount][];

            for (int lodIndex = 0; lodIndex < lodCount; lodIndex++)
            {
                outputFile.ModelList[lodIndex] = new RmvModel[packedMeshes.Count];

                for (int meshIndex = 0; meshIndex < packedMeshes.Count; meshIndex++)
                {
                    var currentMesh = new RmvModel();
                    outputFile.ModelList[lodIndex][meshIndex] = ConvertPackedMeshToRmvModel(materialFactory, outputFile, packedMeshes[meshIndex], skeletonFile);
                };
            }

            outputFile.UpdateOffsets();

            return outputFile;
        }

        private static void CopyMeshWeights(RmvMesh rmv2DestMesh, PackedMesh srcMesh, AnimationFile skeletonFile)
        {
            if (skeletonFile == null)
                return;

            for (int vertexIndex = 0; vertexIndex < srcMesh.Vertices.Count; vertexIndex++)
            {
                foreach (var influence in srcMesh.Vertices[vertexIndex].influences)
                {
                    CommonWeightProcessor.AddWeightToVertexByBoneName(skeletonFile, rmv2DestMesh.VertexList[vertexIndex], influence.boneName, influence.weight);
                }
            }
        }

        private static RmvModel ConvertPackedMeshToRmvModel(MaterialFactory materialFactory, RmvFile outputFile, PackedMesh packMesh, AnimationFile skeletonFile)
        {
            var currentMesh = new RmvModel();
            currentMesh.Material = materialFactory.CreateMaterial(
                outputFile.Header.Version,
                skeletonFile != null ? ModelMaterialEnum.weighted : ModelMaterialEnum.default_type,
                skeletonFile != null ? VertexFormat.Cinematic : VertexFormat.Static);

            currentMesh.Material.ModelName = packMesh.Name;
            currentMesh.Mesh = ConvertPackedMeshToRmvMesh(currentMesh.Material.BinaryVertexFormat, packMesh, skeletonFile);

            SetRmvModelDefaultTextures(currentMesh);

            return currentMesh;
        }

        private static void SetRmvModelDefaultTextures(RmvModel currentMesh)
        {
            currentMesh.Material.SetTexture(TextureType.BaseColour, @"commontextures\default_base_colour.dds");
            currentMesh.Material.SetTexture(TextureType.Normal, @"commontextures\default_normal.dds");
            currentMesh.Material.SetTexture(TextureType.MaterialMap, @"commontextures\default_material_mat.dds");
            currentMesh.Material.SetTexture(TextureType.Diffuse, @"commontextures\default_metal_material_map.dds");
            currentMesh.Material.SetTexture(TextureType.Specular, @"commontextures\default_metal_material_map.dds");
            currentMesh.Material.SetTexture(TextureType.Gloss, @"commontextures\default_metal_material_map.dds");
        }

        private static RmvMesh ConvertPackedMeshToRmvMesh(VertexFormat vertexFormat, PackedMesh packedInputMesh, AnimationFile skeletonFile)
        {
            RmvMesh rmv2Mesh = new RmvMesh();
            rmv2Mesh.IndexList = new ushort[packedInputMesh.Indices.Count];
            rmv2Mesh.VertexList = new CommonVertex[packedInputMesh.Vertices.Count];

            var originalVerticesPacked = MakePackedVertices(vertexFormat, packedInputMesh, skeletonFile);

            rmv2Mesh.VertexList = originalVerticesPacked.ToArray();
            rmv2Mesh.IndexList = packedInputMesh.Indices.ToArray();

            CopyMeshWeights(rmv2Mesh, packedInputMesh, skeletonFile);

            return rmv2Mesh;
        }

        private static List<CommonVertex> MakePackedVertices(VertexFormat vertexFormat, PackedMesh packedInputMesh, AnimationFile skeletonFile)
        {
            var vertices = new CommonVertex[packedInputMesh.Vertices.Count].ToList();

            for (var vertexIndex = 0; vertexIndex < vertices.Count; vertexIndex++)
            {
                vertices[vertexIndex] = MakePackedVertex(
                   packedInputMesh.Vertices[vertexIndex].Position,
                   packedInputMesh.Vertices[vertexIndex].Normal,
                   packedInputMesh.Vertices[vertexIndex].Uv,
                   packedInputMesh.Vertices[vertexIndex].Tangent,
                   packedInputMesh.Vertices[vertexIndex].BiNormal,
                   skeletonFile);

                // TODO: remove?
                //MakeVertexWeights(vertexFormat, packedInputMesh.Vertices[vertexIndex], vertices[vertexIndex]);
            }

            return vertices;
        }

        private static void MakeVertexWeights(VertexFormat vertexFormat, ExtPackedCommonVertex packedInputVertex, CommonVertex outVertex)
        {
            if (vertexFormat == VertexFormat.Static)
            {
                outVertex.WeightCount = 0;
                outVertex.BoneIndex = new byte[0];
                outVertex.BoneWeight = new float[0];
                return;
            }

            for (int influenceIndex = 0; influenceIndex < 4; influenceIndex++)
            {
                outVertex.BoneIndex[influenceIndex] = (byte)packedInputVertex.influences[influenceIndex].boneIndex;
                outVertex.BoneWeight[influenceIndex] = packedInputVertex.influences[influenceIndex].weight;
            }

            CommonWeightProcessor.SortVertexWeights(outVertex);
            CommonWeightProcessor.NormalizeVertexWeights(outVertex);
        }

        private static CommonVertex MakePackedVertex(
            XMFLOAT4 position,
            XMFLOAT3 normal,
            XMFLOAT2 textureCoords,
            XMFLOAT3 tangent,
            XMFLOAT3 bitangent,
            AnimationFile skeletonFile)
        {
            var normalizedNormal = new Vector3(normal.x, normal.y, normal.z);
            normalizedNormal.Normalize();

            var normalizedTangent = new Vector3(tangent.x, tangent.y, tangent.z);
            normalizedTangent.Normalize();

            var normalizedBitangent = new Vector3(bitangent.x, bitangent.y, bitangent.z);
            normalizedBitangent.Normalize();

            var vertex = new CommonVertex()
            {
                Position = new Vector4(position.x, position.y, position.z, 0),
                Normal = normalizedNormal,
                Uv = new Vector2(textureCoords.x, -textureCoords.y),   // inversion needed for Y,   
                Tangent = normalizedTangent,
                BiNormal = normalizedBitangent,
                WeightCount = 0,
            };

            vertex.WeightCount = 0;

            if (skeletonFile != null)
                vertex.WeightCount = 4;

            vertex.BoneIndex = new byte[vertex.WeightCount];
            vertex.BoneWeight = new float[vertex.WeightCount];

            vertex.WeightCount = 0;

            return vertex;
        }
    }
}
