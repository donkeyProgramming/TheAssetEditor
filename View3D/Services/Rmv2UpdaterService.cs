using CommonControls.BaseDialogs.ErrorListDialog;
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

namespace View3D.Services
{
    public class Rmv2UpdaterService
    {
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

        public void UpdateWh2Models(string modelPath, List<Rmv2MeshNode> models, out ErrorListViewModel.ErrorList outputList)
        {
            outputList = new ErrorListViewModel.ErrorList();
            foreach (var model in models)
                ProcessModel(modelPath, model, outputList);
        }

        private void ProcessModel(string modelPath, Rmv2MeshNode model, ErrorListViewModel.ErrorList outputList)
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
                        if (textures.ContainsKey(TextureType.BaseColour) == false)
                            CreateBaseColourMap(textureDictionary, modelPath, model, outputList);

                        if (textures.ContainsKey(TextureType.MaterialMap) == false)
                            CreateMaterialMap(textureDictionary, modelPath, model, outputList);
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

        private bool CreateBaseColourMap(Dictionary<TextureType, Bitmap> textureDictionary, string modelPath, Rmv2MeshNode model, ErrorListViewModel.ErrorList outputList)
        {
            var diffuse = textureDictionary[TextureType.Diffuse];
            using var baseColourMap = new Bitmap(diffuse.Width, diffuse.Height);

            // ---------------
            // Update the texture...
            // baseColourMap.SetPixel(1, 1, Color.FromArgb(255, 255, 255, 255));
            // baseColourMap.GetPixel(1, 1,);
            // ---------------

            var packFilePath = model.GetTextures()[TextureType.Diffuse].Replace("_diffuse", "_basecolour");
            var result = SaveAndApplyBitmapAsModelTexture(baseColourMap, packFilePath, TextureType.BaseColour, modelPath, model, outputList);
            if(result)
                outputList.Ok($"{modelPath}-{model.Name}", $"Generated new base colour map");
            return result;
        }

        private bool CreateMaterialMap(Dictionary<TextureType, Bitmap> textureDictionary, string modelPath, Rmv2MeshNode model, ErrorListViewModel.ErrorList outputList)
        {
            var diffuse = textureDictionary[TextureType.Diffuse];
            using var materialMap = new Bitmap(diffuse.Width, diffuse.Height);

            // ---------------
            // Update the texture...
            // ---------------

            var packFilePath = model.GetTextures()[TextureType.Diffuse].Replace("_diffuse", "_materialmap");
            var result = SaveAndApplyBitmapAsModelTexture(materialMap, packFilePath, TextureType.MaterialMap, modelPath, model, outputList);
            if (result)
                outputList.Ok($"{modelPath}-{model.Name}", $"Generated new material map");
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
