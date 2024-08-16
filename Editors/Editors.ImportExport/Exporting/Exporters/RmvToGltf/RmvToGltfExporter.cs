using Editors.ImportExport.Misc;
using Shared.Core.PackFiles.Models;
using Shared.GameFormats.RigidModel;
using Shared.Core.PackFiles;
using System.IO;
using SharpGLTF.Materials;
using System.Numerics;
using SharpGLTF.Schema2;
using Shared.GameFormats.Animation;
using MeshImportExport;
using System.Windows;
using Editors.ImportExport.Exporting.Exporters.DdsToNormalPng;
using Editors.ImportExport.Exporting.Exporters.DdsToPng;
using SharpGLTF.Geometry.VertexTypes;
using SharpGLTF.Geometry;
using System.Runtime.Serialization;
using System.Diagnostics.Eventing.Reader;
using Editors.ImportExport.Exporting.Exporters.DdsToMaterialPng;
using System.ComponentModel;
using Microsoft.Xna.Framework.Graphics;
using Shared.GameFormats.RigidModel.Vertex;

namespace Editors.ImportExport.Exporting.Exporters.RmvToGltf
{
    public record RmvToGltfExporterSettings(
        PackFile InputFile,
        string OutputPath,
        bool ExportTextures,
        bool ConvertMaterialTextureToBlender,
        bool ConvertNormalTextureToBlue,
        bool ExportAnimations
    );


    public class RmvToGltfExporter
    {
        private readonly PackFileService _packFileService;
        private readonly DdsToNormalPngExporter _exporterNormalBlue;
        private readonly DdsToPngExporter _ddsToPngExporter;
        private readonly DdsToMaterialPngExporter _exporterMaterial;

        public RmvToGltfExporter(PackFileService packFileSerivce, DdsToNormalPngExporter ddsToNormalPngExporter, DdsToPngExporter ddsToPngExporter, DdsToMaterialPngExporter ddsToMaterialPngExporter)
        {
            _packFileService = packFileSerivce;
            _exporterNormalBlue = ddsToNormalPngExporter;
            _ddsToPngExporter = ddsToPngExporter;
            _exporterMaterial = ddsToMaterialPngExporter;
        }

        internal ExportSupportEnum CanExportFile(PackFile file)
        {
            if (FileExtensionHelper.IsRmvFile(file.Name))
                return ExportSupportEnum.HighPriority;
            if(FileExtensionHelper.IsWsModelFile(file.Name))
                return ExportSupportEnum.HighPriority;
            return ExportSupportEnum.NotSupported;
        }

        public void Export(RmvToGltfExporterSettings settings)
        {
            var rmv2 = new ModelFactory().Load(settings.InputFile.DataSource.ReadData());
            var lodLevel = rmv2.ModelList.First();
            var hasSkeleton = (rmv2.Header.SkeletonName != "");
            var gltfSkeleton = new List<(Node, Matrix4x4)>();
            var model = ModelRoot.CreateModel();
            if (hasSkeleton)
            {
                gltfSkeleton = GenerateSkeleton(rmv2);
                model = SharedModel.Model;
            }
            else
            {
                SharedModel.SetModel(model);
            }
            List<Mesh> meshes = new List<Mesh>();
            foreach (var rmvMesh in lodLevel)
            {
                var gltfMaterial = new MaterialBuilder();
                if (settings.ExportTextures == true)
                {
                    gltfMaterial = _ddsToPngExporter.GenerateMaterial(settings, rmvMesh);
                }
                else
                {
                    gltfMaterial = _ddsToPngExporter.BuildFakeMaterialPerMesh(rmvMesh, settings.InputFile);
                }
                var mesh = model.CreateMesh(GenerateMesh(rmvMesh, gltfMaterial, hasSkeleton));
                meshes.Add(mesh);
            }
            BuildGltf(meshes, gltfSkeleton, settings);
        }

        public void BuildGltf(List<Mesh> meshes, List<(Node, Matrix4x4)> gltfSkeleton, RmvToGltfExporterSettings settings)
        {
            var model = SharedModel.Model;
            var scene = model.UseScene("default");
            foreach (var mesh in meshes)
            {
                if (gltfSkeleton.Count != 0)
                {
                    scene.CreateNode(mesh.Name).WithSkinnedMesh(mesh, gltfSkeleton.ToArray());
                }
                else
                {
                    scene.CreateNode(mesh.Name).WithMesh(mesh);
                }
            }
            model.SaveGLTF(settings.OutputPath + Path.GetFileNameWithoutExtension(settings.InputFile.Name) + ".gltf");
        }

        internal List<(Node, Matrix4x4)> GenerateSkeleton(RmvFile rmv2)
        {
            var skeletonName = rmv2.Header.SkeletonName + ".anim";
            var skeletonSearchList = _packFileService.SearchForFile(skeletonName);
            var skeletonPath = _packFileService.GetFullPath(_packFileService.FindFile(skeletonSearchList[0]));
            var skeletonPackFile = _packFileService.FindFile(skeletonPath);

            var invMatrixName = rmv2.Header.SkeletonName + ".bone_inv_trans_mats";
            var invMatrixSearchList = _packFileService.SearchForFile(invMatrixName);
            var invMatrixFilePath = _packFileService.GetFullPath(_packFileService.FindFile(invMatrixSearchList[0]));
            var invMatrixPackFile = _packFileService.FindFile(invMatrixFilePath);

            var animFile = AnimationFile.Create(skeletonPackFile);
            var invMatrixFile = AnimInvMatrixFile.Create(invMatrixPackFile.DataSource.ReadDataAsChunk());
            var gltfSkeletonBindings = SkeletonExporter.CreateSkeletonFromGameSkeleton(animFile, invMatrixFile);

            return gltfSkeletonBindings;
        }

