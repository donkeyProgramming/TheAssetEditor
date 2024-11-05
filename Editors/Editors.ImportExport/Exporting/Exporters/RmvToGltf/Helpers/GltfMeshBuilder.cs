using System.Numerics;
using Editors.ImportExport.Common;
using Editors.ImportExport.Exporting.Exporters.DdsToMaterialPng;
using Editors.ImportExport.Exporting.Exporters.DdsToNormalPng;
using Editors.ImportExport.Exporting.Exporters.DdsToPng;
using Shared.Core.PackFiles;
using Shared.GameFormats.RigidModel;
using Shared.GameFormats.RigidModel.Types;
using Shared.GameFormats.RigidModel.Vertex;
using SharpGLTF.Geometry;
using SharpGLTF.Geometry.VertexTypes;
using SharpGLTF.Materials;
using AlphaMode = SharpGLTF.Materials.AlphaMode;

namespace Editors.ImportExport.Exporting.Exporters.RmvToGltf.Helpers
{
    public record MaterialBuilderTextureInput(string Path, TextureType Type);

    public class GltfMeshBuilder
    {

        private readonly PackFileService _packFileService;
        private readonly DdsToNormalPngExporter _ddsToNormalPngExporter;
        private readonly DdsToPngExporter _ddsToPngExporter;
        private readonly DdsToMaterialPngExporter _ddsToMaterialPngExporter;

        public GltfMeshBuilder(PackFileService packFileSerivce, DdsToNormalPngExporter ddsToNormalPngExporter, DdsToPngExporter ddsToPngExporter, DdsToMaterialPngExporter ddsToMaterialPngExporter)
        {
            _packFileService = packFileSerivce;
            _ddsToNormalPngExporter = ddsToNormalPngExporter;
            _ddsToPngExporter = ddsToPngExporter;
            _ddsToMaterialPngExporter = ddsToMaterialPngExporter;
        }


        public List<IMeshBuilder<MaterialBuilder>> Build(RmvFile rmv2, RmvToGltfExporterSettings settings, bool doMirror)
        {
            var lodLevel = rmv2.ModelList.First();
            var hasSkeleton = string.IsNullOrWhiteSpace(rmv2.Header.SkeletonName) == false;

            var meshes = new List<IMeshBuilder<MaterialBuilder>>();
            foreach (var rmvMesh in lodLevel)
            {
                var textures = ExtractTextures(rmvMesh);
                var gltfMaterial = Create(settings, rmvMesh.Material.ModelName + "_Material", textures);
                var gltfMesh = GenerateMesh(rmvMesh, gltfMaterial, hasSkeleton, doMirror);
                meshes.Add(gltfMesh);
            }
            return meshes;
        }

        MeshBuilder<VertexPositionNormalTangent, VertexTexture1, VertexJoints4> GenerateMesh(RmvModel rmvMesh, MaterialBuilder material, bool hasSkeleton, bool doMirror)
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

                glTfvertex.Geometry.Position = VecConv.GetSys(GlobalSceneTransforms.FlipVector(VecConv.GetXna(glTfvertex.Geometry.Position), doMirror));
                glTfvertex.Geometry.Normal = VecConv.GetSys(GlobalSceneTransforms.FlipVector(VecConv.GetXna(glTfvertex.Geometry.Normal), doMirror));
                glTfvertex.Geometry.Tangent = VecConv.GetSys(GlobalSceneTransforms.FlipVector(VecConv.GetXna(glTfvertex.Geometry.Tangent), doMirror));

                if (hasSkeleton)
                {
                    SetVertexInfluences(vertex, glTfvertex);
                }
                else
                {
                    glTfvertex.Skinning.SetBindings((0, 1));
                }
                vertexList.Add(glTfvertex);
            }

            var triangleCount = rmvMesh.Mesh.IndexList.Length;
            for (var i = 0; i < triangleCount; i += 3)
            {

                ushort i0, i1, i2;
                if (doMirror) // if mirrored, flip the winding order
                {
                    i0 = rmvMesh.Mesh.IndexList[i + 0];
                    i1 = rmvMesh.Mesh.IndexList[i + 2];
                    i2 = rmvMesh.Mesh.IndexList[i + 1];
                }
                else
                {
                    i0 = rmvMesh.Mesh.IndexList[i + 0];
                    i1 = rmvMesh.Mesh.IndexList[i + 1];
                    i2 = rmvMesh.Mesh.IndexList[i + 2];
                }

                prim.AddTriangle(vertexList[i0], vertexList[i1], vertexList[i2]);
            }
            return mesh;
        }


        void SetVertexInfluences(CommonVertex vertex, VertexBuilder<VertexPositionNormalTangent, VertexTexture1, VertexJoints4> glTfvertex)
        {
            if (vertex.WeightCount == 2)
            {
                var rigging = new (int, float)[2] {
                        (vertex.BoneIndex[0], vertex.BoneWeight[0]),
                        (vertex.BoneIndex[1], 1.0f - vertex.BoneWeight[0])
                        };

                glTfvertex.Skinning.SetBindings(rigging);

            }
            else if (vertex.WeightCount == 4)
            {
                var rigging = new (int, float)[4] {
                        (vertex.BoneIndex[0], vertex.BoneWeight[0]),
                        (vertex.BoneIndex[1], vertex.BoneWeight[1]),
                        (vertex.BoneIndex[2], vertex.BoneWeight[2]),
                        (vertex.BoneIndex[3], 1.0f - (vertex.BoneWeight[0] + vertex.BoneWeight[1] + vertex.BoneWeight[2]))
                        };

                glTfvertex.Skinning.SetBindings(rigging);
            }
        }

        List<MaterialBuilderTextureInput> ExtractTextures(RmvModel model)
        {
            var textures = model.Material.GetAllTextures();
            var output = textures.Select(x => new MaterialBuilderTextureInput(x.Path, x.TexureType)).ToList();
            return output;
        }

        MaterialBuilder Create(RmvToGltfExporterSettings settings, string materialName, List<MaterialBuilderTextureInput> textures)
        {
            var material = new MaterialBuilder(materialName)
                  .WithDoubleSide(true)
                  .WithMetallicRoughness()
                  .WithAlpha(AlphaMode.MASK);

            var normalMapTexture = textures.FirstOrDefault(t => t.Type == TextureType.Normal);
            if (normalMapTexture.Path != null)
            {
                var systemPath = _ddsToNormalPngExporter.Export(normalMapTexture.Path, settings.OutputPath, settings.ConvertNormalTextureToBlue);
                material.WithChannelImage(KnownChannel.Normal, systemPath);
            }

            var materialTexture = textures.FirstOrDefault(t => t.Type == TextureType.MaterialMap);
            if (materialTexture.Path != null)
            {
                var systemPath = _ddsToMaterialPngExporter.Export(materialTexture.Path, settings.OutputPath, settings.ConvertMaterialTextureToBlender);
                material.WithChannelImage(KnownChannel.MetallicRoughness, systemPath);
            }

            var baseColourTexture = textures.FirstOrDefault(t => t.Type == TextureType.BaseColour);
            if (baseColourTexture.Path != null)
            {
                //  systemPath = _ddsToPngExporter.GenericExportNoConversion(settings.OutputPath, baseColourTexture);
                //  material.WithChannelImage(KnownChannel.BaseColor, systemPath);
            }

            return material;

        }
    }
}
