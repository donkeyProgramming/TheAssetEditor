using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using Shared.GameFormats.RigidModel;
using SharpGLTF.Geometry.VertexTypes;
using SharpGLTF.Schema2;
using XNA = Microsoft.Xna.Framework;
using Xceed.Wpf.Toolkit.PropertyGrid.Editors;
using Editors.ImportExport.Exporting.Exporters.RmvToGltf.Helpers;
using Shared.GameFormats.RigidModel.Vertex;
using SharpGLTF.Geometry;
using System.ComponentModel.DataAnnotations;
using GameWorld.Core.Animation;
using GameWorld.Core.Rendering.Geometry;
using GameWorld.Core.Rendering.Materials.Serialization;
using GameWorld.Core.Rendering.Materials.Shaders;
using GameWorld.Core.Services;
using Shared.GameFormats.RigidModel.MaterialHeaders;
using SharpGLTF.Runtime;
using Shared.GameFormats.RigidModel.LodHeader;
using Shared;
using SharpDX.MediaFoundation;
using Editors.ImportExport.Common;
using Pfim;
using Shared.GameFormats.RigidModel.Types;

namespace Editors.ImportExport.Importing.Importers.GltfToRmv.Helper
{
    public class Rmv2FromGltfFIleConverter
    {
        private ModelRoot? _modelRoot;

        private void LoadGltfScene(GltfImporterSettings settings)
        {
            _modelRoot = ModelRoot.Load(settings.InputGltfFile);
        }

        public RmvFile Convert(GltfImporterSettings settings)
        {
            LoadGltfScene(settings);

            var rmv2file = new RmvFile();
            rmv2file.Header = new RmvFileHeader() { SkeletonName = "", Version = RmvVersionEnum.RMV2_V7, LodCount = 1 };
            rmv2file.LodHeaders = new RmvLodHeader[1];
            rmv2file.LodHeaders[0] = LodHeaderFactory.Create().CreateEmpty(RmvVersionEnum.RMV2_V7, 100.0f, 0, 0);
            rmv2file.LodHeaders[0].MeshCount = (uint)_modelRoot.LogicalMeshes.Count;


            rmv2file.ModelList = new RmvModel[1][];
            var tempModelList = new List<RmvModel>();


            foreach (var mesh in _modelRoot.LogicalMeshes)
            {
                var rmv2Mesh = ConvertToRmv2MeshByVertexColumns(mesh);
                var rmvModel = CreateRmvModel(ModelMaterialEnum.default_type, VertexFormat.Cinematic, rmv2Mesh, mesh.Name);
                tempModelList.Add(rmvModel);
            }

            rmv2file.ModelList[0] = tempModelList.ToArray();

            rmv2file.RecalculateOffsets();

            return rmv2file;
        }

        private RmvMesh ConvertToRmv2MeshByVertexColumns(SharpGLTF.Schema2.Mesh mesh)
        {
            var rmv2Mesh = new RmvMesh();

            var prim = mesh.Primitives.First();            
            var vertexBufferColumns = prim.GetVertexColumns();

            rmv2Mesh.VertexList = new CommonVertex[vertexBufferColumns.Positions.Count()];
            for (var vertexIndex = 0; vertexIndex < vertexBufferColumns.Positions.Count(); vertexIndex++)
            {             
                var vertexBuilder = vertexBufferColumns.GetVertex<VertexPositionNormalTangent, VertexTexture1, VertexJoints4>(vertexIndex);
                rmv2Mesh.VertexList[vertexIndex] = ConvertToRmvVertex(vertexBuilder);
            }
            
            rmv2Mesh.IndexList = prim.GetIndices().Select(i => (ushort)i).ToArray();

            return rmv2Mesh;
        }

