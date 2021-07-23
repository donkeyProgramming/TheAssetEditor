using Common;
using CommonControls.Common;
using CommonControls.PackFileBrowser;
using CommonControls.Services;
using Filetypes.RigidModel;
using FileTypes.PackFiles.Models;
using FileTypes.RigidModel;
using KitbasherEditor.ViewModels;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using View3D.Components.Component;
using View3D.SceneNodes;

namespace KitbasherEditor.Services
{
    public class ModelSaverHelper
    {
        ILogger _logger = Logging.Create<ModelSaverHelper>();

        private readonly  PackFileService _packFileService;
        private readonly  SceneManager _sceneManager;
        private readonly KitbasherViewModel _kitbasherViewModel;
        private readonly MainEditableNode _editableMeshNode;

        public ModelSaverHelper(PackFileService packFileService, SceneManager sceneManager, KitbasherViewModel kitbasherViewModel, MainEditableNode editableMeshNode)
        {
           _packFileService = packFileService;
           _sceneManager = sceneManager;
           _kitbasherViewModel = kitbasherViewModel;
            _editableMeshNode = editableMeshNode;
        }

        public void Save()
        {
            try
            {
                var inputFile = _kitbasherViewModel.MainFile as PackFile;
                byte[] bytes = GetBytesToSave();
                var path = _packFileService.GetFullPath(inputFile);
                var res = SaveHelper.Save(_packFileService, path, inputFile, bytes);
                if (res != null)
                    _kitbasherViewModel.MainFile = res;
            }
            catch (Exception e)
            {
                _logger.Here().Error("Error saving model - " + e.ToString());
                MessageBox.Show("Saving failed!");
            }
        }

        public void SaveAs()
        {
            try
            {
                var inputFile = _kitbasherViewModel.MainFile as PackFile;
                byte[] bytes = GetBytesToSave();

                using (var browser = new SavePackFileWindow(_packFileService))
                {
                    browser.ViewModel.Filter.SetExtentions(new List<string>() {".rigid_model_v2" });
                    if (browser.ShowDialog() == true)
                    {
                        var path = browser.FilePath;
                        if (path.Contains(".rigid_model_v2") == false)
                            path += ".rigid_model_v2";

                        var res = SaveHelper.Save(_packFileService, path, inputFile, bytes);
                        if (res != null)
                            _kitbasherViewModel.MainFile = res;
                    }
                }
            }
            catch (Exception e)
            {
                _logger.Here().Error("Error saving model - " + e.ToString());
                MessageBox.Show("Saving failed!");
            }
        }

