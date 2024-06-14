using GameWorld.Core.SceneNodes;

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
        public void Generate(MainEditableNode mainNode, string outputPath, bool onlyVisibleNodes);
    }
}
