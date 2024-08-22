using System;
using System.Linq;
using System.Text;
using System.Windows;
using GameWorld.Core.Animation;
using GameWorld.Core.Rendering.Geometry;
using GameWorld.Core.Rendering.Materials.Serialization;
using GameWorld.Core.Rendering.Materials.Shaders;
using GameWorld.Core.SceneNodes;
using Microsoft.Xna.Framework;
using Serilog;
using Shared.Core.ErrorHandling;
using Shared.Core.PackFiles;
using Shared.Core.Services;
using Shared.GameFormats.RigidModel;
using Shared.GameFormats.RigidModel.LodHeader;

namespace GameWorld.Core.Services.SceneSaving.Geometry
{
    public class NodeToRmvSaveHelper
    {
        private readonly ILogger _logger = Logging.Create<NodeToRmvSaveHelper>();
        private readonly PackFileService _packFileService;
        private readonly MeshBuilderService _meshBuilderService;
        private readonly ApplicationSettingsService _applicationSettingsService;

        public NodeToRmvSaveHelper(PackFileService packFileService, MeshBuilderService meshBuilderService, ApplicationSettingsService applicationSettingsService)
        {
            _packFileService = packFileService;
            _meshBuilderService = meshBuilderService;
            _applicationSettingsService = applicationSettingsService;
        }

        public void Save(string outputPath, MainEditableNode mainNode, RmvVersionEnum rmvVersionEnum, GeometrySaveSettings saveSettings)
        {
            try
            {
                var bytes = GenerateBytes(mainNode, mainNode.SkeletonNode.Skeleton, rmvVersionEnum, saveSettings, _applicationSettingsService.CurrentSettings.AutoGenerateAttachmentPointsFromMeshes);
                
                var originalFileHandle = _packFileService.FindFile(outputPath);
                var res = SaveHelper.Save(_packFileService, outputPath, originalFileHandle, bytes);
            }
            catch (Exception e)
            {
                _logger.Here().Error("Error saving model - " + e.ToString());
                MessageBox.Show("Saving failed!");
            }
        }

        byte[] GenerateBytes(Rmv2ModelNode modelNode, GameSkeleton skeleton, RmvVersionEnum version, GeometrySaveSettings saveSettings, bool enrichModel = true)
        {
            _logger.Here().Information($"Starting to save model. Skeleton = {skeleton}, Version = {version}");

            var lodCount = (uint)modelNode.Children.Count;

            if ( !(saveSettings.NumberOfLodsToGenerate == lodCount && saveSettings.LodSettingsPerLod.Count == lodCount) )
                throw new Exception($"Error computer number of lods. saveSettings.NumberOfLodsToGenerate:{saveSettings.NumberOfLodsToGenerate}, lodCount:{lodCount}, saveSettings.LodSettingsPerLod.Count{saveSettings.LodSettingsPerLod.Count}");

            var rmvFile = new RmvFile()
            {
                Header = new RmvFileHeader()
                {
                    _fileType = Encoding.ASCII.GetBytes("RMV2"),
                    SkeletonName = skeleton == null ? "" : skeleton.SkeletonName,
                    Version = version,
                    LodCount = lodCount
                },
                ModelList = new RmvModel[lodCount][],
                LodHeaders = CreateLodHeaders(lodCount, saveSettings, modelNode.Model.LodHeaders, version)
            };

            // Create all the meshes
            for (var lodIndex = 0; lodIndex < lodCount; lodIndex++)
            {
                var meshes = modelNode.GetMeshesInLod(lodIndex, saveSettings.OnlySaveVisible);
                rmvFile.ModelList[lodIndex] = new RmvModel[meshes.Count];
                for (var meshIndex = 0; meshIndex < meshes.Count; meshIndex++)
                {
                    var modelname = meshes[meshIndex].Name;
                    rmvFile.ModelList[lodIndex][meshIndex] = CreateRmvModel(modelname, meshes[meshIndex].PivotPoint, meshes[meshIndex].Material, meshes[meshIndex].Geometry, skeleton, enrichModel);
                }
            }

            // Update data in the header and reCalc offset
            rmvFile.RecalculateOffsets();

            // Output the data
            _logger.Here().Information($"Generating bytes.");
            var outputBytes = ModelFactory.Create().Save(rmvFile);

            _logger.Here().Information($"Model saved correctly");
            return outputBytes;
        }

        RmvModel CreateRmvModel(string modelName, Vector3 pivotPoint, CapabilityMaterial capabilityMaterial, MeshObject geometry, GameSkeleton? skeleton, bool enrichModel)
        {
            var newRmvMaterial = new MaterialToRmvSerializer().CreateMaterialFromCapabilityMaterial(capabilityMaterial);
            newRmvMaterial.UpdateInternalState(geometry.VertexFormat);
            newRmvMaterial.PivotPoint = pivotPoint;
            newRmvMaterial.ModelName = modelName;

            var newModel = new RmvModel()
            {
                CommonHeader = RmvCommonHeader.CreateDefault(),
                Material = newRmvMaterial,
                Mesh = _meshBuilderService.CreateRmvMeshFromGeometry(geometry)
            };

            newModel.UpdateBoundingBox(geometry.BoundingBox);
            newModel.UpdateModelTypeFlag(newModel.Material.MaterialId);

            if (enrichModel && skeleton != null)
            {
                var boneNames = skeleton.BoneNames.Select(x => x.Replace("bn_", "")).ToArray();
                newModel.Material.EnrichDataBeforeSaving(boneNames);
            }

            return newModel;
        }

        static RmvLodHeader[] CreateLodHeaders(uint expectedLodCount, GeometrySaveSettings saveSettings, RmvLodHeader[] originalModelHeaders, RmvVersionEnum version)
        {
            var factory = LodHeaderFactory.Create();
            var output = new RmvLodHeader[expectedLodCount];
            
            for (var i = 0; i < expectedLodCount; i++)
            {
                var hasOriginalMeshLod = originalModelHeaders.Length > i;
                if (hasOriginalMeshLod)
                    output[i] = factory.CreateFromBase(version, originalModelHeaders[i], (uint)i);  // Use the mesh lod header
                else
                    output[i] = factory.CreateFromBase(version, output[i-1], (uint)i);  // Use the last generated lod header

                output[i].LodCameraDistance = saveSettings.LodSettingsPerLod[i].CameraDistance;
                output[i].QualityLvl = saveSettings.LodSettingsPerLod[i].QualityLvl;
            }

            return output;
        }
    }
}
