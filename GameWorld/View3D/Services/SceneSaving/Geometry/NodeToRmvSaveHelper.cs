using System;
using System.Collections.Generic;
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
using Shared.GameFormats.RigidModel;
using Shared.GameFormats.RigidModel.LodHeader;
using Shared.GameFormats.RigidModel.Types;

namespace GameWorld.Core.Services.SceneSaving.Geometry
{
    public class NodeToRmvSaveHelper
    {
        private readonly ILogger _logger = Logging.Create<NodeToRmvSaveHelper>();
        private readonly IFileSaveService _packFileSaveService;
        private readonly MeshBuilderService _meshBuilderService;
       
        public NodeToRmvSaveHelper(IFileSaveService packFileSaveService, MeshBuilderService meshBuilderService)
        {
            _packFileSaveService = packFileSaveService;
            _meshBuilderService = meshBuilderService;
        }

        public RmvFile? Save(string outputPath, Rmv2ModelNode mainNode, GameSkeleton? skeleton, RmvVersionEnum rmvVersionEnum, GeometrySaveSettings saveSettings)
        {
            try
            {
                var rmvFile = GenerateBytes(mainNode, rmvVersionEnum, skeleton, saveSettings);
                var bytes = ModelFactory.Create().Save(rmvFile);
                _logger.Here().Information($"Model generated correctly");

                _packFileSaveService.Save(outputPath, bytes, false);

                return rmvFile;
            }
            catch (Exception e)
            {
                _logger.Here().Error("Error saving model - " + e.ToString());
                MessageBox.Show($"Saving failed!\n{e.Message}" );

                return null;
            }
        }

        RmvFile GenerateBytes(Rmv2ModelNode modelNode, RmvVersionEnum version, GameSkeleton? skeleton, GeometrySaveSettings saveSettings)
        {
            _logger.Here().Information($"Starting to save model. Skeleton = {skeleton}, Version = {version}");

            var lodCount = (uint)modelNode.Children.Count;
            if (saveSettings.LodSettingsPerLod.Count != lodCount )
                throw new Exception($"Error computer number of lods. LodCount:{lodCount}, SaveSettings.LodSettingsPerLod.Count{saveSettings.LodSettingsPerLod.Count}");

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
                LodHeaders = CreateLodHeaders(lodCount, saveSettings.LodSettingsPerLod, version)
            };

            // Create all the meshes
            for (var lodIndex = 0; lodIndex < lodCount; lodIndex++)
            {
                var meshes = modelNode.GetMeshesInLod(lodIndex, saveSettings.OnlySaveVisible);
                rmvFile.ModelList[lodIndex] = new RmvModel[meshes.Count];
                for (var meshIndex = 0; meshIndex < meshes.Count; meshIndex++)
                {
                    var modelname = meshes[meshIndex].Name;
                    rmvFile.ModelList[lodIndex][meshIndex] = CreateRmvModel(modelname, meshes[meshIndex].PivotPoint, meshes[meshIndex].Material, meshes[meshIndex].Geometry, skeleton, saveSettings.AttachmentPoints, meshes[meshIndex].AnimationMatrixOverride);
                }
            }

            // Update data in the header and reCalc offset
            rmvFile.RecalculateOffsets();

            return rmvFile;
        }

        RmvModel CreateRmvModel(string modelName, Vector3 pivotPoint, CapabilityMaterial capabilityMaterial, MeshObject geometry, GameSkeleton? skeleton, List<RmvAttachmentPoint> attachmentPoints, int animationMatrixOverride)
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

     
            newModel.Material.EnrichDataBeforeSaving(attachmentPoints, animationMatrixOverride);
            

            return newModel;
        }

        static RmvLodHeader[] CreateLodHeaders(uint expectedLodCount, List<LodGenerationSettings> lodSettingsPerLod, RmvVersionEnum version)
        {
            var factory = LodHeaderFactory.Create();
            var output = new RmvLodHeader[expectedLodCount];
            
            for (var i = 0; i < expectedLodCount; i++)
                output[i] = factory.CreateEmpty(version, lodSettingsPerLod[i].CameraDistance, (uint)i, lodSettingsPerLod[i].QualityLvl);
            

            return output;
        }
    }
}
