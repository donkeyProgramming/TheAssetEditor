﻿using CommonControls.BaseDialogs.ErrorListDialog;
using CommonControls.Common;
using CommonControls.FileTypes.RigidModel.Types;
using CommonControls.Services;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using View3D.SceneNodes;
using View3D.Utility;


using MS = Microsoft.Xna.Framework;


class ColorHelperService
{

    public MS::Vector4 gamaa_to_linear_accurate(MS::Vector4 fGamma)
    {
        return new MS::Vector4(linear_accurate_component(fGamma.X), linear_accurate_component(fGamma.Y), linear_accurate_component(fGamma.Z), 1.0f);
    }
    public MS::Vector4 linear_to_gamma_accurate(MS::Vector4 _vPixel)
    {

        return new MS::Vector4(
            gamma_accurate_component(_vPixel.X),
            gamma_accurate_component(_vPixel.Y),
            gamma_accurate_component(_vPixel.Z), 1.0f);

    }

    public float gamma_accurate_component(float linear_val)
    {
        const float srgb_gamma_ramp_inflection_point = 0.0031308f;

        if (linear_val <= srgb_gamma_ramp_inflection_point)
        {
            return 12.92f * linear_val;
        }
        else
        {
            const float a = 0.055f;

            return ((1.0f + a) * (float)Math.Pow(linear_val, 1.0 / 2.4)) - a;
        }
    }

    public float linear_accurate_component(float srgb_val)
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

}



namespace View3D.Services
{
    

    public class Rmv2UpdaterService
    {

        public enum ConversionTechniqueEnum
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

        public void UpdateWh2Models(string modelPath, List<Rmv2MeshNode> models, ConversionTechniqueEnum conversionTechnique, out ErrorListViewModel.ErrorList outputList)
        {
            outputList = new ErrorListViewModel.ErrorList();
            foreach (var model in models)
                ProcessModel(modelPath, model, conversionTechnique, outputList);
        }

        private void ProcessModel(string modelPath, Rmv2MeshNode model, ConversionTechniqueEnum conversionTechnique, ErrorListViewModel.ErrorList outputList)
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
                            case ConversionTechniqueEnum.AdditiveBlending:
                                ConvertAdditiveBlending(textureDictionary, modelPath, model, outputList);
                                break;

                            case ConversionTechniqueEnum.ComparativeBlending:
                                ConvertUsingcomparativeBlending(textureDictionary, modelPath, model, outputList);
                                break;
                        }

                        //if (textures.ContainsKey(TextureType.BaseColour) == false)
                        

                        //if (textures.ContainsKey(TextureType.MaterialMap) == false)
                        //    CreateMaterialMap(textureDictionary, modelPath, model, outputList);
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


        private bool MatchMissingTexturesByName(string modelPath, Rmv2MeshNode model, ErrorListViewModel.ErrorList outputList)
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

        bool UpdateTextureBasedOnName(Rmv2MeshNode model, TextureType oldType, TextureType newType, string originalTexturePostFix, string newTexturePostFix, string modelPath, ErrorListViewModel.ErrorList outputList)
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

        private static MS::Vector4 ColorToVector4(Color c)
        {            
            return new MS::Vector4(
                Math.Clamp(((float)c.R) / 255.0f, 0.0f, 1.0f),
                Math.Clamp(((float)c.G) / 255.0f, 0.0f, 1.0f),
                Math.Clamp(((float)c.B) / 255.0f, 0.0f, 1.0f),
                Math.Clamp(((float)c.A) / 255.0f, 0.0f, 1.0f)

            );
        }

        private static Color Vector4ToColor(MS::Vector4 v)
        {
            return Color.FromArgb(
                (int)Math.Clamp((float)(v.W) * 255.0f, 0.0f, 255.0f),
                (int)Math.Clamp((float)(v.X) * 255.0f, 0.0f, 255.0f),
                (int)Math.Clamp((float)(v.Y) * 255.0f, 0.0f, 255.0f),
                (int)Math.Clamp((float)(v.Z) * 255.0f, 0.0f, 255.0f)

            );

        }

