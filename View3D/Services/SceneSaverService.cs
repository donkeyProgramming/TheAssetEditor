using CommonControls.BaseDialogs.ErrorListDialog;
using CommonControls.Common;
using CommonControls.Events.Scoped;
using CommonControls.FileTypes.RigidModel;
using CommonControls.FileTypes.RigidModel.LodHeader;
using CommonControls.PackFileBrowser;
using CommonControls.Services;
using Microsoft.Xna.Framework;
using Monogame.WpfInterop.Common;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using View3D.Animation;
using View3D.Components.Component;
using View3D.SceneNodes;
using View3D.Utility;

namespace View3D.Services
{
    public class SceneSaverService
    {
        ILogger _logger = Logging.Create<SceneSaverService>();
        private readonly EventHub _eventHub;
        private readonly PackFileService _packFileService;
        private readonly IActiveFileResolver _activeFileResolver;
        private readonly SceneManager _sceneManager;
        private readonly ApplicationSettingsService _applicationSettingsService;

        public SceneSaverService(EventHub eventHub, PackFileService packFileService, IActiveFileResolver activeFileResolver, SceneManager sceneManager, ApplicationSettingsService applicationSettingsService)
        {
            _eventHub = eventHub;
            _packFileService = packFileService;
            _activeFileResolver = activeFileResolver;
            _sceneManager = sceneManager;
            _applicationSettingsService = applicationSettingsService;
        }

        public void Save(RmvVersionEnum rmvVersionEnum)
        {
            try
            {
                DisplayValidateDialog();

                var inputFile = _activeFileResolver.Get();
                byte[] bytes = GetBytesToSave(rmvVersionEnum);
                var path = _packFileService.GetFullPath(inputFile);
                var res = SaveHelper.Save(_packFileService, path, inputFile, bytes);
                _eventHub.Publish(new FileSavedEvent());
            }
            catch (Exception e)
            {
                _logger.Here().Error("Error saving model - " + e.ToString());
                MessageBox.Show("Saving failed!");
            }
        }

        public void SaveAs(RmvVersionEnum rmvVersionEnum)
        {
            try
            {
                DisplayValidateDialog();

                var inputFile = _activeFileResolver.Get();
                byte[] bytes = GetBytesToSave(rmvVersionEnum);

                using (var browser = new SavePackFileWindow(_packFileService))
                {
                    browser.ViewModel.Filter.SetExtentions(new List<string>() { ".rigid_model_v2" });
                    if (browser.ShowDialog() == true)
                    {
                        var path = browser.FilePath;
                        if (path.Contains(".rigid_model_v2") == false)
                            path += ".rigid_model_v2";

                        var res = SaveHelper.Save(_packFileService, path, inputFile, bytes);
                        _activeFileResolver.ActiveFileName = path;
                    }
                }
            }
            catch (Exception e)
            {
                _logger.Here().Error("Error saving model - " + e.ToString());
                MessageBox.Show("Saving failed!");
            }
        }

        private byte[] GetBytesToSave(RmvVersionEnum rmvVersionEnum)
        {
            var mainNode = _sceneManager.GetNodeByName<MainEditableNode>(SpecialNodes.EditableModel);
            var isAllVisible = SceneNodeHelper.AreAllNodesVisible(mainNode);
            bool onlySaveVisible = false;
            if (isAllVisible == false)
            {
                if (MessageBox.Show("Only save visible nodes?", "", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                    onlySaveVisible = true;
            }

            var bytes = Save(onlySaveVisible, new List<Rmv2ModelNode>() { mainNode }, mainNode.SkeletonNode.Skeleton, rmvVersionEnum, _applicationSettingsService.CurrentSettings.AutoGenerateAttachmentPointsFromMeshes);
            return bytes;
        }

        public static float GetDefaultLodReductionValue(int numLods, int currentLodIndex)
        {
            var lerpValue = (1.0f / (numLods - 1)) * (numLods - 1 - currentLodIndex);
            var deductionRatio = MathHelper.Lerp(0.25f, 1, lerpValue);
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

                        newModel.UpdateBoundingBox(meshes[meshIndex].Geometry.BoundingBox);

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

        ErrorListViewModel.ErrorList Validate()
        {
            var errorList = new ErrorListViewModel.ErrorList();
            var mainNode = _sceneManager.GetNodeByName<MainEditableNode>(SpecialNodes.EditableModel);

            var skeleton = mainNode.SkeletonNode.Skeleton;
            var meshes = mainNode.GetMeshNodes(0);

            // Different skeltons
            if (skeleton != null)
            {
                var activeSkeletonName = skeleton.SkeletonName;
                var skeltonNames = meshes.Select(x => x.Geometry.ParentSkeletonName).Distinct().ToList();

                if (skeltonNames.Count != 1)
                    errorList.Error("Skeleton", "Model contains meshes with multiple skeleton references. They will not animate well in game");

                skeltonNames.Remove(activeSkeletonName);
                if (skeltonNames.Count != 0)
                    errorList.Error("Skeleton", "Model contains meshes that have not been re-rigged. They will not behave well in game");
            }

            // Mismatch between static and animated vertex
            var vertexTypes = meshes.Select(x => x.Geometry.VertexFormat).Distinct().ToList();
            if (vertexTypes.Contains(UiVertexFormat.Static) && skeleton != null)
                errorList.Error("Vertex", "Model has a skeleton, but contains meshes with non-animated vertexes. Rig them or they will not behave as expected in game");

            if ((vertexTypes.Contains(UiVertexFormat.Weighted) || vertexTypes.Contains(UiVertexFormat.Cinematic)) && skeleton == null)
                errorList.Error("Vertex", "Model does not have a skeleton, but has animated vertex data.");

            // Large model count
            if (meshes.Count > 50)
                errorList.Warning("Mesh Count", "Model contains a large amount of mehses, might cause performance issues");

            if (ModelCombiner.HasPotentialCombineMeshes(meshes, out _))
                errorList.Warning("Mesh", "Model contains multiple meshes that can be merged. Consider merging them for performance reasons");

            // Different pivots
            var pivots = meshes.Select(x => x.Material.PivotPoint).Distinct().ToList();
            if (pivots.Count != 1)
                errorList.Warning("Pivot Point", "Model contains multiple different pivot points, this is almost always not intended");

            // Animation and Pivotpoint
            if (pivots.Count == 1 && skeleton != null)
            {
                if ((pivots.First().X == 0 && pivots.First().Y == 0 && pivots.First().Z == 0) == false)
                    errorList.Warning("Pivot Point", "Model contains a non zero pivot point and animation, this is almost always not intended");
            }

            return errorList;
        }

        void DisplayValidateDialog()
        {
            var errorList = Validate();
            if (errorList.HasData)
                ErrorListWindow.ShowDialog("Potential problems", errorList);
        }

    }
}
