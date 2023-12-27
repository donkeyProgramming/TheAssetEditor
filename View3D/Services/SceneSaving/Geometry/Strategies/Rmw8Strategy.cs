using View3D.SceneNodes;

namespace View3D.Services.SceneSaving.Geometry.Strategies
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

        public void Generate(MainEditableNode mainNode, string outputPath, bool onlyVisibleNodes)
        {
            _sceneSaverService.Save(outputPath, mainNode, CommonControls.FileTypes.RigidModel.RmvVersionEnum.RMV2_V8, onlyVisibleNodes);
        }
    }
}
