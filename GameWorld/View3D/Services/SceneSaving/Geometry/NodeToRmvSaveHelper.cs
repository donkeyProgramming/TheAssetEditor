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

        public void Save(string outputPath, Rmv2ModelNode mainNode, GameSkeleton? skeleton, RmvVersionEnum rmvVersionEnum, GeometrySaveSettings saveSettings)
        {
            try
            {
                var bytes = GenerateBytes(mainNode, rmvVersionEnum, skeleton, saveSettings, true);
                _packFileSaveService.Save(outputPath, bytes, false);
            }
            catch (Exception e)
            {
                _logger.Here().Error("Error saving model - " + e.ToString());
                MessageBox.Show($"Saving failed!\n{e.Message}" );
            }
        }

        byte[] GenerateBytes(Rmv2ModelNode modelNode, RmvVersionEnum version, GameSkeleton? skeleton, GeometrySaveSettings saveSettings, bool enrichModel = true)
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
                    rmvFile.ModelList[lodIndex][meshIndex] = CreateRmvModel(modelname, meshes[meshIndex].PivotPoint, meshes[meshIndex].Material, meshes[meshIndex].Geometry, skeleton, enrichModel);
                }
            }

            // Update data in the header and reCalc offset
            rmvFile.RecalculateOffsets();
            var outputBytes = ModelFactory.Create().Save(rmvFile);
            _logger.Here().Information($"Model generated correctly");

            return outputBytes;
        }

        RmvModel CreateRmvModel(string modelName, Vector3 pivotPoint, CapabilityMaterial capabilityMaterial, MeshObject geometry, GameSkeleton? skeleton, bool addBonesAsAttachmentPoints)
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

            if (addBonesAsAttachmentPoints && skeleton != null)
            {
                var boneNames = skeleton.BoneNames.Select(x => x.Replace("bn_", "")).ToArray();
                newModel.Material.EnrichDataBeforeSaving(boneNames);
            }

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
