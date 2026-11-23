using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Editors.ImportExport.Importing;
using Editors.ImportExport.Importing.Importers.PngToDds;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.Settings;
using Shared.GameFormats.RigidModel;
using Shared.GameFormats.RigidModel.MaterialHeaders;
using Shared.GameFormats.RigidModel.Types;
using SharpGLTF.Schema2;
using TextureType = Shared.GameFormats.RigidModel.Types.TextureType;

namespace Editors.ImportExport.Importing.Importers.GltfToRmv.Helper
{
    // TODO: MOVE THIS TO A SHARED LOCATION
    public class TextureTypeHelper
    {
        static private readonly Dictionary<string, (TextureType texutureType, string namePostFix)> _stringIdToTextureType = new Dictionary<string, (TextureType, string)>()
            {
                {"BaseColor", (TextureType.BaseColour, "base_colour")},
                {"Normal", (TextureType.Normal, "normal")},
                {"MetallicRoughness", (TextureType.MaterialMap, "material_map")},
                {"Diffuse", (TextureType.Diffuse, "diffuse")},
                {"Specular", (TextureType.Specular, "specular")},
                {"Glossiness", (TextureType.Gloss, "gloss_map") }
            };

        static public bool GetRmvTextureTypeFromGltfIdString(string textureTypeString, out TextureType outTextureType, out string postFix)
        {
            if (_stringIdToTextureType.TryGetValue(textureTypeString, out var textureType))
            {
                outTextureType = textureType.texutureType;
                postFix = textureType.namePostFix; 
                return true;
            }

            outTextureType = TextureType.Diffuse;
            postFix = "diffuse";
            return false;
        }
    }
    public class RmvMaterialBuilder
    {
        internal static IReadOnlyList<string> GetBlendMaterialNames(
            ModelRoot modelRoot) =>
            modelRoot.LogicalMaterials
                .Where(material => material.Alpha == SharpGLTF.Schema2.AlphaMode.BLEND)
                .Select(GetMaterialName)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToList();

        internal RmvMaterialBuildResult BuildRmvFileMaterials(
            GltfImporterSettings settings,
            SharpGLTF.Schema2.ModelRoot modelRoot,
            RmvFile rmvFile,
            IReadOnlyList<RmvMeshBuilder.MeshSource> meshSources,
            IProgress<ImportProgress>? progress = null)
        {
            ValidateInput_BuildRmvFileMaterials(modelRoot, rmvFile, meshSources);

            var textureEntries = new List<NewPackFileEntry>();
            var importedTexturePaths = new Dictionary<TextureCacheKey, string>();
            var maskedMaterials = new HashSet<MaskedMaterialImportSummary>();
            var skippedSemantics = new HashSet<SkippedMaterialSemantic>();
            for (int i = 0; i < meshSources.Count; i++)
            {
                progress?.Report(new ImportProgress(
                    string.Format("Converting materials and textures {0:N0}/{1:N0}...",
                        i + 1,
                        meshSources.Count),
                    meshSources[i].ModelName,
                    i,
                    meshSources.Count));
                BuildRmvModelMaterial(
                    settings,
                    meshSources[i],
                    rmvFile.ModelList[0][i],
                    textureEntries,
                    importedTexturePaths,
                    maskedMaterials,
                    skippedSemantics
                );
                progress?.Report(new ImportProgress(
                    string.Format("Converting materials and textures {0:N0}/{1:N0}...",
                        i + 1,
                        meshSources.Count),
                    meshSources[i].ModelName,
                    i + 1,
                    meshSources.Count));
            }

            rmvFile.RecalculateOffsets();
            return new RmvMaterialBuildResult(
                textureEntries,
                new MaterialImportSummary(
                    maskedMaterials
                        .OrderBy(item => item.MaterialName, StringComparer.Ordinal)
                        .ToList(),
                    skippedSemantics
                        .OrderBy(item => item.MaterialName, StringComparer.Ordinal)
                        .ThenBy(item => item.Semantic, StringComparer.Ordinal)
                        .ToList()));
        }

