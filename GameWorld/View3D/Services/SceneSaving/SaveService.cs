using System.Collections.Generic;
using System.Linq;
using System.Windows;
using GameWorld.Core.SceneNodes;
using GameWorld.Core.Services.SceneSaving.Geometry;
using GameWorld.Core.Services.SceneSaving.Lod;
using GameWorld.Core.Services.SceneSaving.Material;
using Shared.Core.PackFiles;

namespace GameWorld.Core.Services.SceneSaving
{
    public class SaveService
    {
        private readonly PackFileService _packFileService;
        private readonly GeometryStrategyProvider _geometryStrategyProvider;
        private readonly LodStrategyProvider _lodStrategyProvider;
        private readonly MaterialStrategyProvider _materialStrategyProvider;

        public SaveService(PackFileService packFileService,
            GeometryStrategyProvider geometryStrategyProvider,
            LodStrategyProvider lodStrategyProvider,
            MaterialStrategyProvider materialStrategyProvider)
        {
            _packFileService = packFileService;
            _geometryStrategyProvider = geometryStrategyProvider;
            _lodStrategyProvider = lodStrategyProvider;
            _materialStrategyProvider = materialStrategyProvider;
        }

        public List<GeometryStrategyInformation> GetGeometryStrategies() => _geometryStrategyProvider.GetStrategies();
        public List<MaterialStrategyInformation> GetMaterialStrategies() => _materialStrategyProvider.GetStrategies();
        public List<LodStrategyInformation> GetLodStrategies() => _lodStrategyProvider.GetStrategies();

        public void Save(MainEditableNode mainNode, SaveSettings settings)
        {
            if (_packFileService.GetEditablePack() == null)
            {
                MessageBox.Show("No editable pack selected", "error");
                return;
            }

            var outputPath = settings.OutputName;
            var onlyVisibleNodes = settings.OnlySaveVisible;

            // Update lod values
            var model = mainNode.Model;
            for (var i = 0; i < model.LodHeaders.Count(); i++)
            {
                model.LodHeaders[i].LodCameraDistance = settings.LodSettingsPerLod[i].CameraDistance;
                model.LodHeaders[i].QualityLvl = settings.LodSettingsPerLod[i].QualityLvl;
            }

            _lodStrategyProvider.GetStrategy(settings.LodGenerationMethod).Generate(mainNode, settings.LodSettingsPerLod);
            _materialStrategyProvider.GetStrategy(settings.MaterialOutputType).Generate(mainNode, outputPath, onlyVisibleNodes);
            _geometryStrategyProvider.GetStrategy(settings.GeometryOutputType).Generate(mainNode, outputPath, onlyVisibleNodes);
        }
    }
}
