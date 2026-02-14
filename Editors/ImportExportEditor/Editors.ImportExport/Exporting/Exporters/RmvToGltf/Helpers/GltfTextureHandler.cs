using Editors.ImportExport.Exporting.Exporters.DdsToMaterialPng;
using Editors.ImportExport.Exporting.Exporters.DdsToNormalPng;
using Shared.GameFormats.RigidModel;
using Shared.GameFormats.RigidModel.Types;
using Shared.Core.PackFiles;
using SharpGLTF.Materials;

namespace Editors.ImportExport.Exporting.Exporters.RmvToGltf.Helpers
{
    public record TextureResult(int MeshIndex, string SystemFilePath, KnownChannel GlftTexureType, bool HasAlphaChannel = false);

    public interface IGltfTextureHandler
    {
        public List<TextureResult> HandleTextures(RmvFile rmvFile, RmvToGltfExporterSettings settings);
    }

    public class GltfTextureHandler : IGltfTextureHandler
    {
        private readonly IDdsToNormalPngExporter _ddsToNormalPngExporter;
        private readonly IDdsToMaterialPngExporter _ddsToMaterialPngExporter;
        private readonly IDisplacementMapGenerator _displacementMapGenerator;
        private readonly IPackFileService _packFileService;

        public GltfTextureHandler(IDdsToNormalPngExporter ddsToNormalPngExporter, IDdsToMaterialPngExporter ddsToMaterialPngExporter, IDisplacementMapGenerator displacementMapGenerator = null, IPackFileService packFileService = null)
        {
            _ddsToNormalPngExporter = ddsToNormalPngExporter;
            _ddsToMaterialPngExporter = ddsToMaterialPngExporter;
            _displacementMapGenerator = displacementMapGenerator ?? new DisplacementMapGenerator();
            _packFileService = packFileService;
        }

        public List<TextureResult> HandleTextures(RmvFile rmvFile, RmvToGltfExporterSettings settings)
        {
            var output = new List<TextureResult>();

            if (!settings.ExportMaterials)
                return output;

            var exportedTextures = new Dictionary<string, string>();    // To avoid exporting same texture multiple times

            int lodICounnt = 1;
            for (var lodIndex = 0; lodIndex < lodICounnt; lodIndex++)
            {
                for (var meshIndex = 0; meshIndex < rmvFile.ModelList[lodIndex].Length; meshIndex++)
                {
                    var model = rmvFile.ModelList[lodIndex][meshIndex];
                    var textures = ExtractTextures(model);

                    // Check if this mesh has both diffuse and mask textures - if so, combine them
                    var diffuseTexture = textures.FirstOrDefault(t => t.Type == TextureType.Diffuse || t.Type == TextureType.BaseColour);
                    var maskTexture = textures.FirstOrDefault(t => t.Type == TextureType.Mask);

                    bool shouldCombineMask = diffuseTexture != null && maskTexture != null;

                    foreach (var tex in textures)
                    {
                        // Skip the mask if we're combining it with diffuse
                        if (shouldCombineMask && tex.Type == TextureType.Mask)
                            continue;

                        switch (tex.Type)
                        {
                            case TextureType.Normal: DoTextureConversionNormalMap(settings, output, exportedTextures, meshIndex, tex); break;
                            case TextureType.MaterialMap: DoTextureConversionMaterialMap(settings, output, exportedTextures, meshIndex, tex); break;
                            case TextureType.BaseColour: 
                                if (shouldCombineMask)
                                    DoCombinedDiffuseWithMask(settings, output, exportedTextures, meshIndex, tex, maskTexture);
                                else
                                    DoTextureDefault(KnownChannel.BaseColor, settings, output, exportedTextures, meshIndex, tex);
                                break;
                            case TextureType.Diffuse: 
                                if (shouldCombineMask)
                                    DoCombinedDiffuseWithMask(settings, output, exportedTextures, meshIndex, tex, maskTexture);
                                else
                                    DoTextureDefault(KnownChannel.BaseColor, settings, output, exportedTextures, meshIndex, tex);
                                break;
                            case TextureType.Specular: DoTextureDefault(KnownChannel.SpecularColor, settings, output, exportedTextures, meshIndex, tex); break;
                            case TextureType.Gloss: DoTextureDefault(KnownChannel.MetallicRoughness, settings, output, exportedTextures, meshIndex, tex); break;
                            case TextureType.Ambient_occlusion: DoTextureDefault(KnownChannel.Occlusion, settings, output, exportedTextures, meshIndex, tex); break;
                            case TextureType.Emissive: DoTextureDefault(KnownChannel.Emissive, settings, output, exportedTextures, meshIndex, tex); break;
                            case TextureType.EmissiveDistortion: DoTextureDefault(KnownChannel.Emissive, settings, output, exportedTextures, meshIndex, tex); break;
                        }
                    }
                }


            }

            return output;
        }
        interface IDDsToPngExporter
        {
            public string Export(string path, string outputPath, bool convertToBlender)
            {
                throw new System.NotImplementedException();
            }
        }


