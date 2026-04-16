using GameWorld.Core.SceneNodes;
using Shared.GameFormats.RigidModel;

namespace GameWorld.Core.Services.SceneSaving.Geometry.Strategies
{
    public class Rmw7Strategy : IGeometryStrategy
    {
        private readonly NodeToRmvSaveHelper _sceneSaverService;

        public GeometryStrategy StrategyId => GeometryStrategy.Rmv7;
        public string Name => "Rmv7";
        public string Description => "";
        public bool IsAvailable => true;

        public Rmw7Strategy(NodeToRmvSaveHelper sceneSaverService)
        {
            _sceneSaverService = sceneSaverService;
        }

        public RmvFile? Generate(MainEditableNode mainNode, GeometrySaveSettings saveSettings)
        {
            var res = _sceneSaverService.Save(saveSettings.OutputName, mainNode, mainNode.SkeletonNode.Skeleton, RmvVersionEnum.RMV2_V7, saveSettings);
            return res;
        }
    }
}
