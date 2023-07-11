using CommonControls.FileTypes.RigidModel.LodHeader;
using CommonControls.FileTypes.RigidModel.MaterialHeaders;
using CommonControls.FileTypes.RigidModel;
using System.IO;
using System.Collections.Generic;
using System.Text;
using CommonControls.FileTypes.RigidModel.Vertex;
using System.Linq;
using CommonControls.FileTypes.PackFiles.Models;
using CommonControls.Services;
using Microsoft.Xna.Framework;
using CommonControls.ModelFiles.Mesh;
using CommonControls.ModelFiles.Mesh.Native;
using SharpDX.Direct3D9;

namespace CommonControls.ModelFiles.FBX
{
    public class MeshFileHelper
    {
        /// <summary>
        /// Make Rmv2File from PackedMeshes and and skeleton name
        /// </summary>        
        public static RmvFile MakeRMV2File(List<Mesh.PackedMesh> packedMeshes, string skeletonName)
        {
            int lodCount = 4; // make 4 idential LODs for compatibility reasons

            RmvFile outputFile = new RmvFile()
            {
                Header = new RmvFileHeader()
                {
                    _fileType = Encoding.ASCII.GetBytes("RMV2"),
                    SkeletonName = skeletonName,
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
                    outputFile.ModelList[lodIndex][meshIndex] = new RmvModel();
                    ref var cuurentMeshRef = ref outputFile.ModelList[lodIndex][meshIndex];

                    MaterialFactory materialFactory = MaterialFactory.Create();

                    cuurentMeshRef.Material = materialFactory.CreateMaterial(
                        outputFile.Header.Version,
                        (skeletonName != "") ? ModelMaterialEnum.weighted : ModelMaterialEnum.default_type,
                        (skeletonName != "") ? FileTypes.RigidModel.VertexFormat.Cinematic : FileTypes.RigidModel.VertexFormat.Static);



                    cuurentMeshRef.Mesh = ConvertPackedMeshtoRmvMesh(cuurentMeshRef.Material.BinaryVertexFormat, packedMeshes[meshIndex], skeletonName);

                    cuurentMeshRef.Material.ModelName = packedMeshes[meshIndex].Name;

                    cuurentMeshRef.Material.SetTexture(FileTypes.RigidModel.Types.TextureType.BaseColour, @"commontextures\default_base_colour.dds");
                    cuurentMeshRef.Material.SetTexture(FileTypes.RigidModel.Types.TextureType.Normal, @"commontextures\default_normal.dds");
                    cuurentMeshRef.Material.SetTexture(FileTypes.RigidModel.Types.TextureType.MaterialMap, @"commontextures\default_material_mat.dds");

                    cuurentMeshRef.Material.SetTexture(FileTypes.RigidModel.Types.TextureType.Diffuse, @"commontextures\default_metal_material_map.dds");
                    cuurentMeshRef.Material.SetTexture(FileTypes.RigidModel.Types.TextureType.Specular, @"commontextures\default_metal_material_map.dds");
                    cuurentMeshRef.Material.SetTexture(FileTypes.RigidModel.Types.TextureType.Gloss, @"commontextures\default_metal_material_map.dds");
                };
            }

            outputFile.UpdateOffsets(); // refresh size/offsdet fields in 

            return outputFile;
        }

