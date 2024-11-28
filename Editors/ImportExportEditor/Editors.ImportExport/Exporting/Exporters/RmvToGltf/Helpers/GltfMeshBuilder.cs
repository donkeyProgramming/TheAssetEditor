using System.Numerics;
using Editors.ImportExport.Common;
using Shared.GameFormats.RigidModel;
using Shared.GameFormats.RigidModel.Vertex;
using SharpGLTF.Geometry;
using SharpGLTF.Geometry.VertexTypes;
using SharpGLTF.Materials;
using AlphaMode = SharpGLTF.Materials.AlphaMode;

namespace Editors.ImportExport.Exporting.Exporters.RmvToGltf.Helpers
{
    public class GltfMeshBuilder
    {
        public List<IMeshBuilder<MaterialBuilder>> Build(RmvFile rmv2, List<TextureResult> textures, RmvToGltfExporterSettings settings)
        {
            var lodLevel = rmv2.ModelList.First();
            var hasSkeleton = string.IsNullOrWhiteSpace(rmv2.Header.SkeletonName) == false;

            var meshes = new List<IMeshBuilder<MaterialBuilder>>();
            for(var i = 0; i < lodLevel.Length; i++)
            {
                var rmvMesh = lodLevel[i];
                var meshTextures = textures.Where(x=>x.MeshIndex == i).ToList();
                var gltfMaterial = Create(settings, rmvMesh.Material.ModelName + "_Material", meshTextures);
                var gltfMesh = GenerateMesh(rmvMesh.Mesh, rmvMesh.Material.ModelName, gltfMaterial, hasSkeleton, settings.MirrorMesh);
                meshes.Add(gltfMesh);
            }
            return meshes;
        }

        MeshBuilder<VertexPositionNormalTangent, VertexTexture1, VertexJoints4> GenerateMesh(RmvMesh rmvMesh, string modelName, MaterialBuilder material, bool hasSkeleton, bool doMirror)
        {
            var mesh = new MeshBuilder<VertexPositionNormalTangent, VertexTexture1, VertexJoints4>(modelName);
            if (hasSkeleton)
                mesh.VertexPreprocessor.SetValidationPreprocessors();

            var prim = mesh.UsePrimitive(material);

            var vertexList = new List<VertexBuilder<VertexPositionNormalTangent, VertexTexture1, VertexJoints4>>();
            foreach (var vertex in rmvMesh.VertexList)
            {
                var glTfvertex = new VertexBuilder<VertexPositionNormalTangent, VertexTexture1, VertexJoints4>();
                glTfvertex.Geometry.Position = new Vector3(vertex.Position.X, vertex.Position.Y, vertex.Position.Z);
                glTfvertex.Geometry.Normal = new Vector3(vertex.Normal.X, vertex.Normal.Y, vertex.Normal.Z);
                glTfvertex.Geometry.Tangent = new Vector4(vertex.Tangent.X, vertex.Tangent.Y, vertex.Tangent.Z, 1);
                glTfvertex.Material.TexCoord = new Vector2(vertex.Uv.X, vertex.Uv.Y);

                glTfvertex.Geometry.Position = VecConv.GetSys(GlobalSceneTransforms.FlipVector(VecConv.GetXna(glTfvertex.Geometry.Position), doMirror));

                glTfvertex.Geometry.Normal = Vector3.Normalize(VecConv.GetSys(GlobalSceneTransforms.FlipVector(VecConv.GetXna(glTfvertex.Geometry.Normal), doMirror)));
                glTfvertex.Geometry.Tangent = VecConv.NormalizeTangentVector4(VecConv.GetSys(GlobalSceneTransforms.FlipVector(VecConv.GetXna(glTfvertex.Geometry.Tangent), doMirror)));

                if (hasSkeleton)
                {
                    glTfvertex = SetVertexInfluences(vertex, glTfvertex);
                }
                else
                {
                    glTfvertex.Skinning.SetBindings((0, 1), (0, 0), (0, 0), (0, 0));
                }
                vertexList.Add(glTfvertex);
            }

            var triangleCount = rmvMesh.IndexList.Length;
            for (var i = 0; i < triangleCount; i += 3)
            {

                ushort i0, i1, i2;
                if (doMirror) // if mirrored, flip the winding order
                {
                    i0 = rmvMesh.IndexList[i + 0];
                    i1 = rmvMesh.IndexList[i + 2];
                    i2 = rmvMesh.IndexList[i + 1];
                }
                else
                {
                    i0 = rmvMesh.IndexList[i + 0];
                    i1 = rmvMesh.IndexList[i + 1];
                    i2 = rmvMesh.IndexList[i + 2];
                }

                prim.AddTriangle(vertexList[i0], vertexList[i1], vertexList[i2]);
            }
            return mesh;
        }


        VertexBuilder<VertexPositionNormalTangent, VertexTexture1, VertexJoints4> SetVertexInfluences(CommonVertex vertex, VertexBuilder<VertexPositionNormalTangent, VertexTexture1, VertexJoints4> glTfvertex)
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

            return glTfvertex;
        }

        MaterialBuilder Create(RmvToGltfExporterSettings settings, string materialName, List<TextureResult> texturesForModel)
        {
            var material = new MaterialBuilder(materialName)
                  .WithDoubleSide(true)
                  .WithMetallicRoughness()
                  .WithAlpha(AlphaMode.MASK);

            foreach (var texture in texturesForModel)
                material.WithChannelImage(texture.GlftTexureType, texture.SystemFilePath);

            return material;
        }
    }
}
