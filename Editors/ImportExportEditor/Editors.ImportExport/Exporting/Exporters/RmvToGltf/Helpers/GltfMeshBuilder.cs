using System.IO;
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
        public List<IMeshBuilder<MaterialBuilder>> Build(RmvFile rmv2, List<TextureResult> textures, RmvToGltfExporterSettings settings, bool willHaveSkeleton = true)
        {
            var lodLevel = rmv2.ModelList.First();
            var hasSkeleton = willHaveSkeleton && string.IsNullOrWhiteSpace(rmv2.Header.SkeletonName) == false;

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
            // Only enable skinning validation if the model has a skeleton and this mesh actually contains weight data
            var hasAnyWeights = rmvMesh.VertexList.Any(v => v.WeightCount > 0);
            if (hasSkeleton && hasAnyWeights)
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
                    if (vertex.WeightCount > 0)
                    {
                        glTfvertex = SetVertexInfluences(vertex, glTfvertex);
                    }
                    else if (hasAnyWeights)
                    {
                        // If some vertices have weights in this mesh we enabled validation.
                        // Ensure vertices without weights get a default binding so validation passes.
                        glTfvertex.Skinning.SetBindings((0, 1), (0, 0), (0, 0), (0, 0));
                    }
                }
                else if (hasAnyWeights)
                {
                    // Model has weight data but no skeleton is available.
                    // Set default binding to prevent validation errors.
                    glTfvertex.Skinning.SetBindings((0, 1), (0, 0), (0, 0), (0, 0));
                }

                // For static meshes or vertices handled above, add the vertex
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
            // Support 1,2,3,4 weight counts and normalize/handle degenerate cases so SharpGLTF validation won't fail.
            var weights = new float[4];
            var indices = new int[4];

            var count = Math.Clamp(vertex.WeightCount, 0, 4);
            for (int i = 0; i < count; ++i)
            {
                indices[i] = vertex.BoneIndex[i];
                weights[i] = vertex.BoneWeight[i];
                // guard against negative weights from malformed data
                if (weights[i] < 0) weights[i] = 0f;
            }

            // If there are fewer than 4 influences, remaining indices default to 0 and weights to 0
            for (int i = count; i < 4; ++i)
            {
                indices[i] = 0;
                weights[i] = 0f;
            }

            float sum = weights[0] + weights[1] + weights[2] + weights[3];

            if (sum <= float.Epsilon)
            {
                // Degenerate: no meaningful weights. Fall back to binding to the first available bone or to bone 0.
                if (count > 0)
                {
                    indices[0] = vertex.BoneIndex[0];
                    weights[0] = 1f;
                    weights[1] = weights[2] = weights[3] = 0f;
                }
                else
                {
                    indices[0] = 0;
                    weights[0] = 1f;
                    weights[1] = weights[2] = weights[3] = 0f;
                }
            }
            else
            {
                // Normalize weights so they sum to 1
                weights[0] /= sum;
                weights[1] /= sum;
                weights[2] /= sum;
                weights[3] /= sum;
            }

            var rigging = new (int, float)[4]
            {
                (indices[0], weights[0]),
                (indices[1], weights[1]),
                (indices[2], weights[2]),
                (indices[3], weights[3])
            };

            glTfvertex.Skinning.SetBindings(rigging);

            return glTfvertex;
        }

        MaterialBuilder Create(RmvToGltfExporterSettings settings, string materialName, List<TextureResult> texturesForModel)
        {
            var material = new MaterialBuilder(materialName)
                  .WithDoubleSide(true)
                  .WithMetallicRoughness()
                  .WithAlpha(AlphaMode.MASK);

            foreach (var texture in texturesForModel)
            {
                material.WithChannelImage(texture.GlftTexureType, texture.SystemFilePath);

                var channel = material.UseChannel(texture.GlftTexureType);
                if (channel?.Texture?.PrimaryImage != null) 
                {
                    // Set SharpGLTF to re-resave textures with specified paths, default behavior is texturePath = "{folder}\meshName{counter}.png"
                    channel.Texture.PrimaryImage.AlternateWriteFileName = Path.GetFileName(texture.SystemFilePath);
                }                               
            }

            return material;
        }
    }
}
