using GameWorld.Core.SceneNodes;
using Shared.GameFormats.RigidModel;

namespace GameWorld.Core.Services.SceneSaving.Geometry.Strategies
{
    public class Rmw8Strategy : IGeometryStrategy
    {
        private readonly NodeToRmvSaveHelper _sceneSaverService;

        public GeometryStrategy StrategyId => GeometryStrategy.Rmv8;
        public string Name => "Rmv8";
        public string Description => "";
        public bool IsAvailable => true;

        public Rmw8Strategy(NodeToRmvSaveHelper sceneSaverService)
        {
            _sceneSaverService = sceneSaverService;
        }

        public RmvFile? Generate(MainEditableNode mainNode, GeometrySaveSettings saveSettings)
        {
            var res = _sceneSaverService.Save(saveSettings.OutputName, mainNode, mainNode.SkeletonNode.Skeleton, RmvVersionEnum.RMV2_V8, saveSettings);
            return res;
        }
    }
}
