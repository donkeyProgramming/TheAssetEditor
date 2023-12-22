using View3D.SceneNodes;

namespace View3D.Services.SceneSaving.Lod
{
    public enum LodStrategy
    {
        Default,
        Simplygon,
        Lod0ForAll,
    }

    public interface ILodGenerationStrategy
    {
        public string Name { get; }
        public string Description { get; }
        public bool IsAvailable { get; }
        public LodStrategy StrategyId { get; }

        void Generate(MainEditableNode mainNode, LodGenerationSettings[] settings);
    }
}
