using Microsoft.Xna.Framework;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using CommonControls.FileTypes.RigidModel.LodHeader;
using CommonControls.FileTypes.RigidModel.MaterialHeaders;
using CommonControls.FileTypes.RigidModel;
using CommonControls.FileTypes.RigidModel.Vertex;
using Assimp;
using CommonControls.FileTypes.Animation;
using System.Windows.Forms;
using Filetypes.ByteParsing;
using CommonControls.FileTypes.PackFiles.Models;
using CommonControls.Services;
using Serilog;
using CommonControls.Common;
using Assimp.Unmanaged;

namespace CommonControls.ModelImportExport
{
    public class AssimpImporter
    {
        ILogger _logger = Serilog.Log.ForContext<AssimpImporter>();

        private Assimp.Scene _assScene;
        private AnimationFile _skeletonFile;
        private PackFileService _packFileService;

        public AssimpImporter(PackFileService packFileService)
        {
            _packFileService = packFileService;
        }

        public void ImportScene(string fileName)
        {
            ImportAssimpScene(fileName);
        }

        private string GetSkeletonName()
        {
            string tempSkeletonString = "";

            var parent = _assScene.RootNode;

            SearchNodesRecursiveLocal(parent, ref tempSkeletonString);

            return tempSkeletonString;
        }

        void SearchNodesRecursiveLocal(Node parent, ref string skeletonString)
        {
            foreach (var node in parent.Children)
            {
                if (node.Name.Contains("skeleton"))
                    skeletonString = node.Name.Replace("skeleton//", "");

                if (skeletonString.Length > 0)
                    return;

                SearchNodesRecursiveLocal(node, ref skeletonString);
            }
        }

        private void LoadSkeletonFile()
        {
            var skeletonId = GetSkeletonName();
            var skeletonFolder = @"animations\skeletons\";
            var animExt = "anim";
            var fullPath = $"{skeletonFolder}{skeletonId}.{animExt}";

            var packFileSkeleton = _packFileService.FindFile(fullPath);

            if (packFileSkeleton == null)
            {
                _logger.Here().Warning($"Failed to Find skeleton '{fullPath}', it doesn't exist.");
                MessageBox.Show($"Couldn't find skeleton '{fullPath}' \rMake sure to Load All CA Packs before importing Rigged Models!\rOr add the appropiate skeleton to your project\r\rFile Will be imported as a non-rigged model.", "Skeleton Missing Warning");
                return;
            }

            var rawByteDataSkeleton = packFileSkeleton.DataSource.ReadData();
            _skeletonFile = AnimationFile.Create(new ByteChunk(rawByteDataSkeleton));
        }