        private RmvMesh ConvertToRmv2MeshByTriangles(SharpGLTF.Schema2.Mesh mesh)
        {
            var rmv2Mesh = new RmvMesh();

            var prim = mesh.Primitives.First();
            {
                var triangles = mesh.EvaluateTriangles<VertexPositionNormalTangent, VertexTexture1, VertexJoints4>();

                var tempVertexList = new List<CommonVertex>();
                var tempIndexList = new List<ushort>();
                foreach (var tri in triangles)
                {
                    tempVertexList.Add(ConvertToRmvVertex(tri.A));
                    tempVertexList.Add(ConvertToRmvVertex(tri.B));
                    tempVertexList.Add(ConvertToRmvVertex(tri.C));

                    // make an "uindexed" index buffer, 0,1,2,3,4,5,6....
                    tempIndexList.Add((ushort)tempIndexList.Count);
                    tempIndexList.Add((ushort)tempIndexList.Count);
                    tempIndexList.Add((ushort)tempIndexList.Count);
                }
                rmv2Mesh.VertexList = tempVertexList.ToArray();
                rmv2Mesh.IndexList = tempIndexList.ToArray();
            }

            return rmv2Mesh;
        }

        private static CommonVertex ConvertToRmvVertex(VertexBuilder<VertexPositionNormalTangent, VertexTexture1, VertexJoints4> vertexBuilder)
        {
            var rmv2Vertex = new CommonVertex();            

            rmv2Vertex.Position = new XNA.Vector4(-vertexBuilder.Geometry.Position.X, vertexBuilder.Geometry.Position.Y, vertexBuilder.Geometry.Position.Z, 1);
            rmv2Vertex.Uv = VecConv.GetXna(vertexBuilder.Material.TexCoord);
            rmv2Vertex.Normal = new XNA.Vector3(-vertexBuilder.Geometry.Normal.X, vertexBuilder.Geometry.Normal.Y, vertexBuilder.Geometry.Normal.Z);
            rmv2Vertex.Tangent = new XNA.Vector3(-vertexBuilder.Geometry.Tangent.X, vertexBuilder.Geometry.Tangent.Y, vertexBuilder.Geometry.Tangent.Z);
            rmv2Vertex.BiNormal = XNA.Vector3.Cross(rmv2Vertex.Normal, rmv2Vertex.Tangent) * vertexBuilder.Geometry.Tangent.W; // should produce th correct bitangent

            rmv2Vertex.WeightCount = vertexBuilder.Skinning.MaxBindings;
            rmv2Vertex.BoneIndex = new byte[rmv2Vertex.WeightCount];
            rmv2Vertex.BoneWeight = new float[rmv2Vertex.WeightCount];

            for (var j = 0; j < rmv2Vertex.WeightCount; j++)
            {
                rmv2Vertex.BoneIndex[j] = (byte)vertexBuilder.Skinning.Joints[j];
                rmv2Vertex.BoneWeight[j] = vertexBuilder.Skinning.Weights[j];
            }

            return rmv2Vertex;
        }
       
        RmvModel CreateRmvModel(ModelMaterialEnum rigidMaterial, VertexFormat vertexFormat, RmvMesh mesh, string modelName = "", GameSkeleton? skeleton = null, bool addBonesAsAttachmentPoints = false)
        {
            var materialHeader = new WeightedMaterial();
            materialHeader.BinaryVertexFormat = vertexFormat;
            var newModel = new RmvModel()
            {
                CommonHeader = RmvCommonHeader.CreateDefault(),
                Material = materialHeader,
                Mesh = mesh
            };

            newModel.Material.ModelName = modelName;

            UpdateBoundBox(newModel);

            if (addBonesAsAttachmentPoints && skeleton != null)
            {
                var boneNames = skeleton.BoneNames;
                var attachmentPoints = AttachmentPointHelper.CreateFromBoneList(boneNames);
                newModel.Material.EnrichDataBeforeSaving(attachmentPoints, -1);
            }

            return newModel;
        }

        void UpdateBoundBox(RmvModel newModel)
        {
            var points = new XNA.Vector3[newModel.Mesh.VertexList.Length];

            for (var i = 0; i < newModel.Mesh.VertexList.Length; i++)
            {
                points[i].X = newModel.Mesh.VertexList[i].Position.X;
                points[i].Y = newModel.Mesh.VertexList[i].Position.Y;
                points[i].Z = newModel.Mesh.VertexList[i].Position.Z;
            }

            newModel.UpdateBoundingBox(XNA.BoundingBox.CreateFromPoints(points));
            newModel.UpdateModelTypeFlag(newModel.Material.MaterialId);
        }
    }
}
