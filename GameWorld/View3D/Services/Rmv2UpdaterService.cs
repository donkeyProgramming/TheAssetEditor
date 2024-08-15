/*using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using GameWorld.Core.SceneNodes;
using GameWorld.Core.Utility;
using Shared.Core.ErrorHandling;
using Shared.Core.Misc;
using Shared.Core.PackFiles;
using Shared.GameFormats.RigidModel.Types;
using MS = Microsoft.Xna.Framework;*/

namespace GameWorld.Core.Services
{/*
    public class Rmv2UpdaterService
    {
        // TODO: maybe make nested inside a class, as there could be multiple options for generation the metal map too....
        public enum BaseColourGenerationTechniqueEnum
        {
            AdditiveBlending,
            ComparativeBlending,
        }

        private readonly PackFileService _pfs;
        private readonly string _outputFolder = DirectoryHelper.Temp + "\\TextureUpdater\\";
        private readonly bool _autoGenerateMissingTextures = false;
        private readonly bool _tryMatchingTexturesByName = true;
        private readonly bool _deleteOldTextures = false;

        public Rmv2UpdaterService(PackFileService pfs, bool autoGenerateMissingTextures = true)
        {

            _pfs = pfs;
            _autoGenerateMissingTextures = autoGenerateMissingTextures;
            DirectoryHelper.EnsureCreated(_outputFolder);
        }

        public void UpdateWh2Models(string modelPath, List<Rmv2MeshNode> models, BaseColourGenerationTechniqueEnum conversionTechnique, out ErrorList outputList)
        {
            outputList = new ErrorList();
            foreach (var model in models)
                ProcessModel(modelPath, model, conversionTechnique, outputList);
        }

        private void ProcessModel(string modelPath, Rmv2MeshNode model, BaseColourGenerationTechniqueEnum conversionTechnique, ErrorList outputList)
        {
            if (_tryMatchingTexturesByName)
            {
                // Check if all is ok
                if (AreTexturesWh3Format(model))
                {
                    DeleteWh2TextureReferences(model);
                    outputList.Ok($"{modelPath}-{model.Name}", $"Mesh already in Warhammer 3 format");
                    return;
                }

                // Try mapping the textures
                if (MatchMissingTexturesByName(modelPath, model, outputList))
                    return;
            }

            // Auto generate textures
            if (_autoGenerateMissingTextures)
            {
                var textures = model.GetTextures();
                var hasMaterialMapAndBaseColour = textures.ContainsKey(TextureType.MaterialMap) && textures.ContainsKey(TextureType.BaseColour);
                if (hasMaterialMapAndBaseColour == false)
                {
                    var textureDictionary = new Dictionary<TextureType, Bitmap>();
                    textureDictionary[TextureType.Diffuse] = ConvertTextureToByte(TextureType.Diffuse, modelPath, model, outputList);
                    textureDictionary[TextureType.Specular] = ConvertTextureToByte(TextureType.Specular, modelPath, model, outputList);
                    textureDictionary[TextureType.Gloss] = ConvertTextureToByte(TextureType.Gloss, modelPath, model, outputList);

                    if (textureDictionary.Select(x => x.Value).Any(x => x == null))
                    {
                        outputList.Error($"{modelPath}-{model.Name}", $"Unable to load all wh2 textures, can not auto generate missing textures");
                    }
                    else
                    {
                        switch (conversionTechnique)
                        {
                            case BaseColourGenerationTechniqueEnum.AdditiveBlending:
                                ConvertTextureAdditiveBlending(textureDictionary, modelPath, model, outputList);
                                break;

                            case BaseColourGenerationTechniqueEnum.ComparativeBlending:
                                ConvertTexturesUsingComparativeBlending(textureDictionary, modelPath, model, outputList);
                                break;
                        }

                    }
                }
            }

            if (AreTexturesWh3Format(model))
            {
                DeleteWh2TextureReferences(model);
                // Update texture directory
                outputList.Ok($"{modelPath}-{model.Name}", $"Updated to wh3 format");
            }
            else
                outputList.Error($"{modelPath}-{model.Name}", $"Failed to update to wh3 format");
        }

        private bool MatchMissingTexturesByName(string modelPath, Rmv2MeshNode model, ErrorList outputList)
        {
            var normalOK = UpdateTextureBasedOnName(model, TextureType.Normal, TextureType.Normal, "_normal", "_normal", modelPath, outputList);
            var materialMapOK = UpdateTextureBasedOnName(model, TextureType.Specular, TextureType.MaterialMap, "_specular", "_material_map", modelPath, outputList);
            var maskOK = UpdateTextureBasedOnName(model, TextureType.Mask, TextureType.Mask, "_mask", "_mask", modelPath, outputList);
            var baseColourOK = UpdateTextureBasedOnName(model, TextureType.Diffuse, TextureType.BaseColour, "_diffuse", "_base_colour", modelPath, outputList);

            if (normalOK && materialMapOK && maskOK && baseColourOK)
            {
                outputList.Ok($"{modelPath}-{model.Name}", $"Correctly updated by swapping texture names");
                return true;
            }
            return false;
        }

        bool UpdateTextureBasedOnName(Rmv2MeshNode model, TextureType oldType, TextureType newType, string originalTexturePostFix, string newTexturePostFix, string modelPath, ErrorList outputList)
        {
            var textures = model.GetTextures();
            if (textures.TryGetValue(oldType, out var texturePath))
            {
                var texureName = Path.GetFileNameWithoutExtension(texturePath);
                var baseTextureName = texureName.Replace(originalTexturePostFix, "");
                var wantedTextureName = baseTextureName + newTexturePostFix + ".dds";

                var matches = _pfs.SearchForFile(wantedTextureName);
                if (matches.Count == 0)
                {
                    outputList.Warning($"{modelPath}-{model.Name}", $"Unable to find matching texture for the {newType} channel");
                }
                else
                {
                    if (matches.Count != 1)
                        outputList.Warning($"{modelPath}-{model.Name}", $"Found multiple textures matches for the {newType} channel, picking the first {matches.First()}");

                    model.UpdateTexture(matches.First(), newType);
                    return true;
                }
            }

            return false;
        }

        private static bool AreTexturesWh3Format(Rmv2MeshNode model)
        {
            var textures = model.GetTextures();
            var hasBaseColour = textures.ContainsKey(TextureType.BaseColour);
            var hasMaterialMap = textures.ContainsKey(TextureType.MaterialMap);
            var hasNormal = textures.ContainsKey(TextureType.Normal);
            var hasMask = textures.ContainsKey(TextureType.Mask);

            if (hasBaseColour && hasMaterialMap && hasNormal && hasMask)
                return true;
            return false;
        }

        private static Bitmap BitMapFromTextureType(TextureType textureType, Dictionary<TextureType, Bitmap> textureDictionary)
        {
            var tempBitmap = textureDictionary[textureType];
            using var texDiffuse = new Bitmap(tempBitmap, tempBitmap.Width, tempBitmap.Height);

            return texDiffuse;
        }


        /// <summary>
        /// Get per-pixel metalness, from simple "rules of thumb" (Heuristics)
        /// </summary>
        private static float GetMetalNess(MS::Vector4 specularPixel, MS::Vector4 diffusePixel)
        {
            // calculate luminosity, to have a scalars to use in comparisons
            var luminosity_Specular = ColorHelper.GetPixelLuminosity(specularPixel);
            var luminosity_Diffuse = ColorHelper.GetPixelLuminosity(diffusePixel);

            // "rules" (heuristic) for creating metal map
            // TODO: improve, as it in some situations it doesn't look "pefect"
            // TODO: possibly include 'smoothness' to detetimne metalicity also
            if (luminosity_Specular > 0.3f)
            {
                return 1.0f;
            }

            if (luminosity_Diffuse < 0.1f)
            {
                return 1.0f;
            }

            // TODO: maybe re-enable, could be useful
            //if (specularPixel.X == specularPixel.Y && specularPixel.Y == specularPixel.Z)
            //  return 0.0f;

            if (luminosity_Specular < 0.1)
            {
                return 0.0f;
            }

            if (Math.Abs(luminosity_Diffuse - luminosity_Specular) < 0.15)
            {
                return 0.0f;
            }

            return 0.0f;
        }


        private bool ConvertTextureAdditiveBlending(Dictionary<TextureType, Bitmap> textureDictionary, string modelPath, Rmv2MeshNode model, ErrorList outputList)
        {
            var inputDiffuseTex = textureDictionary[TextureType.Diffuse];
            var inputSpecularTex = textureDictionary[TextureType.Specular];
            var inputGlosMapTex = textureDictionary[TextureType.Gloss];

            var outBaseColourTex = new Bitmap(inputDiffuseTex.Width, inputDiffuseTex.Height);
            var outMaterialMapTex = new Bitmap(inputDiffuseTex.Width, inputDiffuseTex.Height);

            for (var y = 0; y < inputDiffuseTex.Height; y++)
            {
                for (var x = 0; x < inputDiffuseTex.Width; x++)
                {
                    // get roughness from WH2 smoothness (gloss_map.r)
                    var roughness = GetRoughnessFromPixel(inputGlosMapTex, y, x);

                    var diffuseColorFloat = ColorHelper.ColorToVector4(inputDiffuseTex.GetPixel(x, y));
                    var spcularColorFloat = ColorHelper.ColorToVector4(inputSpecularTex.GetPixel(x, y));

                    var alphaDiffuse = diffuseColorFloat.W;

                    // Additive blending of specular and diffuse to get base_colour 
                    // might not always produce good results, EDIT: didn't!
                    var baseColorFloat = MS::Vector4.Clamp(
                            diffuseColorFloat + spcularColorFloat,
                            new MS::Vector4(0.0f, 0.0f, 0.0f, 0.0f),
                            new MS::Vector4(1.0f, 1.0f, 1.0f, 1.0f)
                        );

                    baseColorFloat = ColorHelper.PowVec4(baseColorFloat, 1.0f / 2.2f);
                    baseColorFloat.W = alphaDiffuse;

                    outBaseColourTex.SetPixel(x, y, ColorHelper.Vector4ToColor(baseColorFloat));

                    var metalness = GetMetalNess(spcularColorFloat, diffuseColorFloat);

                    outMaterialMapTex.SetPixel(x, y,
                        ColorHelper.Vector4ToColor(new MS::Vector4(metalness, roughness, 0.0f, 1.0f)));

                }
            }

            // Do the texture replacement operations
            var packFilePathBaseColor = model.GetTextures()[TextureType.Diffuse].Replace("_diffuse", "_base_colour");
            var packFilePath_MaterialMap = model.GetTextures()[TextureType.Gloss].Replace("_gloss_map", "_material_map");

            var result = SaveAndApplyBitmapAsModelTexture(outBaseColourTex, packFilePathBaseColor, TextureType.BaseColour, modelPath, model, outputList);
            if (result)
                outputList.Ok($"{modelPath}-{model.Name}", $"Generated new 'base_colour' texture");

            var result_material_map = SaveAndApplyBitmapAsModelTexture(outMaterialMapTex, packFilePath_MaterialMap, TextureType.MaterialMap, modelPath, model, outputList);
            if (result_material_map)
                outputList.Ok($"{modelPath}-{model.Name}", $"Generated new `material_map texture");

            return result;
        }

        /// <summary>
        /// Obtain WH3 roughness from WH2 gloss (smoothness)
        /// </summary>
        private static float GetRoughnessFromPixel(Bitmap inputGlosMapTex, int y, int x)
        {
            const float smooth_gamma_scale = 1.0f / 0.90f; // rough const test roughness adjuster value

            var smoothness = Math.Clamp(inputGlosMapTex.GetPixel(x, y).R / 255.0f, 0.0f, 1.0f);
            var roughness = Math.Clamp(1.0f - smoothness, 0.0f, 1.0f);

            roughness = (float)Math.Pow(roughness, 1.0 / 2.2 * smooth_gamma_scale);

            return roughness;

        }

        private bool ConvertTexturesUsingComparativeBlending(Dictionary<TextureType, Bitmap> textureDictionary, string modelPath, Rmv2MeshNode model, ErrorList outputList)
        {
            var inputDiffuseTex = textureDictionary[TextureType.Diffuse];
            var inputSpecularTex = textureDictionary[TextureType.Specular];
            var inputGlosMapTex = textureDictionary[TextureType.Gloss];

            var outBaseColourTex = new Bitmap(inputDiffuseTex.Width, inputDiffuseTex.Height);
            var outMaterialMapTex = new Bitmap(inputDiffuseTex.Width, inputDiffuseTex.Height);


            for (var y = 0; y < inputDiffuseTex.Height; y++)
            {
                for (var x = 0; x < inputDiffuseTex.Width; x++)
                {

                    var roughness = GetRoughnessFromPixel(inputGlosMapTex, y, x);

                    var diffusePixelFloat = ColorHelper.ColorToVector4(inputDiffuseTex.GetPixel(x, y));

                    var alphaDiffuse = diffusePixelFloat.W; // save the the diffuse alpha for later.
                    var specularPixelFloat = ColorHelper.ColorToVector4(inputSpecularTex.GetPixel(x, y));

                    // get metalmess before (any potential) gamma stuff is done on the pixels
                    var metalnessValue = GetMetalNess(specularPixelFloat, diffusePixelFloat);

                    // Get "brightness" (luminosity) from spec amd diffuse, 
                    // reason: to obtain a scalar (float) to decribe the RGB, that can be used in comparisions....
                    var luminosity_Specular = ColorHelper.GetPixelLuminosity(specularPixelFloat);
                    var luminosity_Diffuse = ColorHelper.GetPixelLuminosity(diffusePixelFloat);

                    // Taking the pixel from spec or gloss, based on which is brightest (luminosioty),
                    // with a slight adjustment, to include a tiny bit more diffuse
                    // this MIGHT work in most cases:               
                    const float specular_threshold_offet = 0.07f;
                    var baseColorPixelFloat =
                        luminosity_Specular - specular_threshold_offet >= luminosity_Diffuse ? specularPixelFloat : diffusePixelFloat;

                    // adjust base_colour brightness
                    const float brightNess = 1.3f; // rough const test pre-gamma brightness adjusted (evil:))
                    baseColorPixelFloat = ColorHelper.linear_to_gamma_accurate(baseColorPixelFloat * brightNess);
                    baseColorPixelFloat.W = alphaDiffuse; // store the alpha value read from the diffuse

                    // store pixels
                    outBaseColourTex.SetPixel(x, y, ColorHelper.Vector4ToColor(baseColorPixelFloat));
                    outMaterialMapTex.SetPixel(x, y, ColorHelper.Vector4ToColor(new MS::Vector4(metalnessValue, roughness, 0.0f, 1.0f)));

                }
            }

            // Do the texture replacement operations            
            var packFilePath_Mask = model.GetTextures()[TextureType.Mask];
            var packFilePathBaseColor = model.GetTextures()[TextureType.Diffuse].Replace("_diffuse", "_base_colour");
            var packFilePath_MaterialMap = model.GetTextures()[TextureType.Gloss].Replace("_gloss_map", "_material_map");

            var result = SaveAndApplyBitmapAsModelTexture(outBaseColourTex, packFilePathBaseColor, TextureType.BaseColour, modelPath, model, outputList);
            if (result)
                outputList.Ok($"{modelPath}-{model.Name}", $"Generated new 'base_colour' texture (comparitive blending)");

            var result_material_map = SaveAndApplyBitmapAsModelTexture(outMaterialMapTex, packFilePath_MaterialMap, TextureType.MaterialMap, modelPath, model, outputList);
            if (result_material_map)
                outputList.Ok($"{modelPath}-{model.Name}", $"Generated new `material_map texture (heuristic metalmask, inverted gloss");

            return result;
        }


        Bitmap ConvertTextureToByte(TextureType textureType, string modelPath, Rmv2MeshNode model, ErrorList outputList)
        {
            var textures = model.GetTextures();
            if (textures.TryGetValue(textureType, out var texturePath) == false)
            {
                outputList.Error($"{modelPath}-{model.Name}", $"Does not have a {textureType} map, convertion failed");
                return null;
            }

            var texturePf = _pfs.FindFile(texturePath);
            if (texturePf == null)
            {
                outputList.Error($"{modelPath}-{model.Name}", $"Does not have a valid {textureType} file, convertion failed");
                return null;
            }

            var imageName = Guid.NewGuid() + Path.GetFileNameWithoutExtension(texturePath) + ".png";
            var outputPath = _outputFolder + imageName;
            if (TextureConverter.SaveAsPNG(texturePf, outputPath) == false)
            {
                outputList.Error($"{modelPath}-{model.Name}", $"'{model.Name}' failed to create PNG for {textureType}, convertion failed");
                return null;
            }

            var image = Image.FromStream(new MemoryStream(File.ReadAllBytes(outputPath))) as Bitmap;
            return image;
        }

        bool SaveAndApplyBitmapAsModelTexture(Bitmap bitmap, string packFilePath, TextureType outputTextureType, string modelPath, Rmv2MeshNode model, ErrorList outputList)
        {
            try
            {
                var diskPath = _outputFolder + "tempImage_" + Guid.NewGuid() + ".png";
                if (File.Exists(diskPath))
                    File.Delete(diskPath);

                bitmap.Save(diskPath, ImageFormat.Png);
                TextureConverter.LoadTexture(_pfs, packFilePath, diskPath, outputTextureType);
                model.UpdateTexture(packFilePath, outputTextureType, true);

                if (File.Exists(diskPath))
                    File.Delete(diskPath);
            }
            catch (Exception e)
            {
                outputList.Error($"{modelPath}-{model.Name}", $"Converting from png to dds failed for {outputTextureType}. Reason: {e.Message}");
                return false;
            }

            return true;
        }


        private void DeleteWh2TextureReferences(Rmv2MeshNode model)
        {
            if (_deleteOldTextures == false)
                return;
            model.UpdateTexture("", TextureType.Specular);
            model.UpdateTexture("", TextureType.Diffuse);
            model.UpdateTexture("", TextureType.Gloss);
        }

        static class ColorHelper
        {
            /// <summary>
            /// roughly equilvant to 'x2 = pow(x1, 1/2.2)', just much more precise color-space-wise
            /// </summary>
            public static float gamma_accurate_component(float linear_val)
            {
                const float srgb_gamma_ramp_inflection_point = 0.0031308f;

                if (linear_val <= srgb_gamma_ramp_inflection_point)
                {
                    return 12.92f * linear_val;
                }
                else
                {
                    const float a = 0.055f;

                    return (1.0f + a) * (float)Math.Pow(linear_val, 1.0 / 2.4) - a;
                }
            }

            /// <summary>
            /// roughly equilvant to 'x2 = power(x1, 2.2)', just much more precise color-space-wise
            /// </summary>
            public static float linear_accurate_component(float srgb_val)
            {
                const float inflection_point = 0.04045f;

                if (srgb_val <= inflection_point)
                {
                    return srgb_val / 12.92f;
                }
                else
                {
                    const float a = 0.055f;
                    return (float)Math.Pow((srgb_val + a) / (1.0f + a), 2.4f);
                }
            }

            /// <summary>
            /// Does per component "pow" on a Vector4
            /// </summary>
            static public MS::Vector4 PowVec4(MS::Vector4 v, float e)
            {
                return new MS::Vector4(
                    (float)Math.Pow(v.X, e),
                    (float)Math.Pow(v.Y, e),
                    (float)Math.Pow(v.Z, e),
                    (float)Math.Pow(v.W, e)
                );
            }

            /// <summary>
            /// Gamma to linear for Vector4, does not affect alpha
            /// </summary>
            public static MS::Vector4 gamaa_to_linear_accurate(MS::Vector4 fGamma)
            {
                return new MS::Vector4(linear_accurate_component(fGamma.X), linear_accurate_component(fGamma.Y), linear_accurate_component(fGamma.Z), fGamma.W);
            }

            /// <summary>
            /// Linear to Gamma for Vector4, does not affect alpha
            /// </summary>
            public static MS::Vector4 linear_to_gamma_accurate(MS::Vector4 _vPixel)
            {

                return new MS::Vector4(
                    gamma_accurate_component(_vPixel.X),
                    gamma_accurate_component(_vPixel.Y),
                    gamma_accurate_component(_vPixel.Z),
                    _vPixel.W
               );
            }

            /// <summary>
            /// Converts RGBA8888 system.draw.Color to UNORM float4 (Vector4)
            /// </summary>
            public static MS::Vector4 ColorToVector4(Color c)
            {
                return new MS::Vector4(
                    Math.Clamp(c.R / 255.0f, 0.0f, 1.0f),
                    Math.Clamp(c.G / 255.0f, 0.0f, 1.0f),
                    Math.Clamp(c.B / 255.0f, 0.0f, 1.0f),
                    Math.Clamp(c.A / 255.0f, 0.0f, 1.0f)

                );
            }

            /// <summary>
            /// Converts UNORM float4 (Vector4) to RGBA8888 system.draw.Color 
            /// </summary>
            public static Color Vector4ToColor(MS::Vector4 v)
            {
                return Color.FromArgb(
                    (int)Math.Clamp(v.W * 255.0f, 0.0f, 255.0f),
                    (int)Math.Clamp(v.X * 255.0f, 0.0f, 255.0f),
                    (int)Math.Clamp(v.Y * 255.0f, 0.0f, 255.0f),
                    (int)Math.Clamp(v.Z * 255.0f, 0.0f, 255.0f)

                );

            }

            /// <summary>
            /// Get pixel luminosity from a Vector4 (in UNORM float32_4)
            /// </summary>            
            public static float GetPixelLuminosity(MS::Vector4 vColor)
            {
                return 0.2126f * vColor.X + 0.7152f * vColor.Y + 0.0722f * vColor.Z;
            }

        }

    }*/
}
