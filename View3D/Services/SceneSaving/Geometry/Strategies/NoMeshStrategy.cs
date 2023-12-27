using View3D.SceneNodes;

namespace View3D.Services.SceneSaving.Geometry.Strategies
{
    public class NoMeshStrategy : IGeometryStrategy
    {
        public GeometryStrategy StrategyId => GeometryStrategy.Rmv8;
        public string Name => "None";
        public string Description => "Dont generate a mesh";
        public bool IsAvailable => true;

        public NoMeshStrategy()
        {
        }

        public void Generate(MainEditableNode mainNode, string outputPath, bool onlyVisibleNodes)
        {
        }
    }
}
