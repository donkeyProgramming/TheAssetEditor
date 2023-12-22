using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using CommonControls.Common;
using CommonControls.Events.Scoped;
using CommonControls.FileTypes.RigidModel;
using CommonControls.FileTypes.RigidModel.LodHeader;
using CommonControls.Services;
using Microsoft.Xna.Framework;
using Monogame.WpfInterop.Common;
using Serilog;
using View3D.Animation;
using View3D.SceneNodes;

namespace View3D.Services.SceneSaving.Geometry
{
    public class SceneSaverService
    {
        private readonly ILogger _logger = Logging.Create<SceneSaverService>();
        private readonly EventHub _eventHub;
        private readonly PackFileService _packFileService;
        private readonly IActiveFileResolver _activeFileResolver;
        private readonly ApplicationSettingsService _applicationSettingsService;

        public SceneSaverService(EventHub eventHub, PackFileService packFileService, IActiveFileResolver activeFileResolver, ApplicationSettingsService applicationSettingsService)
        {
            _eventHub = eventHub;
            _packFileService = packFileService;
            _activeFileResolver = activeFileResolver;
            _applicationSettingsService = applicationSettingsService;
        }

        public void Save(string outputPath, MainEditableNode mainNode, RmvVersionEnum rmvVersionEnum, bool onlySaveVisibleNodes = true)
        {
            try
            {
                var inputFile = _packFileService.FindFile(outputPath);
                var bytes = GenerateBytes(onlySaveVisibleNodes, new List<Rmv2ModelNode>() { mainNode }, mainNode.SkeletonNode.Skeleton, rmvVersionEnum, _applicationSettingsService.CurrentSettings.AutoGenerateAttachmentPointsFromMeshes);
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

        static byte[] GenerateBytes(bool onlySaveVisibleNodes, List<Rmv2ModelNode> modelNodes, GameSkeleton skeleton, RmvVersionEnum version, bool enrichModel = true)
        {
            var logger = Logging.Create<SceneSaverService>();
            logger.Here().Information($"Starting to save model. Nodes = {modelNodes.Count}, Skeleton = {skeleton}, Version = {version}");

            var lodCount = (uint)modelNodes.First().Model.LodHeaders.Length;

            logger.Here().Information($"Creating header");
            var outputFile = new RmvFile()
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
            var newMeshList = new List<RmvModel>[lodCount];
            for (var i = 0; i < lodCount; i++)
                newMeshList[i] = new List<RmvModel>();

            foreach (var modelNode in modelNodes)
            {
                for (var currentLodIndex = 0; currentLodIndex < lodCount; currentLodIndex++)
                {
                    var meshes = modelNode.GetMeshesInLod(currentLodIndex, onlySaveVisibleNodes);

                    for (var meshIndex = 0; meshIndex < meshes.Count; meshIndex++)
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
            for (var i = 0; i < lodCount; i++)
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

        static RmvLodHeader[] CreateLodHeaders(RmvLodHeader[] baseHeaders, RmvVersionEnum version)
        {
            var numLods = baseHeaders.Count();
            var factory = LodHeaderFactory.Create();
            var output = new RmvLodHeader[numLods];
            for (var i = 0; i < numLods; i++)
                output[i] = factory.CreateFromBase(version, baseHeaders[i], (uint)i);
            return output;
        }
    }
}