        private void BuildRmvModelMaterial(
            GltfImporterSettings settings,
            RmvMeshBuilder.MeshSource source,
            RmvModel rmvModel,
            ICollection<NewPackFileEntry> textureEntries,
            IDictionary<TextureCacheKey, string> importedTexturePaths,
            ISet<MaskedMaterialImportSummary> maskedMaterials,
            ISet<SkippedMaterialSemantic> skippedSemantics)
        {
            var gltfMaterial = source.Primitive.Material;
            var assignedTextureTypes = new HashSet<TextureType>();
            SetAlphaMode(rmvModel, gltfMaterial);
            var alphaThreshold = GetAlphaThreshold(gltfMaterial);
            if (gltfMaterial?.Alpha == SharpGLTF.Schema2.AlphaMode.MASK)
            {
                maskedMaterials.Add(new MaskedMaterialImportSummary(
                    GetMaterialName(gltfMaterial),
                    alphaThreshold));
            }

            foreach (var itText in gltfMaterial?.Channels ?? [])
            {
                if (!TextureTypeHelper.GetRmvTextureTypeFromGltfIdString(
                    itText.Key,
                    out var textureType,
                    out var postFixString))
                {
                    if (ShouldReportSkippedSemantic(itText))
                    {
                        skippedSemantics.Add(new SkippedMaterialSemantic(
                            GetMaterialName(gltfMaterial!),
                            itText.Key));
                    }
                    continue;
                }

                if (itText.Texture == null) continue;

                var gameType = settings.SelectedGame;                
                
                var texturePackFolder = GetTexturePackFolder(settings, source.ModelName, postFixString);
                var shouldConvert = textureType switch
                {
                    TextureType.MaterialMap => settings.ConvertMaterialFromBlenderType,
                    TextureType.Normal => settings.ConvertNormalTextureFromBlueToOrangeType,
                    _ => true,
                };

                using var imageStream = itText.Texture.PrimaryImage.Content.Open();
                var ddsPackFile = PngToDdsImporter.Import(
                    imageStream,
                    textureType,
                    gameType,
                    Path.GetFileName(texturePackFolder),
                    shouldConvert,
                    textureType == TextureType.BaseColour ? alphaThreshold : 0.5f);

                AddTexture(
                    rmvModel,
                    textureEntries,
                    importedTexturePaths,
                    textureType,
                    texturePackFolder,
                    ddsPackFile,
                    gameType,
                    shouldConvert,
                    textureType == TextureType.BaseColour ? alphaThreshold : 0.5f);
                assignedTextureTypes.Add(textureType);
            }

            if (settings.SelectedGame != GameTypeEnum.Warhammer3)
                return;

            AddNeutralTextureIfMissing(
                settings,
                source.ModelName,
                rmvModel,
                textureEntries,
                importedTexturePaths,
                assignedTextureTypes,
                TextureType.BaseColour,
                "base_colour",
                alphaThreshold);
            AddNeutralTextureIfMissing(
                settings,
                source.ModelName,
                rmvModel,
                textureEntries,
                importedTexturePaths,
                assignedTextureTypes,
                TextureType.Normal,
                "normal");
            AddNeutralTextureIfMissing(
                settings,
                source.ModelName,
                rmvModel,
                textureEntries,
                importedTexturePaths,
                assignedTextureTypes,
                TextureType.MaterialMap,
                "material_map");
            AddNeutralTextureIfMissing(
                settings,
                source.ModelName,
                rmvModel,
                textureEntries,
                importedTexturePaths,
                assignedTextureTypes,
                TextureType.Mask,
                "mask");
        }

        private static bool ShouldReportSkippedSemantic(MaterialChannel channel) =>
            channel.Texture != null ||
            channel.Parameters.Any(parameter => !parameter.IsDefault);

        private static void SetAlphaMode(
            RmvModel rmvModel,
            SharpGLTF.Schema2.Material? gltfMaterial)
        {
            if (rmvModel.Material is not WeightedMaterial weightedMaterial)
                throw new InvalidDataException("The target RMV2 material doesn't support glTF alpha modes; import stopped.");

            weightedMaterial.IntParams.Set(
                WeightedParamterIds.IntParams_Alpha_index,
                gltfMaterial?.Alpha == SharpGLTF.Schema2.AlphaMode.MASK ? 1 : 0);
        }

        private static float GetAlphaThreshold(
            SharpGLTF.Schema2.Material? gltfMaterial) =>
            gltfMaterial?.Alpha == SharpGLTF.Schema2.AlphaMode.MASK
                ? gltfMaterial.AlphaCutoff
                : 0.5f;

        private static string GetMaterialName(
            SharpGLTF.Schema2.Material material) =>
            string.IsNullOrWhiteSpace(material.Name)
                ? "Unnamed material"
                : material.Name;

        private static void AddNeutralTextureIfMissing(
            GltfImporterSettings settings,
            string modelName,
            RmvModel rmvModel,
            ICollection<NewPackFileEntry> textureEntries,
            IDictionary<TextureCacheKey, string> importedTexturePaths,
            ISet<TextureType> assignedTextureTypes,
            TextureType textureType,
            string postFix,
            float alphaThreshold = 0.5f)
        {
            if (assignedTextureTypes.Contains(textureType))
                return;

            var texturePath = GetTexturePackFolder(settings, modelName, postFix);
            var shouldConvert = textureType is
                TextureType.MaterialMap or TextureType.Normal;
            using var imageStream = CreateNeutralTextureStream(textureType);
            var ddsPackFile = PngToDdsImporter.Import(
                imageStream,
                textureType,
                settings.SelectedGame,
                Path.GetFileName(texturePath),
                shouldConvert,
                alphaThreshold);
            AddTexture(
                rmvModel,
                textureEntries,
                importedTexturePaths,
                textureType,
                texturePath,
                ddsPackFile,
                settings.SelectedGame,
                shouldConvert,
                alphaThreshold);
        }