        static float GetPixelLuminosity(MS::Vector4 vColor)
        {
            return (0.2126f * vColor.X + 0.7152f * vColor.Y + 0.0722f * vColor.Z);
        }



        private static float GetMetalNess(MS::Vector4 specularPixel, MS::Vector4 diffusePixel)
        {
            // calculate luminosity
            float luminosity_Specular = GetPixelLuminosity(specularPixel);
            float luminosity_Diffuse = GetPixelLuminosity(diffusePixel);


            // "rules" (heuristic) for creating metal map
            // TODO: improve, as it in some situations it doesn't look "pefect"
            // TODO: possibly include 'smoothness' to detetimne metalicity also
            if (luminosity_Specular > 0.3f)
                return 1.0f;



            if (luminosity_Diffuse < 0.1f)
            {
                return 1.0f;
            }
                
            //if (specularPixel.X == specularPixel.Y && specularPixel.Y == specularPixel.Z)
            //  return 0.0f;

            if (luminosity_Specular < 0.1)
                return 0.0f;

            if (Math.Abs(luminosity_Diffuse - luminosity_Specular) < 0.15)
                return 0.0f;


            return 0.0f;
        }



        static private MS::Vector4 PowVec4(MS::Vector4 v, float e)
        {
            return new MS::Vector4(
                (float)Math.Pow(v.X, e),
                (float)Math.Pow(v.Y, e),
                (float)Math.Pow(v.Z, e),
                (float)Math.Pow(v.W, e)
                
            );
        }

        // TODO: To Ole:
        // this method was meant to create the base_colour, but it creates both the base_color and material map        // 
        // They are somewhat interpendent, but could possibly be split up, 
        // should I refactor and do it like you intended?
        private bool ConvertAdditiveBlending(Dictionary<TextureType, Bitmap> textureDictionary, string modelPath, Rmv2MeshNode model, ErrorListViewModel.ErrorList outputList)
        {
            
            var inputDiffuseTex = textureDictionary[TextureType.Diffuse];
            var inputSpecularTex = textureDictionary[TextureType.Specular];
            var inputGlosMapTex = textureDictionary[TextureType.Gloss];

            var outBaseColourTex = new Bitmap(inputDiffuseTex.Width, inputDiffuseTex.Height);
            var outMaterialMapTex = new Bitmap(inputDiffuseTex.Width, inputDiffuseTex.Height);                        

            for (int y = 0; y < inputDiffuseTex.Height; y++)
            {
                for (int x = 0; x < inputDiffuseTex.Width; x++)
                {
                    // get roughness from WH2 smoothness (gloss_map.r)
                    float smoothness = Math.Clamp((float)(inputGlosMapTex.GetPixel(x, y).R) / 255.0f, 0.0f, 1.0f);
                    smoothness = (float)Math.Pow(smoothness, 2.0 / 1.0);
                    float roughness = Math.Clamp(1.0f - smoothness, 0.0f, 1.0f);

                    var diffuseColorFloat = ColorToVector4(inputDiffuseTex.GetPixel(x, y));
                    var spcularColorFloat = ColorToVector4(inputSpecularTex.GetPixel(x, y));

                    var alphaDiffuse = diffuseColorFloat.W;

                    // Additive blending of specular and diffuse to get base_colour 
                    // might not always produce good results
                    var baseColorFloat = MS::Vector4.Clamp(
                            diffuseColorFloat + spcularColorFloat,
                            new MS::Vector4(0.0f, 0.0f, 0.0f, 0.0f),
                            new MS::Vector4(1.0f, 1.0f, 1.0f, 1.0f)
                        );

                    baseColorFloat = PowVec4(baseColorFloat, 1.0f / 2.2f);
                    baseColorFloat.W = alphaDiffuse;

                    outBaseColourTex.SetPixel(x, y, Vector4ToColor(baseColorFloat));

                    var metalness = GetMetalNess(spcularColorFloat, diffuseColorFloat);
                    outMaterialMapTex.SetPixel(x, y,
                        Vector4ToColor(new MS::Vector4(metalness, roughness, 0.0f, 1.0f)));

                    // TODO REMOVE: 
                    // Debuggin code should NOT be removed yeat, as the algo is still being fine tuned
                    // BEGIN: DEBUGGIN CODE
                    if (false)
                    {
                        outBaseColourTex.SetPixel(x, y, Vector4ToColor(new MS::Vector4(roughness, roughness, roughness, 1.0f)));
                        outMaterialMapTex.SetPixel(x, y,Vector4ToColor(new MS::Vector4(0.0f, 1.0f, 0.0f, 1.0f)));
                    }
                    // END: DEBUGGIN CODE
                }
            }

            // do the replacement, 
            
            // TODO: to Ole:
            // it is: '_base_colour' and 'material_map', you had written '_materialmap' and '_basecolour' :)
            // this gives errors around the program, in the converter window, it seams
            var packFilePathBaseColor = model.GetTextures()[TextureType.Diffuse].Replace("_diffuse", "_base_colour");
            var packFilePath_MaterialMap = model.GetTextures()[TextureType.Gloss].Replace("_gloss_map", "_material_map");

            var result = SaveAndApplyBitmapAsModelTexture(outBaseColourTex, packFilePathBaseColor, TextureType.BaseColour, modelPath, model, outputList);
            if(result)
                outputList.Ok($"{modelPath}-{model.Name}", $"Generated new 'base_colour' texture");

            var result_material_map = SaveAndApplyBitmapAsModelTexture(outMaterialMapTex, packFilePath_MaterialMap, TextureType.MaterialMap, modelPath, model, outputList);
            if(result_material_map)
                outputList.Ok($"{modelPath}-{model.Name}", $"Generated new `material_map texture");

            return result;
        }

