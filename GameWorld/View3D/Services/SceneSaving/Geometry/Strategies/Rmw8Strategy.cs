using GameWorld.Core.SceneNodes;
using Shared.GameFormats.RigidModel;

namespace GameWorld.Core.Services.SceneSaving.Geometry.Strategies
{
    public class Rmw8Strategy : IGeometryStrategy
    {
        private readonly SceneSaverService _sceneSaverService;

        public GeometryStrategy StrategyId => GeometryStrategy.Rmv8;
        public string Name => "Rmv8";
        public string Description => "";
        public bool IsAvailable => true;

        public Rmw8Strategy(SceneSaverService sceneSaverService)
        {
            _sceneSaverService = sceneSaverService;
        }

        public void Generate(MainEditableNode mainNode, GeometrySaveSettings saveSettings)
        {
            _sceneSaverService.Save(saveSettings.OutputName, mainNode, RmvVersionEnum.RMV2_V8, saveSettings);
        }
    }
}