        private static void AddTexture(
            RmvModel rmvModel,
            ICollection<NewPackFileEntry> textureEntries,
            IDictionary<TextureCacheKey, string> importedTexturePaths,
            TextureType textureType,
            string texturePath,
            PackFile ddsPackFile,
            GameTypeEnum gameType,
            bool shouldConvert,
            float alphaThreshold)
        {
            var hash = Convert.ToHexString(SHA256.HashData(
                ddsPackFile.DataSource.ReadData()));
            var cacheKey = new TextureCacheKey(
                gameType,
                textureType,
                shouldConvert,
                alphaThreshold,
                hash);
            if (importedTexturePaths.TryGetValue(cacheKey, out var importedPath))
            {
                rmvModel.Material.SetTexture(textureType, importedPath);
                return;
            }

            rmvModel.Material.SetTexture(textureType, texturePath);
            importedTexturePaths.Add(cacheKey, texturePath);
            textureEntries.Add(new NewPackFileEntry(
                Path.GetDirectoryName(texturePath) ?? "",
                ddsPackFile));
        }

        private readonly record struct TextureCacheKey(
            GameTypeEnum GameType,
            TextureType TextureType,
            bool ShouldConvert,
            float AlphaThreshold,
            string ContentHash);

        private static Stream CreateNeutralTextureStream(TextureType textureType)
        {
            var color = textureType switch
            {
                TextureType.BaseColour => Color.FromArgb(255, 255, 255, 255),
                TextureType.Normal => Color.FromArgb(255, 128, 128, 255),
                TextureType.MaterialMap => Color.FromArgb(255, 0, 255, 0),
                TextureType.Mask => Color.FromArgb(255, 0, 0, 0),
                _ => throw new ArgumentOutOfRangeException(nameof(textureType)),
            };
            using var bitmap = new Bitmap(4, 4, PixelFormat.Format32bppArgb);
            for (var y = 0; y < bitmap.Height; y++)
            {
                for (var x = 0; x < bitmap.Width; x++)
                    bitmap.SetPixel(x, y, color);
            }

            var stream = new MemoryStream();
            bitmap.Save(stream, ImageFormat.Png);
            stream.Position = 0;
            return stream;
        }

        private static string GetTexturePackFolder(GltfImporterSettings settings, string meshName, string postFixString)
        {
            // set file name
            var textureNameBase = meshName.Any() ? meshName : Path.GetFileNameWithoutExtension(settings.InputGltfFile);
            foreach (var invalidCharacter in Path.GetInvalidFileNameChars())
                textureNameBase = textureNameBase.Replace(invalidCharacter, '_');
            textureNameBase = textureNameBase.Trim().TrimEnd('.');
            if (string.IsNullOrWhiteSpace(textureNameBase))
                textureNameBase = "texture";

            var textureFileName = $"{textureNameBase}{(postFixString.Any() ? $"_{postFixString}.dds" : ".dds")}";

            var texturePackFolder = Path.Combine(
                settings.DestinationPackPath,
                "tex");
            var textureFullPackPath = Path.Combine(
                texturePackFolder,
                textureFileName);

            return textureFullPackPath;
        }

        private static void ValidateInput_BuildRmvFileMaterials(
            ModelRoot modelRoot,
            RmvFile rmvFile,
            IReadOnlyList<RmvMeshBuilder.MeshSource> meshSources)
        {
            if (modelRoot == null)
                throw new ArgumentNullException(nameof(modelRoot), "Invalid Scene: ModelRoot can't be null");

            if (modelRoot.LogicalNodes == null)
                throw new ArgumentNullException(nameof(modelRoot), "Invalid Scene: root.LogicalNodes can't be null");

            if (!modelRoot.LogicalNodes.Any())
                throw new Exception("Invalid Scene: no (logical) nodes");

            if (!rmvFile.ModelList.Any())
                throw new Exception("ERROR: unexpected not meshes in rmv2 file struct");

            if (rmvFile.ModelList[0].Length != meshSources.Count)
                throw new Exception("ERROR: unexpected rmv2 mesh count mismatch");
        }
    }

    internal sealed record RmvMaterialBuildResult(
        IReadOnlyList<NewPackFileEntry> Files,
        MaterialImportSummary Summary);
}

