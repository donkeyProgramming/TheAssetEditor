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
    public class SceneSaverService
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

        public SceneSaverService(PackFileService packFileService, IEditorViewModel editorViewModel, MainEditableNode editableMeshNode)
        {
            _packFileService = packFileService;
            _editorViewModel = editorViewModel;
            _editableMeshNode = editableMeshNode;
        }

        public void Save()
        {
            try
            {
                var inputFile = _editorViewModel.MainFile ;
                byte[] bytes = GetBytesToSave();
                var path = _packFileService.GetFullPath(inputFile);
                var res = SaveHelper.Save(_packFileService, path, inputFile, bytes);
                if (res != null)
                    _editorViewModel.MainFile = res;
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
                var inputFile = _editorViewModel.MainFile ;
                byte[] bytes = GetBytesToSave();

                using (var browser = new SavePackFileWindow(_packFileService))
                {
                    browser.ViewModel.Filter.SetExtentions(new List<string>() { ".rigid_model_v2" });
                    if (browser.ShowDialog() == true)
                    {
                        var path = browser.FilePath;
                        if (path.Contains(".rigid_model_v2") == false)
                            path += ".rigid_model_v2";

                        var res = SaveHelper.Save(_packFileService, path, inputFile, bytes);
                        if (res != null)
                            _editorViewModel.MainFile = res;
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
            var isAllVisible = SceneNodeHelper.AreAllNodesVisible(_editableMeshNode);
            bool onlySaveVisible = false;
            if (isAllVisible == false)
            {
                if (MessageBox.Show("Only save visible nodes?", "", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                    onlySaveVisible = true;
            }

            var bytes = Save(onlySaveVisible, new List<Rmv2ModelNode>() { _editableMeshNode }, _editableMeshNode.Skeleton.AnimationProvider.Skeleton, _editableMeshNode.SelectedOutputFormat);
            return bytes;
        }

        public static List<float> GetDefaultLodReductionValues(int numLods)
        {
            var output = new List<float>();

            for (int lodIndex = 0; numLods < lodIndex; lodIndex++)
                output.Add(GetDefaultLodReductionValue(numLods, lodIndex));

            return output;
        }

        public static float GetDefaultLodReductionValue(int numLods, int currentLodIndex)
        {
            var lerpValue = (1.0f / (numLods - 1)) * (numLods - 1 - currentLodIndex);
            var deductionRatio = MathHelper.Lerp(0.25f, 0.75f, lerpValue);
            return deductionRatio;
        }

        static RmvLodHeader[] CreateLodHeaders(RmvLodHeader[] baseHeaders, RmvVersionEnum version)
        {
            var numLods = baseHeaders.Count();
            var factory = LodHeaderFactory.Create();
            var output = new RmvLodHeader[numLods];
            for (int i = 0; i < numLods; i++)
                output[i] = factory.CreateFromBase(version, baseHeaders[i], (uint)i);
            return output;
        }

        public static byte[] Save(bool onlySaveVisibleNodes, List<Rmv2ModelNode> modelNodes, GameSkeleton skeleton, RmvVersionEnum version, bool enrichModel = true)
        {
            var logger = Logging.Create<SceneSaverService>();
            logger.Here().Information($"Starting to save model. Nodes = {modelNodes.Count}, Skeleton = {skeleton}, Version = {version}");

            uint lodCount = (uint)modelNodes.First().Model.LodHeaders.Length;

            logger.Here().Information($"Creating header");
            RmvFile outputFile = new RmvFile()
            {
                Header = new RmvFileHeader()
                {
                    _fileType = Encoding.ASCII.GetBytes("RMV2"),
                    SkeletonName = skeleton == null ? "" : skeleton.SkeletonName,
                    Version = version,
                    LodCount = lodCount
                },

                LodHeaders = CreateLodHeaders(modelNodes.First().Model.LodHeaders, version)
            };

            // Create all the meshes
            logger.Here().Information($"Creating meshes");
            List<RmvModel>[] newMeshList = new List<RmvModel>[lodCount];
            for (int i = 0; i < lodCount; i++)
                newMeshList[i] = new List<RmvModel>();

            foreach (var modelNode in modelNodes)
            {
                for (int currentLodIndex = 0; currentLodIndex < lodCount; currentLodIndex++)
                {
                    List<Rmv2MeshNode> meshes = modelNode.GetMeshesInLod(currentLodIndex, onlySaveVisibleNodes);

                    for (int meshIndex = 0; meshIndex < meshes.Count; meshIndex++)
                    {
                        logger.Here().Information($"Creating model. Lod: {currentLodIndex}, Model: {meshIndex}");

                        var newModel = new RmvModel()
                        {
                            CommonHeader = meshes[meshIndex].CommonHeader,
                            Material = meshes[meshIndex].Material,
                            Mesh = MeshBuilderService.CreateRmvMeshFromGeometry(meshes[meshIndex].Geometry)
                        };

                        var boneNames = new string[0];
                        if (skeleton != null)
                            boneNames = skeleton.BoneNames.Select(x => x.Replace("bn_", "")).ToArray();

                        newModel.Material.UpdateEnumsBeforeSaving(meshes[meshIndex].Geometry.VertexFormat, version);

                        if (enrichModel)
                            newModel.Material.EnrichDataBeforeSaving(boneNames, BoundingBox.CreateFromPoints(newModel.Mesh.VertexList.Select(x => x.GetPosistionAsVec3())));

                        logger.Here().Information($"Model. Lod: {currentLodIndex}, Model: {meshIndex} created.");
                        newMeshList[currentLodIndex].Add(newModel);
                    }
                }
            }

            // Convert the list to an array
            var newMeshListArray = new RmvModel[lodCount][];
            for (int i = 0; i < lodCount; i++)
                newMeshListArray[i] = newMeshList[i].ToArray();

            // Update data in the header and recalc offset
            logger.Here().Information($"Update offsets");
            outputFile.ModelList = newMeshListArray;
            outputFile.UpdateOffsets();

            // Output the data
            logger.Here().Information($"Generating bytes.");
            var outputBytes = ModelFactory.Create().Save(outputFile);

            logger.Here().Information($"Model saved correctly");
            return outputBytes;
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

                var modelFile = _editorViewModel.MainFile ;
                var modelFilePath = _packFileService.GetFullPath(modelFile);
                var wsModelPath = Path.ChangeExtension(modelFilePath, ".wsmodel");

                var existingWsModelFile = _packFileService.FindFile(wsModelPath, _packFileService.GetEditablePack());

                var wsModelData = GenerateWsModel(modelFilePath, onlySaveVisible, out var wsModelGeneratedPerfectly);
                if (wsModelGeneratedPerfectly == false)
                    MessageBox.Show("Unable to correclty generate WS model, this file needs manual work before its can be used by the game!");

                SaveHelper.Save(_packFileService, wsModelPath, existingWsModelFile , Encoding.UTF8.GetBytes(wsModelData));
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
            List<WsModelFile> materialList = new List<WsModelFile>();
            foreach (var materialPack in materialPacks)
            {
                try
                {
                    materialList.Add(new WsModelFile(materialPack.Item2, materialPack.Item1));
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

            var fileName = modelFileName + "_alpha_" + (alphaOn ? "on" : "off") + ".xml";

            result = result.Replace("FILE_NAME", fileName);

            var dir = Path.GetDirectoryName(modelFilePath);
            var fullPath = dir + "\\materials\\" + fileName + ".material";

            if (_packFileService.FindFile(fullPath, _packFileService.GetEditablePack()) == null)
                SaveHelper.Save(_packFileService, fullPath, null, Encoding.UTF8.GetBytes(result));

            return fullPath;
        }

        string CreateKnownMaterial(Rmv2MeshNode mesh, List<WsModelFile> possibleMaterials)
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
