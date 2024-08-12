using System.Collections.Generic;
using System.Linq;
using System.Windows;
using GameWorld.Core.SceneNodes;
using GameWorld.Core.Services.SceneSaving.Geometry;
using GameWorld.Core.Services.SceneSaving.Lod;
using GameWorld.Core.Services.SceneSaving.Material;
using Shared.Core.Events.Scoped;
using Shared.Core.Events;
using Shared.Core.PackFiles;
using Shared.GameFormats.RigidModel.MaterialHeaders;
using GameWorld.Core.Rendering.Materials.Capabilities;
using GameWorld.Core.Rendering.Materials.Shaders;
using GameWorld.Core.Rendering.Materials;
using GameWorld.Core.Rendering.Materials.Serialization;

namespace GameWorld.Core.Services.SceneSaving
{
    public class SaveService
    {
        private readonly PackFileService _packFileService;
        private readonly EventHub _eventHub;
        private readonly GeometryStrategyProvider _geometryStrategyProvider;
        private readonly LodStrategyProvider _lodStrategyProvider;
        private readonly MaterialStrategyProvider _materialStrategyProvider;

        public SaveService(PackFileService packFileService, EventHub eventHub,
            GeometryStrategyProvider geometryStrategyProvider,
            LodStrategyProvider lodStrategyProvider,
            MaterialStrategyProvider materialStrategyProvider)
        {
            _packFileService = packFileService;
            _eventHub = eventHub;
            _geometryStrategyProvider = geometryStrategyProvider;
            _lodStrategyProvider = lodStrategyProvider;
            _materialStrategyProvider = materialStrategyProvider;
        }


        public void Save(MainEditableNode mainNode, GeometrySaveSettings settings)
        {
            if (_packFileService.GetEditablePack() == null)
            {
                MessageBox.Show("No editable pack selected", "error");
                return;
            }

            var outputPath = settings.OutputName;

            // Update lod values
            var model = mainNode.Model;
            for (var i = 0; i < model.LodHeaders.Count(); i++)
            {
                model.LodHeaders[i].LodCameraDistance = settings.LodSettingsPerLod[i].CameraDistance;
                model.LodHeaders[i].QualityLvl = settings.LodSettingsPerLod[i].QualityLvl;
            }

            UpdateRmv2MaterialFromShader(mainNode);
            _lodStrategyProvider.GetStrategy(settings.LodGenerationMethod).Generate(mainNode, settings.LodSettingsPerLod);
            _geometryStrategyProvider.GetStrategy(settings.GeometryOutputType).Generate(mainNode, settings);
            _materialStrategyProvider.GetStrategy(settings.MaterialOutputType).Generate(mainNode, outputPath, settings.OnlySaveVisible);

            _eventHub.Publish(new ScopedFileSavedEvent() { NewPath = outputPath });
        }

        void UpdateRmv2MaterialFromShader(Rmv2ModelNode mainNode)
        {
            foreach (var mesh in mainNode.GetMeshesInLod(0, false))
            {
                var material = mesh.Effect;


                var rmvMaterial = mesh.Material;

                var t = new MaterialToRmvSerializer();
                mesh.Material = t.CreateMaterialFromCapabilityMaterial(mesh.Material, material);   //This is the place!

                //rmvMaterial.SetTexture(Shared.GameFormats.RigidModel.Types.TextureType.BaseColour, material.TryGetCapability<DefaultCapability>().BaseColour.TexturePath);
            }
        }


        public List<GeometryStrategyInformation> GetGeometryStrategies() => _geometryStrategyProvider.GetStrategies();
        public List<MaterialStrategyInformation> GetMaterialStrategies() => _materialStrategyProvider.GetStrategies();
        public List<LodStrategyInformation> GetLodStrategies() => _lodStrategyProvider.GetStrategies();
    }
}
