using GameWorld.Core.SceneNodes;
using Shared.GameFormats.RigidModel;

namespace GameWorld.Core.Services.SceneSaving.Geometry.Strategies
{
    public class Rmw7Strategy : IGeometryStrategy
    {
        private readonly SceneSaverService _sceneSaverService;

        public GeometryStrategy StrategyId => GeometryStrategy.Rmv7;
        public string Name => "Rmv7";
        public string Description => "";
        public bool IsAvailable => true;

        public Rmw7Strategy(SceneSaverService sceneSaverService)
        {
            _sceneSaverService = sceneSaverService;
        }

        public void Generate(MainEditableNode mainNode, string outputPath, bool onlyVisibleNodes)
        {
            _sceneSaverService.Save(outputPath, mainNode, RmvVersionEnum.RMV2_V7, onlyVisibleNodes);
        }
    }
}
