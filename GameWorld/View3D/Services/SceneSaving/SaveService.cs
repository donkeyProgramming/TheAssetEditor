using System.Collections.Generic;
using System.Windows;
using GameWorld.Core.SceneNodes;
using GameWorld.Core.Services.SceneSaving.Geometry;
using GameWorld.Core.Services.SceneSaving.Lod;
using GameWorld.Core.Services.SceneSaving.Material;
using Shared.Core.Events;
using Shared.Core.Events.Scoped;
using Shared.Core.PackFiles;

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
            for (var i = 0; i < model.LodHeaders.Length; i++)
            {
                model.LodHeaders[i].LodCameraDistance = settings.LodSettingsPerLod[i].CameraDistance;
                model.LodHeaders[i].QualityLvl = settings.LodSettingsPerLod[i].QualityLvl;
            }

            _lodStrategyProvider.GetStrategy(settings.LodGenerationMethod).Generate(mainNode, settings.LodSettingsPerLod);
            _geometryStrategyProvider.GetStrategy(settings.GeometryOutputType).Generate(mainNode, settings);
            _materialStrategyProvider.GetStrategy(settings.MaterialOutputType).Generate(mainNode, outputPath, settings.OnlySaveVisible);

            _eventHub.Publish(new ScopedFileSavedEvent() { NewPath = outputPath });
        }

        public List<GeometryStrategyInformation> GetGeometryStrategies() => _geometryStrategyProvider.GetStrategies();
        public List<MaterialStrategyInformation> GetMaterialStrategies() => _materialStrategyProvider.GetStrategies();
        public List<LodStrategyInformation> GetLodStrategies() => _lodStrategyProvider.GetStrategies();
    }
}
