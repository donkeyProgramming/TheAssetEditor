using Shared.GameFormats.RigidModel.LodHeader;
using Shared.GameFormats.RigidModel;
using SharpGLTF.Schema2;
using Shared.GameFormats.RigidModel.Vertex;
using SharpGLTF.Geometry.VertexTypes;
using SharpGLTF.Geometry;
using Shared.GameFormats.RigidModel.MaterialHeaders;
using XNA = Microsoft.Xna.Framework;
using Shared.GameFormats.RigidModel.Types;
using System.Windows.Controls;
using System.CodeDom;
using System.Windows.Forms.VisualStyles;
using System.Drawing.Printing;
using System;
using System.Text;
using Editors.ImportExport.Common;
using Shared.GameFormats.Animation;
using static Shared.GameFormats.Animation.AnimationFile;

namespace Editors.ImportExport.Importing.Importers.GltfToRmv.Helper
{
    /// <summary>
    /// Builds RmvMesh from Gltf asset (input: modelRoot)
    /// </summary>
    public class RmvMeshBuilder
    {
        public static RmvFile Build(GltfImporterSettings settings, ModelRoot modelRoot, AnimationFile? animSkeletonFile, string SkeletonName)
        {
            if (modelRoot == null)
                throw new ArgumentNullException(nameof(modelRoot), "Invalid Scene: ModelRoot can't be null");

            if (modelRoot.LogicalNodes == null)
                throw new ArgumentNullException(nameof(modelRoot), "Invalid Scene: root.LogicalNodes can't be null");

            if (!modelRoot.LogicalNodes.Any())
                throw new Exception("Invalid Scene: no (logical) nodes");

            List<string> boneNames = new List<string>();
            var test = boneNames?.Count;

            const int lodCount = 1; // All meshes go in LOD 0, LODs are sorted later

            var rmv2File = new RmvFile()
            {
                Header = new RmvFileHeader()
                {
                    _fileType = Encoding.ASCII.GetBytes("RMV2"),
                    SkeletonName = SkeletonName, // TODO: Store + get skeleton name from gltf
                    Version = RmvVersionEnum.RMV2_V7,
                    LodCount = lodCount
                },
                ModelList = new RmvModel[lodCount][],
            };

            rmv2File.LodHeaders = new RmvLodHeader[1];
            rmv2File.LodHeaders[0] = LodHeaderFactory.Create().CreateEmpty(RmvVersionEnum.RMV2_V7, 100.0f, 0, 0);
            rmv2File.LodHeaders[0].MeshCount = (uint)modelRoot.LogicalMeshes.Count;

            var modelList = new List<RmvModel>();


            foreach (var mesh in modelRoot.LogicalMeshes)
            {
                var rmv2Mesh = GenerateRmvMesh(mesh, modelRoot, animSkeletonFile);
                var rmvModel = CreateRmvModel(rmv2Mesh, mesh.Name,animSkeletonFile);
                modelList.Add(rmvModel);
            }

            rmv2File.ModelList[0] = modelList.ToArray();
            rmv2File.RecalculateOffsets();

            return rmv2File;
        }

        private static RmvMesh GenerateRmvMesh(SharpGLTF.Schema2.Mesh mesh, ModelRoot modelRoot, AnimationFile? animSkeletonFile)
        {
            if (mesh == null)
                throw new ArgumentNullException(nameof(mesh), "Invalid Mesh: Mesh can't be null");

            if (mesh.Primitives == null || !mesh.Primitives.Any())
                throw new Exception($"Invalid Mesh: No Primitives found in mesh. Primitives.Count = {mesh.Primitives?.Count}");

            var primitive = mesh.Primitives.First();

            if (primitive == null)
                throw new Exception("Invalid Mesh: primitive[0] can't be null ");

            var vertexBufferColumns = primitive.GetVertexColumns();

            if (vertexBufferColumns == null)
                throw new ArgumentNullException(nameof(vertexBufferColumns), "Invalid Mesh: value cannot be null ");

            if (vertexBufferColumns.Positions == null || !vertexBufferColumns.Positions.Any())
                throw new Exception($"Invalid Mesh: No vertex data. Positions.Count = {vertexBufferColumns.Positions?.Count}");

            if (vertexBufferColumns.Positions.Count() > ushort.MaxValue + 1)
                throw new Exception("Unsupported Mesh (Vertex count too high): RMV2 only supports 65536 vertices per mesh");
    

            var rmv2Mesh = new RmvMesh();
            rmv2Mesh.VertexList = new CommonVertex[vertexBufferColumns.Positions.Count()];
            for (var vertexIndex = 0; vertexIndex < vertexBufferColumns.Positions.Count(); vertexIndex++)
            {
                var vertexBuilder = vertexBufferColumns.GetVertex<VertexPositionNormalTangent, VertexTexture1, VertexJoints4>(vertexIndex);
                rmv2Mesh.VertexList[vertexIndex] = ConvertToRmvVertex(vertexBuilder, modelRoot, animSkeletonFile);
            }

            var indices = primitive.GetIndices();
            rmv2Mesh.IndexList = new ushort[indices.Count()];
            for (int i = 0; i < indices.Count(); i += 3) // reverse wind order, as we "mirrored" t
            {
                rmv2Mesh.IndexList[i + 0] = (ushort)indices[i + 0];
                rmv2Mesh.IndexList[i + 2] = (ushort)indices[i + 1];
                rmv2Mesh.IndexList[i + 1] = (ushort)indices[i + 2];
            }

            // recæculate tangents and bitangents
            TangentBasisCalculator.CalculateForRmv2Mesh(rmv2Mesh);

            return rmv2Mesh;
        }

