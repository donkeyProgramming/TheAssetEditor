using Editors.ImportExport.Exporting.Exporters.DdsToMaterialPng;
using Editors.ImportExport.Exporting.Exporters.DdsToNormalPng;
using Shared.GameFormats.RigidModel;
using Shared.GameFormats.RigidModel.Types;
using SharpGLTF.Materials;

namespace Editors.ImportExport.Exporting.Exporters.RmvToGltf.Helpers
{
    public record TextureResult(int MeshIndex, string SystemFilePath, KnownChannel GlftTexureType);

    public interface IGltfTextureHandler
    {
        public List<TextureResult> HandleTextures(RmvFile rmvFile, RmvToGltfExporterSettings settings);
    }

    public class GltfTextureHandler : IGltfTextureHandler
    {
        private readonly IDdsToNormalPngExporter _ddsToNormalPngExporter;
        private readonly IDdsToMaterialPngExporter _ddsToMaterialPngExporter;

        public GltfTextureHandler(IDdsToNormalPngExporter ddsToNormalPngExporter, IDdsToMaterialPngExporter ddsToMaterialPngExporter)
        {
            _ddsToNormalPngExporter = ddsToNormalPngExporter;
            _ddsToMaterialPngExporter = ddsToMaterialPngExporter;
        }

        public List<TextureResult> HandleTextures(RmvFile rmvFile, RmvToGltfExporterSettings settings)
        {
            var lodLevel = rmvFile.ModelList.First();
            var output = new List<TextureResult>();

            var exportedTextures = new Dictionary<string, string>();    // To avoid exporting same texture multiple times

            for(var i = 0; i < lodLevel.Length; i++) 
            {
                var model = lodLevel[i];
                var textures = ExtractTextures(model);

                var normalMapTexture = textures.FirstOrDefault(t => t.Type == TextureType.Normal);
                if (normalMapTexture?.Path != null)
                {
                    if (exportedTextures.ContainsKey(normalMapTexture.Path) == false)
                        exportedTextures[normalMapTexture.Path] = _ddsToNormalPngExporter.Export(normalMapTexture.Path, settings.OutputPath, settings.ConvertNormalTextureToBlue);

                    var systemPath = exportedTextures[normalMapTexture.Path];
                    if (systemPath != null)
                        output.Add(new TextureResult(i, systemPath, KnownChannel.Normal));
                }

                var materialTexture = textures.FirstOrDefault(t => t.Type == TextureType.MaterialMap);
                if (materialTexture?.Path != null)
                {
                    if (exportedTextures.ContainsKey(materialTexture.Path) == false)
                        exportedTextures[materialTexture.Path] = _ddsToMaterialPngExporter.Export(materialTexture.Path, settings.OutputPath, settings.ConvertMaterialTextureToBlender);

                    var systemPath = exportedTextures[materialTexture.Path];
                    if (systemPath != null)
                        output.Add(new TextureResult(i, systemPath, KnownChannel.MetallicRoughness));
                }

                var baseColourTexture = textures.FirstOrDefault(t => t.Type == TextureType.BaseColour);
                if (baseColourTexture?.Path != null)
                {
                    if (exportedTextures.ContainsKey(baseColourTexture.Path) == false)
                        exportedTextures[baseColourTexture.Path] = _ddsToMaterialPngExporter.Export(baseColourTexture.Path, settings.OutputPath, false);
                    
                    var systemPath = exportedTextures[baseColourTexture.Path];
                    if (systemPath != null)
                        output.Add(new TextureResult(i, systemPath, KnownChannel.BaseColor));
                }
            }

            return output;
        }

        List<MaterialBuilderTextureInput> ExtractTextures(RmvModel model)
        {
            var textures = model.Material.GetAllTextures();
            var output = textures.Select(x => new MaterialBuilderTextureInput(x.Path, x.TexureType)).ToList();
            return output;
        }

        record MaterialBuilderTextureInput(string Path, TextureType Type);
    }
}
