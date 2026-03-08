using System.IO;
using System.Numerics;
using Editors.ImportExport.Common;
using Shared.GameFormats.RigidModel;
using SharpGLTF.Geometry;
using SharpGLTF.Geometry.VertexTypes;
using SharpGLTF.Materials;
using AlphaMode = SharpGLTF.Materials.AlphaMode;

namespace Editors.ImportExport.Exporting.Exporters.RmvToGltf.Helpers
{
    public class GltfStaticMeshBuilder
    {
        public List<IMeshBuilder<MaterialBuilder>> Build(RmvFile rmv2, List<TextureResult> textures, RmvToGltfExporterSettings settings)
        {
            var lodLevel = rmv2.ModelList.First();

            var meshes = new List<IMeshBuilder<MaterialBuilder>>();
            for (var i = 0; i < lodLevel.Length; i++)
            {
                var rmvMesh = lodLevel[i];
                var meshTextures = textures.Where(x => x.MeshIndex == i).ToList();
                var gltfMaterial = Create(settings, rmvMesh.Material.ModelName + "_Material", meshTextures);
                var gltfMesh = GenerateStaticMesh(rmvMesh.Mesh, rmvMesh.Material.ModelName, gltfMaterial, settings.MirrorMesh);
                meshes.Add(gltfMesh);
            }
            return meshes;
        }