        private static CommonVertex ConvertToRmvVertex(VertexBuilder<VertexPositionNormalTangent, VertexTexture1, VertexJoints4> vertexBuilder, ModelRoot modelRoot, AnimationFile animSkeletonFile)
        {
            var rmv2Vertex = new CommonVertex();

            rmv2Vertex.Position = new XNA.Vector4(-vertexBuilder.Geometry.Position.X, vertexBuilder.Geometry.Position.Y, vertexBuilder.Geometry.Position.Z, 1);
            rmv2Vertex.Uv = VecConv.GetXna(vertexBuilder.Material.TexCoord);
            rmv2Vertex.Normal = new XNA.Vector3(-vertexBuilder.Geometry.Normal.X, vertexBuilder.Geometry.Normal.Y, vertexBuilder.Geometry.Normal.Z);
            rmv2Vertex.Tangent = new XNA.Vector3(-vertexBuilder.Geometry.Tangent.X, vertexBuilder.Geometry.Tangent.Y, vertexBuilder.Geometry.Tangent.Z);
            rmv2Vertex.BiNormal = XNA.Vector3.Cross(rmv2Vertex.Normal, rmv2Vertex.Tangent) * vertexBuilder.Geometry.Tangent.W; // should produce th correct bitangent
                      
            if (animSkeletonFile == null)
            {
                rmv2Vertex.WeightCount = 0;
                
                return rmv2Vertex;
            }

            // TODO: check skeleton == null 
            rmv2Vertex.WeightCount = vertexBuilder.Skinning.MaxBindings;
            rmv2Vertex.BoneIndex = new byte[rmv2Vertex.WeightCount];
            rmv2Vertex.BoneWeight = new float[rmv2Vertex.WeightCount];

            // TODO: check skeleton == null 
            for (var bindindIndex = 0; bindindIndex < rmv2Vertex.WeightCount; bindindIndex++)
            {
                var boneTableIndex = GetMappedBoneTableIndex(vertexBuilder, modelRoot, animSkeletonFile, bindindIndex);

                rmv2Vertex.BoneIndex[bindindIndex] = (byte)boneTableIndex;
                rmv2Vertex.BoneWeight[bindindIndex] = vertexBuilder.Skinning.Weights[bindindIndex];
            }

            return rmv2Vertex;
        }

        /// <summary>
        /// Maps the gltf vertex joint index to th CA AnimationFile bone index
        /// </summary>        
        private static int GetMappedBoneTableIndex(VertexBuilder<VertexPositionNormalTangent, VertexTexture1, VertexJoints4> vertexBuilder, ModelRoot modelRoot, AnimationFile animSkeletonFile, int bindingIndex)
        {
            var binding = vertexBuilder.Skinning.GetBinding(bindingIndex); // Get binding

            if (modelRoot.LogicalSkins == null || !modelRoot.LogicalSkins.Any())
                throw new Exception($"No skins in scene. modelRoot.LogicalSkins = {modelRoot?.LogicalSkins}");

            var joint = modelRoot.LogicalSkins[0].GetJoint(binding.Index); // get skin joint node

            // use bone name, to obtain index to AnimationFile bone index
            var boneTableIndex = Array.FindIndex<BoneInfo>(animSkeletonFile.Bones, x => x.Name == joint.Joint.Name);
            return boneTableIndex;
        }

        private static RmvModel CreateRmvModel(RmvMesh rmv2Mesh, string modelName, AnimationFile? animSkeletonFile,  bool addBonesAsAttachmentPoints = false)
        {
            var materialHeader = new WeightedMaterial();

            MeshWeightValidator.Validate(rmv2Mesh);
            if (animSkeletonFile != null)
            {
                materialHeader.BinaryVertexFormat = VertexFormat.Cinematic;
                materialHeader.MaterialId = ModelMaterialEnum.weighted;
            }
            else
            {
                materialHeader.BinaryVertexFormat = VertexFormat.Static;
                materialHeader.MaterialId = ModelMaterialEnum.default_type;
            }

            var newModel = new RmvModel()
            {
                CommonHeader = RmvCommonHeader.CreateDefault(),
                Material = materialHeader,
                Mesh = rmv2Mesh
            };

            newModel.Material.ModelName = modelName;

            CalculateBoundBox(newModel);

            if (addBonesAsAttachmentPoints && animSkeletonFile != null)
            {
                var boneNames = animSkeletonFile.Bones.Select(x => x.Name).ToList();
                var attachmentPoints = AttachmentPointHelper.CreateFromBoneList(boneNames);
                newModel.Material.EnrichDataBeforeSaving(attachmentPoints, -1);
            }

            return newModel;
        }

        private static void CalculateBoundBox(RmvModel newModel)
        {
            var points = new XNA.Vector3[newModel.Mesh.VertexList.Length];

            for (var i = 0; i < newModel.Mesh.VertexList.Length; i++)
            {
                points[i].X = newModel.Mesh.VertexList[i].Position.X;
                points[i].Y = newModel.Mesh.VertexList[i].Position.Y;
                points[i].Z = newModel.Mesh.VertexList[i].Position.Z;
            }

            var testPoints = newModel.Mesh.VertexList.Select(item => item.Position).ToList();

            newModel.UpdateBoundingBox(XNA.BoundingBox.CreateFromPoints(points));
        }
    }
}
