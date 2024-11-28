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
            var output = new List<TextureResult>();

            var exportedTextures = new Dictionary<string, string>();    // To avoid exporting same texture multiple times

            int lodICounnt = 1;
            for (var lodIndex = 0; lodIndex < lodICounnt; lodIndex++)
            {
                for (var meshIndex = 0; meshIndex < rmvFile.ModelList[lodIndex].Length; meshIndex++)
                {
                    var model = rmvFile.ModelList[lodIndex][meshIndex];
                    var textures = ExtractTextures(model);

                    foreach (var tex in textures)
                    {
                        switch (tex.Type)
                        {
                            case TextureType.Normal: DoTextureConversionNormalMap(settings, output, exportedTextures, meshIndex, tex); break;
                            case TextureType.MaterialMap: DoTextureConversionMaterialMap(settings, output, exportedTextures, meshIndex, tex); break;
                            case TextureType.BaseColour: DoTextureDefault(KnownChannel.BaseColor, settings, output, exportedTextures, meshIndex, tex); break;
                            case TextureType.Diffuse: DoTextureDefault(KnownChannel.BaseColor, settings, output, exportedTextures, meshIndex, tex); break;
                            case TextureType.Specular: DoTextureDefault(KnownChannel.SpecularColor, settings, output, exportedTextures, meshIndex, tex); break;
                            case TextureType.Gloss: DoTextureDefault(KnownChannel.MetallicRoughness, settings, output, exportedTextures, meshIndex, tex); break;
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
                exportedTextures[text.Path] = _ddsToMaterialPngExporter.Export(text.Path, settings.OutputPath, false); // TODO: exchange export with a default one

            var systemPath = exportedTextures[text.Path];
            if (systemPath != null)
                output.Add(new TextureResult(meshIndex, systemPath, textureType));
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
