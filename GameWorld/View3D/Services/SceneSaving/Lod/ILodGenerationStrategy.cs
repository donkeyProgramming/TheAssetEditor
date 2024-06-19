using GameWorld.Core.SceneNodes;
using GameWorld.Core.Services.SceneSaving;

namespace GameWorld.Core.Services.SceneSaving.Lod
{
    public enum LodStrategy
    {
        AssetEditor,
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
