using System.Collections.Generic;
using System.Linq;
using System.Text;
using AssetManagement.GenericFormats.DataStructures.Managed;
using AssetManagement.GenericFormats.Unmanaged;
using AssetManagement.MeshProcessing.Common;
using Microsoft.Xna.Framework;
using Shared.GameFormats.Animation;
using Shared.GameFormats.RigidModel;
using Shared.GameFormats.RigidModel.LodHeader;
using Shared.GameFormats.RigidModel.MaterialHeaders;
using Shared.GameFormats.RigidModel.Types;
using Shared.GameFormats.RigidModel.Vertex;

namespace AssetManagement.AssetBuilders
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
            var materialFactory = MaterialFactory.Create();

            var lodCount = 4; // make 4 identical LODs for compatibility reasons

            var outputFile = new RmvFile()
            {
                Header = new RmvFileHeader()
                {
                    _fileType = Encoding.ASCII.GetBytes("RMV2"),
                    SkeletonName = skeletonFile != null ? skeletonFile.Header.SkeletonName : "",
                    Version = RmvVersionEnum.RMV2_V6,
                    LodCount = (uint)lodCount,
                },
                LodHeaders = new RmvLodHeader[lodCount],
            };

            for (var i = 0; i < lodCount; i++)
            {
                outputFile.LodHeaders[i] = new Rmv2LodHeader_V6() { QualityLvl = 0, LodCameraDistance = 0 };
            }

            outputFile.ModelList = new RmvModel[lodCount][];

            for (var lodIndex = 0; lodIndex < lodCount; lodIndex++)
            {
                outputFile.ModelList[lodIndex] = new RmvModel[packedMeshes.Count];

                for (var meshIndex = 0; meshIndex < packedMeshes.Count; meshIndex++)
                {
                    var currentMesh = new RmvModel();
                    outputFile.ModelList[lodIndex][meshIndex] = ConvertPackedMeshToRmvModel(materialFactory, outputFile, packedMeshes[meshIndex], skeletonFile);
                };
            }

            outputFile.UpdateOffsets();

            return outputFile;
        }

        private static void ProcessMeshWeights(RmvMesh rmv2DestMesh, PackedMesh srcMesh, AnimationFile skeletonFile)
        {
            if (skeletonFile == null)
                return;

            for (var vertexWeightIndex = 0; vertexWeightIndex < srcMesh.VertexWeights.Count; vertexWeightIndex++)
            {
                CommonWeightProcessor.AddWeightToVertexByBoneName(
                    skeletonFile,
                    rmv2DestMesh.VertexList[srcMesh.VertexWeights[vertexWeightIndex].vertexIndex],
                    srcMesh.VertexWeights[vertexWeightIndex].boneName,
                    srcMesh.VertexWeights[vertexWeightIndex].weight);
            }

            foreach (var vertex in rmv2DestMesh.VertexList)
            {
                CommonWeightProcessor.SortVertexWeightsByWeightValue(vertex);
                CommonWeightProcessor.NormalizeVertexWeights(vertex);
            }
        }


        private static RmvModel ConvertPackedMeshToRmvModel(MaterialFactory materialFactory, RmvFile outputFile, PackedMesh packMesh, AnimationFile skeletonFile)
        {
            var outMesh = new RmvModel();

            var materialCreator = new WeighterMaterialCreator();

            outMesh.Material = materialCreator.CreateEmpty(
                skeletonFile != null ? ModelMaterialEnum.weighted : ModelMaterialEnum.default_type,
                outputFile.Header.Version,
                skeletonFile != null ? VertexFormat.Cinematic : VertexFormat.Static);

            outMesh.Material.ModelName = packMesh.Name;
            outMesh.Mesh = ConvertPackedMeshToRmvMesh(materialFactory, outMesh.Material.BinaryVertexFormat, packMesh, skeletonFile);

            SetRmvModelDefaultTextures(outMesh);

            return outMesh;
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

        private static RmvMesh ConvertPackedMeshToRmvMesh(MaterialFactory materialFactory, VertexFormat vertexFormat, PackedMesh packedInputMesh, AnimationFile skeletonFile)
        {
            var rmv2Mesh = new RmvMesh();
            rmv2Mesh.IndexList = new ushort[packedInputMesh.Indices.Count];
            rmv2Mesh.VertexList = new CommonVertex[packedInputMesh.Vertices.Count];
            rmv2Mesh.VertexList = MakeCommonVertices(vertexFormat, packedInputMesh, skeletonFile).ToArray();            

            for (var indexBufferIndex = 0; indexBufferIndex < rmv2Mesh.IndexList.Length; indexBufferIndex++)
            {
                rmv2Mesh.IndexList[indexBufferIndex] = (ushort)packedInputMesh.Indices[indexBufferIndex];
            }

            if (skeletonFile == null)
            {
                return MakeStatic(rmv2Mesh); // no skeleton, return static mesh
            }

            ProcessMeshWeights(rmv2Mesh, packedInputMesh, skeletonFile);

            SetWeightCounter(rmv2Mesh);

            return rmv2Mesh;
        }

        /// <summary>
        /// Sets the weight count to 4, also checks for invalid weights
        /// </summary>
        /// <param name="rmv2Mesh"></param>
        /// <exception cref="System.Exception"></exception>
        private static void SetWeightCounter(RmvMesh rmv2Mesh)
        {
            foreach (var vertex in rmv2Mesh.VertexList)
            {
                if (vertex.WeightCount == 0 || vertex.WeightCount > 4)
                {
                    throw new System.Exception("weight count incorrect!!");
                }

                vertex.WeightCount = 4;
            }
        }

        /// <summary>
        /// Makes the mesh into a static mesh "default", 
        /// TODO: the whole "weight-count" thing, should be changed?
        /// </summary>
        /// <param name="rmvMesh"></param>
        /// <returns></returns>
        private static RmvMesh MakeStatic(RmvMesh rmvMesh)
        {
            foreach (var v in rmvMesh.VertexList)
            {
                v.WeightCount = 0;
                v.BoneWeight = new float[0];
                v.BoneIndex = new byte[0];
            }

            return rmvMesh;
        }


        private static List<CommonVertex> MakeCommonVertices(VertexFormat vertexFormat, PackedMesh packedInputMesh, AnimationFile skeletonFile)
        {
            var vertices = new CommonVertex[packedInputMesh.Vertices.Count].ToList();

            for (var vertexIndex = 0; vertexIndex < vertices.Count; vertexIndex++)
            {
                vertices[vertexIndex] = MakeCommonPackedVertex(
                   packedInputMesh.Vertices[vertexIndex].Position,
                   packedInputMesh.Vertices[vertexIndex].Normal,
                   packedInputMesh.Vertices[vertexIndex].Uv,
                   packedInputMesh.Vertices[vertexIndex].Tangent,
                   packedInputMesh.Vertices[vertexIndex].BiNormal,
                   skeletonFile);
            }

            return vertices;
        }

        private static CommonVertex MakeCommonPackedVertex(
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
