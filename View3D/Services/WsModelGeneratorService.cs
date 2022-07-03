using CommonControls.Common;
using CommonControls.PackFileBrowser;
using CommonControls.Services;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows;
using View3D.SceneNodes;
using Microsoft.Xna.Framework;
using View3D.Animation;
using CommonControls.FileTypes.RigidModel.LodHeader;
using CommonControls.FileTypes.RigidModel.Types;
using CommonControls.FileTypes.RigidModel;

namespace View3D.Services
{
    public class WsModelGeneratorService
    {
        ILogger _logger = Logging.Create<SceneSaverService>();

        private readonly PackFileService _packFileService;
        private readonly IEditorViewModel _editorViewModel;
        private readonly MainEditableNode _editableMeshNode;
        private readonly List<WsModelMaterialFile> _existingMaterials;


        private static readonly Dictionary<string, TexureType> TemplateStringToTextureTypes = new Dictionary<string, TexureType>
        {
            {"BASE_COLOUR_PATH", TexureType.BaseColour},
            {"MATERIAL_MAP", TexureType.MaterialMap},
            {"NORMAL_PATH", TexureType.Normal},
            {"MASK_PATH", TexureType.Mask},
            {"DIFFUSE_PATH", TexureType.Diffuse },
            {"GLOSS_PATH", TexureType.Gloss },
            {"SPECULAR_PATH", TexureType.Specular },
        };

        public WsModelGeneratorService(PackFileService packFileService, IEditorViewModel editorViewModel, MainEditableNode editableMeshNode)
        {
            _packFileService = packFileService;
            _editorViewModel = editorViewModel;
            _editableMeshNode = editableMeshNode;

            _existingMaterials = LoadAllExistingMaterials();
        }

        public void GenerateWsModel(GameTypeEnum game = GameTypeEnum.Warhammer3)
        {
            try
            {
                if (MessageBox.Show($"Generate ws model for {game}", "", MessageBoxButton.YesNo) != MessageBoxResult.Yes)
                    return;

                if (_packFileService.GetEditablePack() == null)
                {
                    MessageBox.Show("No editable pack selected", "error");
                    return;
                }

                var modelFile = _editorViewModel.MainFile;
                var modelFilePath = _packFileService.GetFullPath(modelFile);
                var wsModelPath = Path.ChangeExtension(modelFilePath, ".wsmodel");

                var materialTemplate = game switch
                {
                    GameTypeEnum.Warhammer3 => LoadMaterialTemplate("View3D.Content.Game.MaterialTemplate_wh3.xml.material"),
                    GameTypeEnum.Warhammer2 => LoadMaterialTemplate("View3D.Content.Game.MaterialTemplate_wh2.xml.material"),
                    _ => throw new Exception("Unkown game - unable to generate ws model")
                };

                var wsModelData = CreateWsModel(game, modelFilePath, materialTemplate);
                var existingWsModelFile = _packFileService.FindFile(wsModelPath, _packFileService.GetEditablePack());
                SaveHelper.Save(_packFileService, wsModelPath, existingWsModelFile, Encoding.UTF8.GetBytes(wsModelData));
            }
            catch (Exception e)
            {
                _logger.Here().Error("Error generating ws model - " + e.Message);
                MessageBox.Show("Generation failed!");
            }
        }


        string CreateWsModel(GameTypeEnum game, string modelFilePath, string materialTemplate)
        {
            var sb = new StringBuilder();

            sb.Append("<model version=\"1\">\n");
            sb.Append($"\t<geometry>{modelFilePath}</geometry>\n");
            sb.Append("\t\t<materials>\n");

            var lodNodes = _editableMeshNode.GetLodNodes();
            for (int lodIndex = 0; lodIndex < lodNodes.Count; lodIndex++)
            {
                var meshes = _editableMeshNode.GetMeshesInLod(lodIndex, false);
                var uniqueNames = GenerateUniqueNames(meshes);
                for (int meshIndex = 0; meshIndex < meshes.Count; meshIndex++)
                {
                    var materialFile = GetOrCreateMaterial(game, meshes[meshIndex], uniqueNames[meshIndex], materialTemplate);
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
                for (var index = 0; index < 1024; index++)
                {
                    var name = (index == 0) ? fileName : string.Format("{0}_{1}", fileName, index);
                    if (output.Contains(name))
                        continue;

                    fileName = name;
                    break;
                }

                output.Add(fileName);
            }

            return output;
        }

        string GetOrCreateMaterial(GameTypeEnum game, Rmv2MeshNode mesh, string uniqueName, string materialTemplate)
        {
            uniqueName = uniqueName.Trim();
            var materialFileName = FindApplicableExistingMaterial(game, mesh);
            if (materialFileName == null)
                materialFileName = CreateNewMaterial(mesh, uniqueName, materialTemplate);
            return materialFileName;
        }

        string LoadMaterialTemplate(string key)
        {
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(key);
            using var reader = new StreamReader(stream!);
            var result = reader.ReadToEnd();
            return result;
        }

        string CreateNewMaterial(Rmv2MeshNode mesh, string uniqueName, string materialTemplate)
        {
            var vertexType = ModelMaterialEnumHelper.GetToolVertexFormat(mesh.Material.BinaryVertexFormat);
            var alphaOn = mesh.Material.AlphaMode != AlphaMode.Opaque;

            var shaderNamePart = vertexType switch
            {
                UiVertexFormat.Cinematic => "weighted4",
                UiVertexFormat.Weighted => "weighted2",
                UiVertexFormat.Static => "rigid",
                _ => throw new Exception("Unknown vertex type")
            };

            // Update the shader name
            var shaderAlphaStr = "";
            if (alphaOn)
                shaderAlphaStr = "_alpha";
            var shaderName = $"shaders/{shaderNamePart}_character{shaderAlphaStr}.xml.shader";
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
            var fileName = uniqueName + "_" + shaderNamePart + "_alpha_" + (alphaOn ? "on" : "off") + ".xml";
            materialTemplate = materialTemplate.Replace("FILE_NAME", fileName);

            var modelFile = _editorViewModel.MainFile;
            var modelFilePath = _packFileService.GetFullPath(modelFile);
            var dir = Path.GetDirectoryName(modelFilePath);
            var fullPath = dir + "\\materials\\" + fileName + ".material";
            SaveHelper.Save(_packFileService, fullPath, null, Encoding.UTF8.GetBytes(materialTemplate), false);

            return fullPath;
        }

        string FindApplicableExistingMaterial(GameTypeEnum game, Rmv2MeshNode mesh)
        {
            foreach (var material in _existingMaterials)
            {
                var isMatch = IsMaterialMatch(game, mesh, material);
                if (isMatch)
                    return material.FullPath;
            }

            return null;
        }

        bool IsMaterialMatch(GameTypeEnum game, Rmv2MeshNode mesh, WsModelMaterialFile material)
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
                var tempTextureArray = new Dictionary<TexureType, string>();
                var itemsToSkip = new TexureType[] { TexureType.Specular, TexureType.Diffuse, TexureType.Gloss };
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

        List<WsModelMaterialFile> LoadAllExistingMaterials()
        {
            var materialPacks = _packFileService.FindAllWithExtentionIncludePaths(".material");
            materialPacks = materialPacks.Where(x => x.Item2.Name.Contains(".xml.material")).ToList();
            var materialList = new List<WsModelMaterialFile>();
            foreach (var materialPack in materialPacks)
            {
                try
                {
                    var material = new WsModelMaterialFile(materialPack.Item2, materialPack.Item1);
                    materialList.Add(material);
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
