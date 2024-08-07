﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using GameWorld.Core.SceneNodes;
using Serilog;
using Shared.Core.ErrorHandling;
using Shared.Core.PackFiles;
using Shared.Core.Services;
using Shared.EmbeddedResources;
using Shared.GameFormats.RigidModel;
using Shared.GameFormats.RigidModel.Types;
using Shared.GameFormats.WsModel;

namespace GameWorld.Core.Services.SceneSaving.Material.Strategies
{
    public class WsModelGeneratorService
    {
        private readonly ILogger _logger = Logging.Create<WsModelGeneratorService>();
        private readonly PackFileService _packFileService;
        private readonly Dictionary<string, WsModelMaterialFile> _existingMaterials;

        private static readonly Dictionary<string, TextureType> TemplateStringToTextureTypes = new()
        {
            {"BASE_COLOUR_PATH", TextureType.BaseColour},
            {"MATERIAL_MAP", TextureType.MaterialMap},
            {"NORMAL_PATH", TextureType.Normal},
            {"MASK_PATH", TextureType.Mask},
            {"DIFFUSE_PATH", TextureType.Diffuse },
            {"GLOSS_PATH", TextureType.Gloss },
            {"SPECULAR_PATH", TextureType.Specular },
        };

        public WsModelGeneratorService(PackFileService packFileService)
        {
            _packFileService = packFileService;
            _existingMaterials = LoadAllExistingMaterials();
        }

        public void GenerateWsModel(string modelFilePath, MainEditableNode mainNode, GameTypeEnum game = GameTypeEnum.Warhammer3)
        {
            try
            {
                if (_packFileService.GetEditablePack() == null)
                {
                    MessageBox.Show("No editable pack selected", "error");
                    return;
                }

                var wsModelPath = Path.ChangeExtension(modelFilePath, ".wsmodel");

                var materialTemplate = game switch
                {
                    GameTypeEnum.Warhammer3 => ResourceLoader.LoadString("Resources.WsModelTemplates.MaterialTemplate_wh3.xml.material"),
                    GameTypeEnum.Warhammer2 => ResourceLoader.LoadString("Resources.WsModelTemplates.MaterialTemplate_wh2.xml.material"),
                    GameTypeEnum.Pharaoh => ResourceLoader.LoadString("Resources.WsModelTemplates.MaterialTemplate_pharaoh.xml.material"),
                    _ => throw new Exception("Unknown game - unable to generate ws model")
                };
                var wsModelData = CreateWsModel(mainNode, game, modelFilePath, materialTemplate);
                var existingWsModelFile = _packFileService.FindFile(wsModelPath, _packFileService.GetEditablePack());
                SaveHelper.Save(_packFileService, wsModelPath, existingWsModelFile, Encoding.UTF8.GetBytes(wsModelData));
            }
            catch (Exception e)
            {
                _logger.Here().Error("Error generating ws model - " + e.Message);
                MessageBox.Show("Generation failed!");
            }
        }

        string CreateWsModel(MainEditableNode mainNode, GameTypeEnum game, string modelFilePath, string materialTemplate)
        {
            var sb = new StringBuilder();

            sb.Append("<model version=\"1\">\n");
            sb.Append($"\t<geometry>{modelFilePath}</geometry>\n");
            sb.Append("\t\t<materials>\n");

            var lodNodes = mainNode.GetLodNodes();
            for (var lodIndex = 0; lodIndex < lodNodes.Count; lodIndex++)
            {
                var meshes = mainNode.GetMeshesInLod(lodIndex, false);
                var uniqueNames = GenerateUniqueNames(meshes);
                for (var meshIndex = 0; meshIndex < meshes.Count; meshIndex++)
                {
                    var materialFile = GetOrCreateMaterial(modelFilePath, game, meshes[meshIndex], uniqueNames[meshIndex], materialTemplate);
                    sb.Append($"\t\t\t<material lod_index=\"{lodIndex}\" part_index=\"{meshIndex}\">");
                    sb.Append(materialFile);
                    sb.Append("</material>\n");
                }

                sb.AppendLine();
            }

            sb.Append("\t</materials>\n");
            sb.Append("</model>\n");

            return sb.ToString();
        }

        private List<string> GenerateUniqueNames(List<Rmv2MeshNode> meshes)
        {
            var output = new List<string>();
            foreach (var mesh in meshes)
            {
                var fileName = mesh.Name;
                var modelFullPath = mesh.OriginalFilePath;
                //if mesh name is blank, grab a mesh name from the file path, so far the only
                //total war with this issue appears to be Pharaoh.
                if (String.IsNullOrWhiteSpace(fileName))
                {
                    modelFullPath = Path.GetFileNameWithoutExtension(fileName);
                }

                for (var index = 0; index < 1024; index++)
                {
                    var name = index == 0 ? fileName : string.Format("{0}_{1}", fileName, index);
                    if (output.Contains(name))
                        continue;

                    fileName = name;
                    break;
                }

                output.Add(fileName);
            }

            return output;
        }

        string GetOrCreateMaterial(string modelFilePath, GameTypeEnum game, Rmv2MeshNode mesh, string uniqueName, string materialTemplate)
        {
            uniqueName = uniqueName.Trim();
            var materialFileName = FindApplicableExistingMaterial(game, mesh);
            if (materialFileName == null)
                materialFileName = CreateNewMaterial(modelFilePath, mesh, uniqueName, materialTemplate, game);
            return materialFileName;
        }