        MeshBuilder<VertexPositionNormalTangent, VertexTexture1, VertexEmpty> GenerateStaticMesh(RmvMesh rmvMesh, string modelName, MaterialBuilder material, bool doMirror)
        {
            var mesh = new MeshBuilder<VertexPositionNormalTangent, VertexTexture1, VertexEmpty>(modelName);
            var prim = mesh.UsePrimitive(material);

            var vertexList = new List<VertexBuilder<VertexPositionNormalTangent, VertexTexture1, VertexEmpty>>();
            foreach (var vertex in rmvMesh.VertexList)
            {
                var glTfvertex = new VertexBuilder<VertexPositionNormalTangent, VertexTexture1, VertexEmpty>();
                glTfvertex.Geometry.Position = new Vector3(vertex.Position.X, vertex.Position.Y, vertex.Position.Z);
                glTfvertex.Geometry.Normal = new Vector3(vertex.Normal.X, vertex.Normal.Y, vertex.Normal.Z);
                glTfvertex.Geometry.Tangent = new Vector4(vertex.Tangent.X, vertex.Tangent.Y, vertex.Tangent.Z, 1);
                glTfvertex.Material.TexCoord = new Vector2(vertex.Uv.X, vertex.Uv.Y);

                // Apply geometric transformations
                glTfvertex.Geometry.Position = VecConv.GetSys(GlobalSceneTransforms.FlipVector(VecConv.GetXna(glTfvertex.Geometry.Position), doMirror));
                glTfvertex.Geometry.Normal = VecConv.GetSys(GlobalSceneTransforms.FlipVector(VecConv.GetXna(glTfvertex.Geometry.Normal), doMirror));
                glTfvertex.Geometry.Tangent = VecConv.GetSys(GlobalSceneTransforms.FlipVector(VecConv.GetXna(glTfvertex.Geometry.Tangent), doMirror));

                // Validate and fix normals and tangents for quality
                glTfvertex.Geometry.Normal = ValidateAndFixNormal(glTfvertex.Geometry.Normal);
                glTfvertex.Geometry.Tangent = ValidateAndFixTangent(glTfvertex.Geometry.Tangent, glTfvertex.Geometry.Normal);

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

        Vector3 ValidateAndFixNormal(Vector3 normal)
        {
            float lengthSquared = normal.LengthSquared();
            
            // Check for zero or near-zero length normals
            if (lengthSquared < 0.0001f)
            {
                // Return a safe default normal pointing up
                return new Vector3(0, 0, 1);
            }

            // Check if normalization is needed (tolerance for floating point precision)
            if (Math.Abs(lengthSquared - 1.0f) > 0.001f)
            {
                return Vector3.Normalize(normal);
            }

            return normal;
        }

        Vector4 ValidateAndFixTangent(Vector4 tangent, Vector3 normal)
        {
            var tangentXYZ = new Vector3(tangent.X, tangent.Y, tangent.Z);
            float lengthSquared = tangentXYZ.LengthSquared();
            
            // Check for zero or near-zero length tangents - generate a perpendicular vector
            if (lengthSquared < 0.0001f)
            {
                tangentXYZ = GeneratePerpendicularVector(normal);
                return new Vector4(tangentXYZ.X, tangentXYZ.Y, tangentXYZ.Z, 1);
            }

            // Normalize tangent if needed
            if (Math.Abs(lengthSquared - 1.0f) > 0.001f)
            {
                tangentXYZ = Vector3.Normalize(tangentXYZ);
            }

            // Ensure tangent handedness is valid (W should be ±1, typically 1 for right-handed)
            float handedness = tangent.W;
            if (Math.Abs(handedness) < 0.5f)
            {
                handedness = 1.0f;
            }
            else
            {
                handedness = handedness > 0 ? 1.0f : -1.0f;
            }

            return new Vector4(tangentXYZ.X, tangentXYZ.Y, tangentXYZ.Z, handedness);
        }

        Vector3 GeneratePerpendicularVector(Vector3 normal)
        {
            Vector3 tangent;
            
            // Choose axis that's most perpendicular to normal
            if (Math.Abs(normal.X) > 0.9f)
            {
                tangent = Vector3.Cross(normal, new Vector3(0, 1, 0));
            }
            else
            {
                tangent = Vector3.Cross(normal, new Vector3(1, 0, 0));
            }
            
            return Vector3.Normalize(tangent);
        }

        MaterialBuilder Create(RmvToGltfExporterSettings settings, string materialName, List<TextureResult> texturesForModel)
        {
            // Option 4: Material Enhancement with proper PBR setup
            var material = new MaterialBuilder(materialName)
                  .WithDoubleSide(true)
                  .WithMetallicRoughness();

            // Enhanced alpha detection for masked geometry (capes, fur, wings)
            bool hasAlphaMaskedTexture = texturesForModel.Any(t => t.HasAlphaChannel);
            bool hasMaskInName = texturesForModel.Any(t => 
                t.SystemFilePath.Contains("mask", StringComparison.OrdinalIgnoreCase) ||
                t.SystemFilePath.Contains("_m.", StringComparison.OrdinalIgnoreCase));
            bool hasTransparency = texturesForModel.Any(t => 
                t.SystemFilePath.Contains("alpha", StringComparison.OrdinalIgnoreCase) ||
                t.SystemFilePath.Contains("transparent", StringComparison.OrdinalIgnoreCase));
            
            // Detect common alpha-masked mesh types
            bool isAlphaMaskedMesh = materialName.Contains("cape", StringComparison.OrdinalIgnoreCase) ||
                                     materialName.Contains("fur", StringComparison.OrdinalIgnoreCase) ||
                                     materialName.Contains("wing", StringComparison.OrdinalIgnoreCase) ||
                                     materialName.Contains("feather", StringComparison.OrdinalIgnoreCase) ||
                                     materialName.Contains("hair", StringComparison.OrdinalIgnoreCase) ||
                                     materialName.Contains("foliage", StringComparison.OrdinalIgnoreCase) ||
                                     materialName.Contains("leaf", StringComparison.OrdinalIgnoreCase) ||
                                     materialName.Contains("chain", StringComparison.OrdinalIgnoreCase);

            // Set appropriate alpha mode
            if (hasTransparency)
            {
                material.WithAlpha(AlphaMode.BLEND);
            }
            else if (hasAlphaMaskedTexture || hasMaskInName || isAlphaMaskedMesh)
            {
                // Use MASK mode with alpha cutoff for sharp edges (better for fur/capes)
                material.WithAlpha(AlphaMode.MASK, 0.5f);
            }
            else
            {
                material.WithAlpha(AlphaMode.OPAQUE);
            }

            foreach (var texture in texturesForModel)
            {
                material.WithChannelImage(texture.GlftTexureType, texture.SystemFilePath);

                var channel = material.UseChannel(texture.GlftTexureType);
                if (channel?.Texture?.PrimaryImage != null)
                {
                    // Set SharpGLTF to re-resave textures with specified paths
                    channel.Texture.PrimaryImage.AlternateWriteFileName = Path.GetFileName(texture.SystemFilePath);
                }
            }

            return material;
        }
    }
}