        // TODO: To Ole:
        // this method was meant to create the material_map, 
        //  but the material map and basecolor are somewhat interpendent, CAN be split up, MAYBE detrimental down the line
        // should I refactor and do it like you intneded?
        private bool ConvertUsingcomparativeBlending(Dictionary<TextureType, Bitmap> textureDictionary, string modelPath, Rmv2MeshNode model, ErrorListViewModel.ErrorList outputList)
        {


            var colorService = new ColorHelperService();

            var inputDiffuseTex = textureDictionary[TextureType.Diffuse];
            var inputSpecularTex = textureDictionary[TextureType.Specular];
            var inputGlosMapTex = textureDictionary[TextureType.Gloss];

            var outBaseColourTex = new Bitmap(inputDiffuseTex.Width, inputDiffuseTex.Height);
            var outMaterialMapTex = new Bitmap(inputDiffuseTex.Width, inputDiffuseTex.Height);

            for (int y = 0; y < inputDiffuseTex.Height; y++)
            {
                for (int x = 0; x < inputDiffuseTex.Width; x++)
                {
                    // get roughness from WH2 smoothness (gloss_map.r) (uint8)
                    float gloss_temp = (float)(inputGlosMapTex.GetPixel(x, y).R);

                    // convert to float32_UNORM
                    float smoothness = gloss_temp / 255.0f;

                    // get roughness
                    float rougnness = 1.0f - smoothness;


                    var diffusePixelFloat = ColorToVector4(inputDiffuseTex.GetPixel(x, y));

                    var alphaDiffuse = diffusePixelFloat.W;
                    var specularPixelFloat = ColorToVector4(inputSpecularTex.GetPixel(x, y));

                    // get metalmess before gamma stuff is done on the pixelse
                    var metalnessValue = GetMetalNess(specularPixelFloat, diffusePixelFloat);


                    // conver to gamma space
                    //diffusePixelFloat = colorService.linear_to_gamma_accurate(diffusePixelFloat);
                    //specularPixelFloat = colorService.linear_to_gamma_accurate(specularPixelFloat);


                    // convert textures to "linear color space"
                    //specularPixelFloat = PowVec4(specularPixelFloat, 1.0f / 2.2f);
                    //diffusePixelFloat = PowVec4(diffusePixelFloat, 1.0f / 2.2f);


                    // get "brightness" from spec amd diffuse
                    var luminosity_Specular = GetPixelLuminosity(specularPixelFloat);
                    var luminosity_Diffuse = GetPixelLuminosity(diffusePixelFloat);

                    // Taking the pixel from spec or gloss, based on which is brightest (luminosioty), with a slight adjustmen
                    // this MIGHT work in most cases;                   
                    var baseColorPixelFloat = (luminosity_Specular - 0.07 >= luminosity_Diffuse) ? specularPixelFloat : diffusePixelFloat;


                    //baseColorPixelFloat = PowVec4(baseColorPixelFloat, 1.0f/2.2f);
                    const float brightNess = 1.2f;
                    baseColorPixelFloat = colorService.linear_to_gamma_accurate(baseColorPixelFloat* brightNess);

                    //const float Gamma = 1.2f;
                    //baseColorPixelFloat = PowVec4(baseColorPixelFloat, 1.0f / Gamma);


                    baseColorPixelFloat.W = alphaDiffuse; // store the alpha value read from the diffuse

                    outBaseColourTex.SetPixel(x, y, Vector4ToColor(baseColorPixelFloat));

                    outMaterialMapTex.SetPixel(x, y,
                        Vector4ToColor(new MS::Vector4(metalnessValue, rougnness, 0.0f, 1.0f)));

                }
            }

            // do the replacement, 

            // TODO: to Ole:
            // it is: '_base_colour' and 'material_map', you had written '_materialmap' and '_basecolour' :)
            // this gives errors around the program, in the converter window, it seams
            var packFilePathBaseNormal = model.GetTextures()[TextureType.Normal];
            var packFilePath_Mask = model.GetTextures()[TextureType.Mask];
            var packFilePathBaseColor = model.GetTextures()[TextureType.Diffuse].Replace("_diffuse", "_base_colour");
            var packFilePath_MaterialMap = model.GetTextures()[TextureType.Gloss].Replace("_gloss_map", "_material_map");


            //// remove spec-gloss textuiures67   
            //model.GetTextures().Clear();
            //model.GetTextures().update();
            //model.GetTextures().Add(TextureType.Normal, packFilePathBaseNormal);
            //model.GetTextures().Add(TextureType.Mask, packFilePath_Mask);

            var result = SaveAndApplyBitmapAsModelTexture(outBaseColourTex, packFilePathBaseColor, TextureType.BaseColour, modelPath, model, outputList);
            if (result)
                outputList.Ok($"{modelPath}-{model.Name}", $"Generated new 'base_colour' texture (comparitive blending)");

            var result_material_map = SaveAndApplyBitmapAsModelTexture(outMaterialMapTex, packFilePath_MaterialMap, TextureType.MaterialMap, modelPath, model, outputList);
            if (result_material_map)
                outputList.Ok($"{modelPath}-{model.Name}", $"Generated new `material_map texture (heuristic metalmask, inverted gloss");

            return result;
        }


        Bitmap ConvertTextureToByte(TextureType textureType, string modelPath, Rmv2MeshNode model, ErrorListViewModel.ErrorList outputList)
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

        bool SaveAndApplyBitmapAsModelTexture(Bitmap bitmap, string packFilePath, TextureType outputTextureType, string modelPath, Rmv2MeshNode model, ErrorListViewModel.ErrorList outputList)
        {
            try
            {
                var diskPath = _outputFolder + "tempImage_" + Guid.NewGuid() +".png";
                if (File.Exists(diskPath))
                    File.Delete(diskPath);

                bitmap.Save(diskPath, ImageFormat.Png);
                TextureConverter.LoadTexture(_pfs, packFilePath, diskPath, outputTextureType);
                model.UpdateTexture(packFilePath, outputTextureType, true);

                if (File.Exists(diskPath))
                    File.Delete(diskPath);
            }
            catch(Exception e)
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
    }
}
