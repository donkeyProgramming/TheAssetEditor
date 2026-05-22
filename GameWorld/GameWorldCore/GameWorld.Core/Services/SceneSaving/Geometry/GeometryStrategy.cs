using GameWorld.Core.SceneNodes;
using Shared.GameFormats.RigidModel;

namespace GameWorld.Core.Services.SceneSaving.Geometry
{
    public enum GeometryStrategy
    {
        None,
        Rmv6,
        Rmv7,
        Rmv8,
    }

    public interface IGeometryStrategy
    {
        public string Name { get; }
        public string Description { get; }
        public bool IsAvailable { get; }
        public GeometryStrategy StrategyId { get; }
        public RmvFile? Generate(MainEditableNode mainNode, GeometrySaveSettings saveSettings);
    }
}
