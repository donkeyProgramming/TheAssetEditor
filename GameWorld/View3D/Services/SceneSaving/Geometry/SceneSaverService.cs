using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using GameWorld.Core.Animation;
using GameWorld.Core.SceneNodes;
using Microsoft.Xna.Framework;
using Serilog;
using Shared.Core.ErrorHandling;
using Shared.Core.Events;
using Shared.Core.Events.Scoped;
using Shared.Core.PackFiles;
using Shared.Core.Services;
using Shared.GameFormats.RigidModel;
using Shared.GameFormats.RigidModel.LodHeader;

namespace GameWorld.Core.Services.SceneSaving.Geometry
{
    public class SceneSaverService
    {
        private readonly ILogger _logger = Logging.Create<SceneSaverService>();
        private readonly EventHub _eventHub;
        private readonly PackFileService _packFileService;
        private readonly ApplicationSettingsService _applicationSettingsService;

        public SceneSaverService(EventHub eventHub, PackFileService packFileService,ApplicationSettingsService applicationSettingsService)
        {
            _eventHub = eventHub;
            _packFileService = packFileService;
            _applicationSettingsService = applicationSettingsService;
        }

        public void Save(string outputPath, MainEditableNode mainNode, RmvVersionEnum rmvVersionEnum, bool onlySaveVisibleNodes = true)
        {
            try
            {
                var inputFile = _packFileService.FindFile(outputPath);
                var bytes = GenerateBytes(onlySaveVisibleNodes, mainNode, mainNode.SkeletonNode.Skeleton, rmvVersionEnum, _applicationSettingsService.CurrentSettings.AutoGenerateAttachmentPointsFromMeshes);
                var res = SaveHelper.Save(_packFileService, outputPath, inputFile, bytes);
                _eventHub.Publish(new ScopedFileSavedEvent());
            }
            catch (Exception e)
            {
                _logger.Here().Error("Error saving model - " + e.ToString());
                MessageBox.Show("Saving failed!");
            }
        }

        byte[] GenerateBytes(bool onlySaveVisibleNodes, Rmv2ModelNode modelNode, GameSkeleton skeleton, RmvVersionEnum version, bool enrichModel = true)
        {
            _logger.Here().Information($"Starting to save model. Skeleton = {skeleton}, Version = {version}");

            var lodCount = (uint)modelNode.Children.Count;
            var outputFile = new RmvFile()
            {
                Header = new RmvFileHeader()
                {
                    _fileType = Encoding.ASCII.GetBytes("RMV2"),
                    SkeletonName = skeleton == null ? "" : skeleton.SkeletonName,
                    Version = version,
                    LodCount = lodCount
                },

                LodHeaders = CreateLodHeaders(lodCount, modelNode.Model.LodHeaders, version)
            };

            // Create all the meshes
            _logger.Here().Information($"Creating meshes");
            var newMeshList = new List<RmvModel>[lodCount];
            for (var i = 0; i < lodCount; i++)
                newMeshList[i] = [];

            for (var currentLodIndex = 0; currentLodIndex < lodCount; currentLodIndex++)
            {
                var meshes = modelNode.GetMeshesInLod(currentLodIndex, onlySaveVisibleNodes);

                for (var meshIndex = 0; meshIndex < meshes.Count; meshIndex++)
                {
                    _logger.Here().Information($"Creating model. Lod: {currentLodIndex}, Model: {meshIndex}");

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

                    _logger.Here().Information($"Model. Lod: {currentLodIndex}, Model: {meshIndex} created.");
                    newMeshList[currentLodIndex].Add(newModel);
                }
            }
            
            // Convert the list to an array
            var newMeshListArray = new RmvModel[lodCount][];
            for (var i = 0; i < lodCount; i++)
                newMeshListArray[i] = newMeshList[i].ToArray();

            // Update data in the header and reCalc offset
            _logger.Here().Information($"Update offsets");
            outputFile.ModelList = newMeshListArray;
            outputFile.UpdateOffsets();

            // Output the data
            _logger.Here().Information($"Generating bytes.");
            var outputBytes = ModelFactory.Create().Save(outputFile);

            _logger.Here().Information($"Model saved correctly");
            return outputBytes;
        }

        static RmvLodHeader[] CreateLodHeaders(uint expectedLodCount, RmvLodHeader[] originalModelHeaders, RmvVersionEnum version)
        {
            var numLods = originalModelHeaders.Length;
            var factory = LodHeaderFactory.Create();
            var output = new RmvLodHeader[expectedLodCount];
            
            for (var i = 0; i < expectedLodCount; i++)
            {
                var hasOriginalMeshLod = originalModelHeaders.Length > i;
                if (hasOriginalMeshLod)
                    output[i] = factory.CreateFromBase(version, originalModelHeaders[i], (uint)i);  // Use the mesh lod header
                else
                    output[i] = factory.CreateFromBase(version, output[i-1], (uint)i);  // Use the last generated lod header
            }

            return output;
        }
    }
}