        private byte[] GetBytesToSave()
        {
            var isAllVisible = _editableMeshNode.AreAllNodesVisible();
            bool onlySaveVisible = false;
            if (isAllVisible == false)
            {
                if (MessageBox.Show("Only save visible nodes?", "", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                    onlySaveVisible = true;
            }

            var bytes = _editableMeshNode.Save(onlySaveVisible);
            var reloadedModel = new RmvRigidModel(bytes, "reloadedFile");
            return bytes;
        }

        public void GenerateWsModel()
        {
            try
            {
                var isAllVisible = _editableMeshNode.AreAllNodesVisible();
                bool onlySaveVisible = false;
                if (isAllVisible == false)
                {
                    if (MessageBox.Show("Only generate for visible nodes?", "", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                        onlySaveVisible = true;
                }

                var modelFile = _kitbasherViewModel.MainFile as PackFile;
                var modelFilePath = _packFileService.GetFullPath(modelFile);
                var wsModelPath = Path.ChangeExtension(modelFilePath, ".wsmodel");

                var existingWsModelFile = _packFileService.FindFile(wsModelPath, _packFileService.GetEditablePack());

                var wsModelData = GenerateWsModel(modelFilePath, onlySaveVisible, out var wsModelGeneratedPerfectly);
                if (wsModelGeneratedPerfectly == false)
                    MessageBox.Show("Unable to correclty generate WS model, this file needs manual work before its can be used by the game!");

                SaveHelper.Save(_packFileService, wsModelPath, existingWsModelFile as PackFile, Encoding.UTF8.GetBytes(wsModelData));
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
            List<WsModelMaterial> materialList = new List<WsModelMaterial>();
            foreach (var materialPack in materialPacks)
            {
                try
                {
                    materialList.Add(new WsModelMaterial(materialPack.Item2, materialPack.Item1));
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
                        materialFile = UnknownMaterial(meshes[meshIndex]);
                        wsModelGeneratedPerfectly = false;
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

        private string UnknownMaterial(Rmv2MeshNode mesh)
        {
            var textureName = "?";
            var texture = mesh.MeshModel.GetTexture(TexureType.Diffuse);
            if (texture.HasValue)
                textureName = texture.Value.Path;
            var vertextType = mesh.MeshModel.Header.VertextType;
            var alphaOn = mesh.MeshModel.AlphaSettings.Mode != AlphaMode.Opaque;

            var vertexName = "uknown";
            if (vertextType == VertexFormat.Cinematic)
                vertexName = "weighted4";
            else if (vertextType == VertexFormat.Weighted)
                vertexName = "weighted2";
            else if (vertextType == VertexFormat.Static)
                vertexName = "static";

            return $" MeshName='{mesh.Name}' Texture='{textureName}' VertType='{vertexName}' Alpha='{alphaOn}'";
        }

        string  CreateKnownMaterial(Rmv2MeshNode mesh, List<WsModelMaterial> possibleMaterials)
        {
            foreach (var material in possibleMaterials)
            {
                if (mesh.MeshModel.Header.VertextType != material.VertexType)
                    continue;

                var alphaOn = mesh.MeshModel.AlphaSettings.Mode != AlphaMode.Opaque;
                if (alphaOn && material.Alpha == false)
                    continue;

                bool texturesOk = true;
                foreach (var modelTexture in mesh.MeshModel.Textures)
                {
                    if (modelTexture.Path.Contains("test_mask", StringComparison.InvariantCultureIgnoreCase))
                        continue;

                    var modelTextureType = modelTexture.TexureType;
                    var materialHasTexture = material.Textures.TryGetValue(modelTextureType, out var materialTexurePath);
                    if (materialHasTexture == false)
                    {
                        texturesOk = false;
                        break;
                    }

                    var arePathsEqual = materialTexurePath.Contains(modelTexture.Path, StringComparison.InvariantCultureIgnoreCase);
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


        /*
         <model version="1">
  <geometry>VariantMeshes/wh_variantmodels/ce2/chaos/chs_dragon_ogre/chs_dragon_ogre_head_01.rigid_model_v2</geometry>
  <materials>
    <material part_index="0" lod_index="0">VariantMeshes/wh_variantmodels/ce2/chaos/chs_dragon_ogre/materials/chs_dragon_ogre_head_01_weighted4_alpha_off.xml.material</material>
    <material part_index="0" lod_index="1">VariantMeshes/wh_variantmodels/ce2/chaos/chs_dragon_ogre/materials/chs_dragon_ogre_head_01_weighted4_alpha_off.xml.material</material>
    <material part_index="0" lod_index="2">VariantMeshes/wh_variantmodels/ce2/chaos/chs_dragon_ogre/materials/chs_dragon_ogre_head_01_weighted2_alpha_off.xml.material</material>
    <material part_index="0" lod_index="3">VariantMeshes/wh_variantmodels/ce2/chaos/chs_dragon_ogre/materials/chs_dragon_ogre_head_01_weighted2_alpha_off.xml.material</material>
    
	<material part_index="1" lod_index="0">VariantMeshes/wh_variantmodels/ce2/chaos/chs_dragon_ogre/materials/chs_dragon_ogre_body_01_weighted4_alpha_on.xml.material</material>
    <material part_index="1" lod_index="1">VariantMeshes/wh_variantmodels/ce2/chaos/chs_dragon_ogre/materials/chs_dragon_ogre_body_01_weighted4_alpha_on.xml.material</material>
    <material part_index="1" lod_index="2">VariantMeshes/wh_variantmodels/ce2/chaos/chs_dragon_ogre/materials/chs_dragon_ogre_body_01_weighted2_alpha_on.xml.material</material>
  </materials>
</model>
         */
    }
}