        public RmvFile MakeRMV2File()
        {
            int lodCount = 4; // make 4 idential LODs for compatibility reasons

            RmvFile outputFile = new RmvFile()
            {
                Header = new RmvFileHeader()
                {
                    _fileType = Encoding.ASCII.GetBytes("RMV2"),
                    SkeletonName = _skeletonFile != null ? _skeletonFile.Header.SkeletonName : "",
                    Version = RmvVersionEnum.RMV2_V6,
                    LodCount = (uint)lodCount,
                },
                LodHeaders = new RmvLodHeader[lodCount],
            };

            for (int i = 0; i < lodCount; i++)
            {
                outputFile.LodHeaders[i] =
                    new Rmv2LodHeader_V6()
                    {
                        MeshCount = (uint)_assScene.MeshCount,
                        QualityLvl = 0,
                        LodCameraDistance = 0,
                    };
            }

            outputFile.ModelList = new RmvModel[lodCount][];

            for (int lodIndex = 0; lodIndex < lodCount; lodIndex++)
            {
                outputFile.ModelList[lodIndex] = new RmvModel[_assScene.MeshCount];

                for (int meshIndex = 0; meshIndex < _assScene.MeshCount; meshIndex++)
                {
                    outputFile.ModelList[lodIndex][meshIndex] = new RmvModel();
                    ref var cuurentMeshRef = ref outputFile.ModelList[lodIndex][meshIndex];

                    MaterialFactory materialFactory = MaterialFactory.Create();

                    cuurentMeshRef.Material = materialFactory.CreateMaterial(
                        outputFile.Header.Version,
                        (_skeletonFile != null) ? ModelMaterialEnum.weighted : ModelMaterialEnum.default_type,
                        (_skeletonFile != null) ? FileTypes.RigidModel.VertexFormat.Cinematic : FileTypes.RigidModel.VertexFormat.Static);

                    cuurentMeshRef.Mesh = MakeMeshIndexed(_assScene.Meshes[meshIndex]);

                    cuurentMeshRef.Material.ModelName = _assScene.Meshes[meshIndex].Name;

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

        private void ImportAssimpScene(string fileName)
        {
            using (var importer = new AssimpContext())
            {
                // -- left all the flags outcommented, 
                _assScene = importer.ImportFile(fileName,
                   //PostProcessSteps.FindInstances | // No effect + slow?
                   //PostProcessSteps.FindInvalidData |
                   //PostProcessSteps.FlipUVs |
                   //PostProcessSteps.FlipWindingOrder |
                   //PostProcessSteps.MakeLeftHanded | // Appears to just mess things up
                   //PostProcessSteps.JoinIdenticalVertices |
                   //PostProcessSteps.ImproveCacheLocality |
                   //PostProcessSteps.OptimizeMeshes |
                   //PostProcessSteps.OptimizeGraph | // Will eliminate helper nodes
                   //PostProcessSteps.RemoveRedundantMaterials |
                   PostProcessSteps.Triangulate |
                   PostProcessSteps.CalculateTangentSpace |
                   //PostProcessSteps.FixInFacingNormals |
                   PostProcessSteps.GlobalScale
                   );
            }

            LoadSkeletonFile();
        }

        /// <summary>
        /// Mesh is made unindexed, so it can be processed, for calculing tangent basis better
        /// </summary>
        private RmvMesh MakeMeshUnindexed(Assimp.Mesh assimpInputMesh)
        {
            var asFaces = assimpInputMesh.Faces;
            var asVertices = assimpInputMesh.Vertices;
            var asNormals = assimpInputMesh.Normals;
            var asTex = assimpInputMesh.TextureCoordinateChannels[0];
            var asBones = assimpInputMesh.Bones;

            RmvMesh unindexesMesh = new RmvMesh();
            unindexesMesh.IndexList = new ushort[assimpInputMesh.FaceCount * 3];
            unindexesMesh.VertexList = new CommonVertex[assimpInputMesh.FaceCount * 3];

            // get converte asssimp Mesh vertices to packed vertivces
            var originalVerticesPacked = MakePackedVertices(assimpInputMesh);

            int vertexIndex = 0;
            for (int assFaceIndex = 0; assFaceIndex < assimpInputMesh.FaceCount; assFaceIndex++, vertexIndex += 3) // traverse "per triangle"
            {
                var v0 = assimpInputMesh.Faces[assFaceIndex].Indices[0];
                var v1 = assimpInputMesh.Faces[assFaceIndex].Indices[1];
                var v2 = assimpInputMesh.Faces[assFaceIndex].Indices[2];

                // make vertices sequential
                unindexesMesh.VertexList[vertexIndex + 0] = originalVerticesPacked[v0];
                unindexesMesh.VertexList[vertexIndex + 1] = originalVerticesPacked[v1];
                unindexesMesh.VertexList[vertexIndex + 2] = originalVerticesPacked[v2];

                // make sequential indices (0,1,2,3,4...)vb
                unindexesMesh.IndexList[vertexIndex + 0] = (ushort)(vertexIndex + 0);
                unindexesMesh.IndexList[vertexIndex + 1] = (ushort)(vertexIndex + 1);
                unindexesMesh.IndexList[vertexIndex + 2] = (ushort)(vertexIndex + 2);
            }

            return unindexesMesh;
        }
        private RmvMesh MakeMeshIndexed(Assimp.Mesh assInputMesh)
        {
            var asFaces = assInputMesh.Faces;
            var asVertices = assInputMesh.Vertices;
            var asNormals = assInputMesh.Normals;
            var asTex = assInputMesh.TextureCoordinateChannels[0];
            var asBones = assInputMesh.Bones;

            RmvMesh unindexesMesh = new RmvMesh();
            unindexesMesh.IndexList = new ushort[assInputMesh.FaceCount * 3];
            unindexesMesh.VertexList = new CommonVertex[assInputMesh.FaceCount * 3];

            var originalVerticesPacked = MakePackedVertices(assInputMesh);

            unindexesMesh.VertexList = originalVerticesPacked.ToArray();

            int vertexIndex = 0;
            for (int assFaceIndex = 0; assFaceIndex < assInputMesh.FaceCount; assFaceIndex++, vertexIndex += 3)
            {
                var v0 = assInputMesh.Faces[assFaceIndex].Indices[0];
                var v1 = assInputMesh.Faces[assFaceIndex].Indices[1];
                var v2 = assInputMesh.Faces[assFaceIndex].Indices[2];

                unindexesMesh.IndexList[assFaceIndex * 3 + 0] = (ushort)v0;
                unindexesMesh.IndexList[assFaceIndex * 3 + 1] = (ushort)v1;
                unindexesMesh.IndexList[assFaceIndex * 3 + 2] = (ushort)v2;
            }

            return unindexesMesh;
        }

        /// <summary>
        ///  Make packed vertices from the assimp meshs attributes
        /// </summary>        
        private List<CommonVertex> MakePackedVertices(Assimp.Mesh assInputMesh)
        {
            List<CommonVertex> vertices = new CommonVertex[assInputMesh.VertexCount].ToList();

            for (int vertexIndex = 0; vertexIndex < vertices.Count; vertexIndex++)
            {
                vertices[vertexIndex] = MakePackedVertex(assInputMesh.Vertices[vertexIndex], assInputMesh.Normals[vertexIndex], assInputMesh.TextureCoordinateChannels[0][vertexIndex], assInputMesh.Tangents[vertexIndex], assInputMesh.BiTangents[vertexIndex]);
            }

            if (_skeletonFile != null)
            {
                ProcessWeights(vertices, assInputMesh);
            }

            return vertices;
        }

        /// <summary>
        ///  Make packed vertex from position, normal and UV
        /// </summary>        
        private CommonVertex MakePackedVertex(
            Assimp.Vector3D position,
            Assimp.Vector3D normal,
            Assimp.Vector3D textureCoords,
            Assimp.Vector3D tangent,
            Assimp.Vector3D bitangent)
        {
            // -- Attempt at getting unit values from the "native"
            //Scene.Metadata.TryGetValue("UnitScaleFactor", out var value);
            //float scaleFactor = 1 / (float)value.DataAs<double>();

            // -- Assimp outpus unnormalized normals and tangents.
            var normalizedNormal = new Vector3(-normal.X, normal.Y, normal.Z);
            normalizedNormal.Normalize();

            var normalizedTangent = new Vector3(-tangent.X, tangent.Y, tangent.Z);
            normalizedTangent.Normalize();

            var normalizedBitangent = new Vector3(-bitangent.X, bitangent.Y, bitangent.Z);
            normalizedBitangent.Normalize();

            var vertex = new CommonVertex()
            {
                Position = new Vector4(-position.X, position.Y, position.Z, 0),// * scaleFactor,
                Normal = normalizedNormal,
                Uv = new Vector2(textureCoords.X, -textureCoords.Y),
                //Uv = new Vector2(textureCoords.X, textureCoords.Y),
                Tangent = normalizedTangent,
                BiNormal = normalizedBitangent,
                WeightCount = 0,
            };

            var numWeight = 0;
            if (_skeletonFile != null)            
                numWeight = 4;            

            vertex.BoneIndex = new byte[numWeight];
            vertex.BoneWeight = new float[numWeight];

            return vertex;
        }

        private void ProcessWeights(List<CommonVertex> vertices, Assimp.Mesh assMesh)
        {
            foreach (var assBoneWeightInfo in assMesh.Bones)
            {
                var boneIndex = this._skeletonFile.GetIdFromBoneName(assBoneWeightInfo.Name);
                if (boneIndex == -1)
                {
                    throw new Exception("Weight Processing Error: Bone: '" + assBoneWeightInfo.Name + "' not found in Skeleton ANIM file");
                }

                foreach (var vertexWeight in assBoneWeightInfo.VertexWeights)
                {
                    if (vertexWeight.VertexID >= vertices.Count)
                    {
                        throw new Exception("Weight Processing Error: Vertex ID out of bounds");
                    }

                    VertexWeightProcessor.AddWeightToVertex(vertices[vertexWeight.VertexID], boneIndex, vertexWeight.Weight);
                }
            }

            // -- process/check the added vertex weight SETS 
            foreach (var vertex in vertices)
            {
                VertexWeightProcessor.CheckVertexWeights(vertex);
                VertexWeightProcessor.SortVertexWeights(vertex);
                VertexWeightProcessor.NormalizeVertexWeights(vertex);
            }
        }
    }
};