        string CreateNewMaterial(string modelFilePath, Rmv2MeshNode mesh, string uniqueName, string materialTemplate, GameTypeEnum game)
        {
            var vertexType = ModelMaterialEnumHelper.GetToolVertexFormat(mesh.Material.BinaryVertexFormat);
            var alphaOn = mesh.Material.AlphaMode != AlphaMode.Opaque;

            var shaderNamePart = (vertexType, game) switch
            {
                (UiVertexFormat.Cinematic, GameTypeEnum.Pharaoh) => "weighted_standard_4",
                (UiVertexFormat.Weighted, GameTypeEnum.Pharaoh) => "weighted_standard_2",
                (UiVertexFormat.Static, _) => "rigid",
                (UiVertexFormat.Cinematic, _) when game != GameTypeEnum.Pharaoh => "weighted4",
                (UiVertexFormat.Weighted, _) when game != GameTypeEnum.Pharaoh => "weighted2",
                _ => throw new Exception("Unknown vertex type")
            };

            // Update the shader name
            var shaderAlphaStr = "";
            if (alphaOn)
                shaderAlphaStr = "_alpha";
            var shaderName = "";
            if (game != GameTypeEnum.Pharaoh)
            {
                shaderName = $"shaders/{shaderNamePart}_character{shaderAlphaStr}.xml.shader";
            }
            else
            {
                shaderName = $"shaders/system/{shaderNamePart}.xml.shader";

            }
            materialTemplate = materialTemplate.Replace("SHADER_PATH", shaderName);

            // Update the textures
            foreach (var (replacment, textureType) in TemplateStringToTextureTypes)
            {
                var texture = mesh.Material.GetTexture(textureType);
                if (texture.HasValue)
                {
                    materialTemplate = materialTemplate.Replace(replacment, texture.Value.Path);
                    Log.Write(Serilog.Events.LogEventLevel.Information, $"writing {replacment} {textureType} {texture.Value.Path}");
                }
                else
                    materialTemplate.Replace(replacment, "test_mask.dds");
            }

            // Save the new file
            var fileName = "";
            if (game != GameTypeEnum.Pharaoh)
            {
                fileName = uniqueName + "_" + shaderNamePart + "_alpha_" + (alphaOn ? "on" : "off") + ".xml";
            }
            else
            {
                fileName = uniqueName + "_" + shaderNamePart + ".xml";
            }

            materialTemplate = materialTemplate.Replace("FILE_NAME", fileName);

            var dir = Path.GetDirectoryName(modelFilePath);
            var fullPath = dir + "\\materials\\" + fileName + ".material";
            SaveHelper.Save(_packFileService, fullPath, null, Encoding.UTF8.GetBytes(materialTemplate), false);

            return fullPath;
        }

        string FindApplicableExistingMaterial(GameTypeEnum game, Rmv2MeshNode mesh)
        {
            foreach (var material in _existingMaterials)
            {
                var isMatch = IsMaterialMatch(game, mesh, material.Value);
                if (isMatch)
                    return material.Key;
            }

            return null;
        }

        static bool IsMaterialMatch(GameTypeEnum game, Rmv2MeshNode mesh, WsModelMaterialFile material)
        {
            var vertexType = ModelMaterialEnumHelper.GetToolVertexFormat(mesh.Material.BinaryVertexFormat);
            if (vertexType != material.VertexType)
                return false;

            var alphaOn = mesh.Material.AlphaMode != AlphaMode.Opaque;
            if (alphaOn && material.Alpha == false)
                return false;

            var originalTextures = mesh.GetTextures();
            if (game == GameTypeEnum.Warhammer3)
            {
                var tempTextureArray = new Dictionary<TextureType, string>();
                var itemsToSkip = new TextureType[] { TextureType.Specular, TextureType.Diffuse, TextureType.Gloss };
                foreach (var texture in originalTextures)
                {
                    if (itemsToSkip.Contains(texture.Key))
                        continue;
                    tempTextureArray[texture.Key] = texture.Value;
                }
                originalTextures = tempTextureArray;
            }

            foreach (var modelTexture in originalTextures)
            {
                if (TemplateStringToTextureTypes.ContainsValue(modelTexture.Key) == false)
                    continue;

                var path = modelTexture.Value;
                var modelTextureType = modelTexture.Key;

                if (path.Contains("test_mask", StringComparison.InvariantCultureIgnoreCase))
                    continue;

                var materialHasTexture = material.Textures.TryGetValue(modelTextureType, out var materialTexurePath);
                if (materialHasTexture == false)
                    return false;

                var arePathsEqual = materialTexurePath.Contains(path, StringComparison.InvariantCultureIgnoreCase);
                if (arePathsEqual == false)
                    return false;
            }

            return true;
        }

        Dictionary<string, WsModelMaterialFile> LoadAllExistingMaterials()
        {
            var materialPacks = _packFileService.FindAllWithExtentionIncludePaths(".material");
            materialPacks = materialPacks.Where(x => x.Item2.Name.Contains(".xml.material")).ToList();
            var materialList = new Dictionary<string,WsModelMaterialFile>();
            foreach (var materialPack in materialPacks)
            {
                try
                {
                    materialList[materialPack.FileName] = new WsModelMaterialFile(materialPack.Pack);
                }
                catch (Exception e)
                {
                    _logger.Here().Error($"Error loading material for wsmodel generation - {e.Message}");
                }
            }

            return materialList;
        }
    }
}