        /// <summary>
        /// Imports an FBX/OBJ/etx model using the FBX SDK
        /// </summary>
        /// <param name="pfs">Pack file service</param>
        /// <param name="container">Pack Container of pack folder</param>
        /// <param name="parentPackPath">Pack Folder</param>
        /// <param name="diskFilePath">Input model disk file</param>
        /// <param name="diskSkeletonFile">Optional skeleton file, use instead skeleton namm-scene-lookup</param>
        public static SceneContainer Import3dModelDiskFileToPack(PackFileService pfs, PackFileContainer contaipfsner, string parentPackPath, string diskFilePath, string diskSkeletonFile = "")
        {
            string skeletonName = "";
            var sceneContainer = SceneImorterService.CreateSceneFromFBX(diskFilePath, pfs, out skeletonName);

            var rmv2FileName = $"{System.IO.Path.GetFileNameWithoutExtension(diskFilePath)}.rigid_model_v2";
            MeshFileHelper.AddRmv2ToPackFIle(pfs, contaipfsner, parentPackPath, rmv2FileName, skeletonName, sceneContainer);
            return sceneContainer;
        }
        public static string GetOutFileName(string diskFilePath)
        {
            var fileNameNoExt = Path.GetFileNameWithoutExtension(diskFilePath);
            var outExtension = ".rigid_model_v2";
            var outFileName = fileNameNoExt + outExtension;
            return outFileName;
        }

        public static void AddRmv2ToPackFIle(PackFileService pfs, PackFileContainer container, string parentPackPath, string outFileName, string skeletonName, SceneContainer scene)
        {
            
            var rmv2File = MakeRMV2File(scene.Meshes, skeletonName); ;
            var factory = ModelFactory.Create();
            var buffer = factory.Save(rmv2File);

            var packFile = new PackFile(outFileName, new MemorySource(buffer));
            pfs.AddFileToPack(container, parentPackPath, packFile);
        }

        /// <summary>
        ///  Copy the data of "PackedMesh" into an "RmvMesh"
        /// </summary>
        private static RmvMesh ConvertPackedMeshtoRmvMesh(FileTypes.RigidModel.VertexFormat vertexFormat, PackedMesh packedInputMesh, string skeletonName)
        {
            RmvMesh unindexesMesh = new RmvMesh();
            unindexesMesh.IndexList = new ushort[packedInputMesh.Indices.Count];
            unindexesMesh.VertexList = new CommonVertex[packedInputMesh.Vertices.Count];

            var originalVerticesPacked = MakePackedVertices(vertexFormat, packedInputMesh, skeletonName);
                       

            unindexesMesh.VertexList = originalVerticesPacked.ToArray();
            unindexesMesh.IndexList = packedInputMesh.Indices.ToArray();

            return unindexesMesh;
        }
        
        /// <summary>
        /// Make list of "CommonVertex" from the vertices in a "PackedMesh"
        /// </summary>        
        private static List<CommonVertex> MakePackedVertices(FileTypes.RigidModel.VertexFormat vertexFormat, PackedMesh packedInputMesh, string skeletonName)
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
                    skeletonName);
                
                MakeVertexWeights(vertexFormat, packedInputMesh.Vertices[vertexIndex], vertices[vertexIndex]);                
            }

            return vertices;
        }

        /// <summary>
        /// Copies the vertex influences from a "PackedCommonVertex" to a "CommonVertex"
        /// </summary>        
        private static void MakeVertexWeights(FileTypes.RigidModel.VertexFormat vertexFormat, PackedCommonVertex packedInputVertex, CommonVertex outVertex)
        {
            if (vertexFormat == FileTypes.RigidModel.VertexFormat.Static)
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
            
            VertexWeightProcessor.SortVertexWeights(outVertex);
            VertexWeightProcessor.NormalizeVertexWeights(outVertex);
        }

        /// <summary>
        /// Makes a packed vertex (vertex that contains pos+normal+uv+ect attribytes, as opposed the an actual "math vertex" that is just position)
        /// </summary>        
        private static CommonVertex MakePackedVertex(
            XMFLOAT4 position,
            XMFLOAT3 normal,
            XMFLOAT2 textureCoords,
            XMFLOAT3 tangent,
            XMFLOAT3 bitangent,
            string skeletonName)
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

            if (skeletonName != "")
                vertex.WeightCount = 4;

            vertex.BoneIndex = new byte[vertex.WeightCount];
            vertex.BoneWeight = new float[vertex.WeightCount];

            return vertex;
        }
    }
}
