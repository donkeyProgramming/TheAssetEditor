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

        private static readonly Dictionary<string, TexureType> TemplateStringToTextureTypes = new Dictionary<string, TexureType>
        {
            {"GLOSS_PATH", TexureType.Gloss},
            {"SPECULAR_PATH", TexureType.Specular},
            {"NORMAL_PATH", TexureType.Normal},
            {"MASK_PATH", TexureType.Mask},
            {"DIFFUSE_PATH", TexureType.Diffuse},
        };

        public WsModelGeneratorService(PackFileService packFileService, IEditorViewModel editorViewModel, MainEditableNode editableMeshNode)
        {
            _packFileService = packFileService;
            _editorViewModel = editorViewModel;
            _editableMeshNode = editableMeshNode;
        }

        public void GenerateWsModel()
        {
            try
            {
                if (_packFileService.GetEditablePack() == null)
                {
                    MessageBox.Show("No editable pack selected", "error");
                    return;
                }

                var isAllVisible = SceneNodeHelper.AreAllNodesVisible(_editableMeshNode);
                bool onlySaveVisible = false;
                if (isAllVisible == false)
                {
                    if (MessageBox.Show("Only generate for visible nodes?", "", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                        onlySaveVisible = true;
                }

                var modelFile = _editorViewModel.MainFile;
                var modelFilePath = _packFileService.GetFullPath(modelFile);
                var wsModelPath = Path.ChangeExtension(modelFilePath, ".wsmodel");

                var existingWsModelFile = _packFileService.FindFile(wsModelPath, _packFileService.GetEditablePack());

                var wsModelData = GenerateWsModel(modelFilePath, onlySaveVisible, out var wsModelGeneratedPerfectly);
                if (wsModelGeneratedPerfectly == false)
                    MessageBox.Show("Unable to correclty generate WS model, this file needs manual work before its can be used by the game!");

                SaveHelper.Save(_packFileService, wsModelPath, existingWsModelFile, Encoding.UTF8.GetBytes(wsModelData));
            }
            catch (Exception e)
            {
                _logger.Here().Error("Error generating ws model - " + e.ToString());
                MessageBox.Show("Generation failed!");
            }
        }


        string GenerateWsModel(string modelFilePath, bool onlyVisible, out bool wsModelGeneratedPerfectly)
        {
            wsModelGeneratedPerfectly = true;

            var materialPacks = _packFileService.FindAllWithExtentionIncludePaths(".material");
            materialPacks = materialPacks.Where(x => x.Item2.Name.Contains(".xml.material")).ToList();
            List<WsModelMaterialFile> materialList = new List<WsModelMaterialFile>();
            foreach (var materialPack in materialPacks)
            {
                try
                {
                    materialList.Add(new WsModelMaterialFile(materialPack.Item2, materialPack.Item1));
                }
                catch (Exception e)
                {
                    _logger.Here().Error("Error loading material for wsmodel generation - " + e.ToString());
                }
            }

            var sb = new StringBuilder();

            sb.Append("<model version=\"1\">\n");
            sb.Append($"\t<geometry>{modelFilePath}</geometry>\n");
            sb.Append("\t\t<materials>\n");

            var lodNodes = _editableMeshNode.GetLodNodes();
            for (int lodIndex = 0; lodIndex < lodNodes.Count; lodIndex++)
            {
                var meshes = _editableMeshNode.GetMeshesInLod(lodIndex, onlyVisible);
                for (int meshIndex = 0; meshIndex < meshes.Count; meshIndex++)
                {
                    var materialFile = CreateKnownMaterial(meshes[meshIndex], materialList);
                    if (materialFile == null)
                    {
                        materialFile = CreateUnknownMaterial(meshes[meshIndex]);
                    }

                    sb.Append($"\t\t\t<material part_index=\"{meshIndex}\" lod_index=\"{lodIndex}\">");
                    sb.Append(materialFile);
                    sb.Append("</material>\n");
                }
            }

            sb.Append("\t</materials>\n");
            sb.Append("</model>\n");

            return sb.ToString();
        }

        private string CreateUnknownMaterial(Rmv2MeshNode mesh)
        {
            var vertextType = mesh.Material.VertexType;
            var alphaOn = mesh.Material.AlphaMode != AlphaMode.Opaque;

            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("View3D.Content.Game.MaterialTemplate.xml.material");
            using var reader = new StreamReader(stream!);
            var result = reader.ReadToEnd();

            var shaderPath = vertextType switch
            {
                UiVertexFormat.Cinematic => alphaOn ? "weighted4_default_alpha" : "weighted4_default",
                UiVertexFormat.Weighted => alphaOn ? "weighted2_default_alpha" : "weighted2_default",
                UiVertexFormat.Static => alphaOn ? "rigid_default_alpha" : "rigid_default",
                _ => string.Empty
            };

            var shaderNamePart = vertextType switch
            {
                UiVertexFormat.Cinematic => "_weighted4",
                UiVertexFormat.Weighted => "_weighted2",
                UiVertexFormat.Static => "_rigid",
                _ => string.Empty
            };

            result = result.Replace("SHADER_PATH", "shaders/" + shaderPath + ".xml.shader");

            foreach (var (replacment, textureType) in TemplateStringToTextureTypes)
            {
                var texture = mesh.Material.GetTexture(textureType);
                if (texture.HasValue)
                    result = result.Replace(replacment, texture.Value.Path);
            }

            var modelFile = _editorViewModel.MainFile;
            var modelFilePath = _packFileService.GetFullPath(modelFile);

            var modelFileName = Path.GetFileNameWithoutExtension(modelFilePath);

            var fileName = modelFileName + shaderNamePart + "_alpha_" + (alphaOn ? "on" : "off") + ".xml";

            result = result.Replace("FILE_NAME", fileName);

            var dir = Path.GetDirectoryName(modelFilePath);
            var fullPath = dir + "\\materials\\" + fileName + ".material";

            if (_packFileService.FindFile(fullPath, _packFileService.GetEditablePack()) == null)
                SaveHelper.Save(_packFileService, fullPath, null, Encoding.UTF8.GetBytes(result));

            return fullPath;
        }

        string CreateKnownMaterial(Rmv2MeshNode mesh, List<WsModelMaterialFile> possibleMaterials)
        {
            foreach (var material in possibleMaterials)
            {
                if (mesh.Material.VertexType != material.VertexType)
                    continue;

                var alphaOn = mesh.Material.AlphaMode != AlphaMode.Opaque;
                if (alphaOn && material.Alpha == false)
                    continue;

                bool texturesOk = true;
                foreach (var modelTexture in mesh.GetTextures())
                {
                    var path = modelTexture.Value;
                    var modelTextureType = modelTexture.Key;

                    if (path.Contains("test_mask", StringComparison.InvariantCultureIgnoreCase))
                        continue;

                    var materialHasTexture = material.Textures.TryGetValue(modelTextureType, out var materialTexurePath);
                    if (materialHasTexture == false)
                    {
                        texturesOk = false;
                        break;
                    }

                    var arePathsEqual = materialTexurePath.Contains(path, StringComparison.InvariantCultureIgnoreCase);
                    if (arePathsEqual == false)
                    {
                        texturesOk = false;
                        break;
                    }
                }

                if (texturesOk)
                {
                    return material.FullPath;
                }
            }

            return null;
        }
    }
}