        public static MeshBuilder<VertexPositionNormalTangent, VertexTexture1, VertexJoints4> GenerateMesh(RmvModel rmvMesh, MaterialBuilder material, bool hasSkeleton)
        {
            var mesh = new MeshBuilder<VertexPositionNormalTangent, VertexTexture1, VertexJoints4>(rmvMesh.Material.ModelName);
            if (hasSkeleton)
            {
                mesh.VertexPreprocessor.SetValidationPreprocessors();
            }

            var prim = mesh.UsePrimitive(material);

            var vertexList = new List<VertexBuilder<VertexPositionNormalTangent, VertexTexture1, VertexJoints4>>();
            foreach (var vertex in rmvMesh.Mesh.VertexList)
            {
                var glTfvertex = new VertexBuilder<VertexPositionNormalTangent, VertexTexture1, VertexJoints4>();
                glTfvertex.Geometry.Position = new Vector3(vertex.Position.X, vertex.Position.Y, vertex.Position.Z);
                glTfvertex.Geometry.Normal = new Vector3(vertex.Normal.X, vertex.Normal.Y, vertex.Normal.Z);
                glTfvertex.Geometry.Tangent = new Vector4(vertex.Tangent.X, vertex.Tangent.Y, vertex.Tangent.Z, 1);
                glTfvertex.Material.TexCoord = new Vector2(vertex.Uv.X, vertex.Uv.Y);

                if (hasSkeleton)
                {
                    glTfvertex = BonesAndWeight(vertex, glTfvertex);
                }
                else
                {
                    glTfvertex.Skinning.Weights = new Vector4(0, 1, 0, 0);
                    glTfvertex.Skinning.Joints = new Vector4(0, 1, 0, 0);
                }
                vertexList.Add(glTfvertex);
            }

            var triangleCount = rmvMesh.Mesh.IndexList.Length;
            for (var i = 0; i < triangleCount; i += 3)
            {
                var i0 = rmvMesh.Mesh.IndexList[i + 0];
                var i1 = rmvMesh.Mesh.IndexList[i + 1];
                var i2 = rmvMesh.Mesh.IndexList[i + 2];

                prim.AddTriangle(vertexList[i0], vertexList[i1], vertexList[i2]);
            }
            return mesh;
        }


        internal static VertexBuilder<VertexPositionNormalTangent, VertexTexture1, VertexJoints4> BonesAndWeight(CommonVertex vertex,
            VertexBuilder<VertexPositionNormalTangent, VertexTexture1, VertexJoints4> glTfvertex)
        {
            if (vertex.WeightCount == 2)
            {
                if (vertex.BoneWeight[0] == 0)
                    vertex.BoneIndex[0] = 0;

                if (vertex.BoneWeight[1] == 0)
                    vertex.BoneIndex[1] = 0;

                var sum = vertex.BoneWeight[0] + vertex.BoneWeight[1];
                if (Math.Abs(sum - 1) > 4E-07)
                {

                    vertex.BoneWeight[0] += Math.Abs(sum - 1);
                    // Values are within specified tolerance of each other....
                }

                glTfvertex.Skinning.Weights = new Vector4(vertex.BoneWeight[0], vertex.BoneWeight[1], 0, 0);
                glTfvertex.Skinning.Joints = new Vector4(vertex.BoneIndex[0], vertex.BoneIndex[1], 0, 0);
            }
            else if (vertex.WeightCount == 4)
            {
                if (vertex.BoneWeight[0] == 0)
                    vertex.BoneIndex[0] = 0;

                if (vertex.BoneWeight[1] == 0)
                    vertex.BoneIndex[1] = 0;

                if (vertex.BoneWeight[2] == 0)
                    vertex.BoneIndex[2] = 0;

                if (vertex.BoneWeight[3] == 0)
                    vertex.BoneIndex[3] = 0;

                var sum = vertex.BoneWeight[0] + vertex.BoneWeight[1] + vertex.BoneWeight[2] + vertex.BoneWeight[3];
                if (Math.Abs(sum - 1) > 4E-07)
                {

                    vertex.BoneWeight[0] += Math.Abs(sum - 1);
                    // Values are within specified tolerance of each other....
                }

                glTfvertex.Skinning.Weights = new Vector4(vertex.BoneWeight[0], vertex.BoneWeight[1], vertex.BoneWeight[2], vertex.BoneWeight[3]);
                glTfvertex.Skinning.Joints = new Vector4(vertex.BoneIndex[0], vertex.BoneIndex[1], vertex.BoneIndex[2], vertex.BoneIndex[3]);
            }
            else
            {
                //throw new Exception("Woops");
                glTfvertex.Skinning.Weights = new Vector4(0, 1, 0, 0);
                glTfvertex.Skinning.Joints = new Vector4(0, 1, 0, 0);
            }
            return glTfvertex;
        }
    }
}