        List<MaterialBuilderTextureInput> ExtractTextures(RmvModel model)
        {
            var textures = model.Material.GetAllTextures();
            var output = textures.Select(x => new MaterialBuilderTextureInput(x.Path, x.TexureType)).ToList();
            return output;
        }

        record MaterialBuilderTextureInput(string Path, TextureType Type);

        private void DoTextureConversionMaterialMap(RmvToGltfExporterSettings settings, List<TextureResult> output, Dictionary<string, string> exportedTextures, int meshIndex, MaterialBuilderTextureInput text)
        {
            if (exportedTextures.ContainsKey(text.Path) == false)
                exportedTextures[text.Path] = _ddsToMaterialPngExporter.Export(text.Path, settings.OutputPath, settings.ConvertMaterialTextureToBlender);

            var systemPath = exportedTextures[text.Path];
            if (systemPath != null)
                output.Add(new TextureResult(meshIndex, systemPath, KnownChannel.MetallicRoughness));
        }

        private void DoTextureDefault(KnownChannel textureType, RmvToGltfExporterSettings settings, List<TextureResult> output, Dictionary<string, string> exportedTextures, int meshIndex, MaterialBuilderTextureInput text)
        {
            if (exportedTextures.ContainsKey(text.Path) == false)
                exportedTextures[text.Path] = _ddsToMaterialPngExporter.Export(text.Path, settings.OutputPath, false);

            var systemPath = exportedTextures[text.Path];
            if (systemPath != null)
                output.Add(new TextureResult(meshIndex, systemPath, textureType, hasAlphaChannel: false));
        }

        private void DoCombinedDiffuseWithMask(RmvToGltfExporterSettings settings, List<TextureResult> output, Dictionary<string, string> exportedTextures, int meshIndex, MaterialBuilderTextureInput diffuseTexture, MaterialBuilderTextureInput maskTexture)
        {
            var combinedKey = $"{diffuseTexture.Path}+{maskTexture.Path}";

            if (exportedTextures.ContainsKey(combinedKey) == false)
            {
                try
                {
                    // Get the pack files
                    var diffusePackFile = _packFileService.FindFile(diffuseTexture.Path);
                    var maskPackFile = _packFileService.FindFile(maskTexture.Path);

                    if (diffusePackFile == null || maskPackFile == null)
                    {
                        throw new InvalidOperationException($"Could not find diffuse or mask texture in pack files");
                    }

                    // Read DDS data
                    var diffuseDdsBytes = diffusePackFile.DataSource.ReadData();
                    var maskDdsBytes = maskPackFile.DataSource.ReadData();

                    // Combine diffuse and mask
                    var combinedPngBytes = AlphaMaskCombiner.CombineDiffuseWithMask(diffuseDdsBytes, maskDdsBytes);

                    // Save combined texture
                    var fileName = Path.GetFileNameWithoutExtension(diffuseTexture.Path) + "_with_alpha.png";
                    var outDirectory = Path.GetDirectoryName(settings.OutputPath);
                    var outFilePath = Path.Combine(outDirectory, fileName);

                    File.WriteAllBytes(outFilePath, combinedPngBytes);
                    exportedTextures[combinedKey] = outFilePath;
                }
                catch (Exception ex)
                {
                    // If combining fails, fall back to just the diffuse
                    exportedTextures[combinedKey] = _ddsToMaterialPngExporter.Export(diffuseTexture.Path, settings.OutputPath, false);
                }
            }

            var systemPath = exportedTextures[combinedKey];
            if (systemPath != null)
                // Mark as having alpha channel since we combined the mask into it
                output.Add(new TextureResult(meshIndex, systemPath, KnownChannel.BaseColor, hasAlphaChannel: true));
        }

        private void DoTextureConversionNormalMap(RmvToGltfExporterSettings settings, List<TextureResult> output, Dictionary<string, string> exportedTextures, int meshIndex, MaterialBuilderTextureInput text)
        {
            if (exportedTextures.ContainsKey(text.Path) == false)
                exportedTextures[text.Path] = _ddsToNormalPngExporter.Export(text.Path, settings.OutputPath, settings.ConvertNormalTextureToBlue);

            var systemPath = exportedTextures[text.Path];
            if (systemPath != null)
                output.Add(new TextureResult(meshIndex, systemPath, KnownChannel.Normal));
        }
    }
}
