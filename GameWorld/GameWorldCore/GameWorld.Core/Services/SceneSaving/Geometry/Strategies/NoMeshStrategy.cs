using GameWorld.Core.SceneNodes;
using GameWorld.Core.Services.SceneSaving.Geometry;
using Shared.GameFormats.RigidModel;

namespace GameWorld.Core.Services.SceneSaving.Geometry.Strategies
{
    public class NoMeshStrategy : IGeometryStrategy
    {
        public GeometryStrategy StrategyId => GeometryStrategy.None;
        public string Name => "None";
        public string Description => "Dont generate a mesh";
        public bool IsAvailable => true;

        public NoMeshStrategy()
        {
        }

        public RmvFile? Generate(MainEditableNode mainNode, GeometrySaveSettings saveSettings)
        {
            return null;
        }
    }
}
