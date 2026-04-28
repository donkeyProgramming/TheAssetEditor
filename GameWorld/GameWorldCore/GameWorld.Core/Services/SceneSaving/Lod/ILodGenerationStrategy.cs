using System.Collections.Generic;
using GameWorld.Core.SceneNodes;

namespace GameWorld.Core.Services.SceneSaving.Lod
{
    public enum LodStrategy
    {
        AssetEditor,
        Lod0ForAll,
        NoLodGeneration,
    }

    public interface ILodGenerationStrategy
    {
        public string Name { get; }
        public string Description { get; }
        public bool IsAvailable { get; }
        public LodStrategy StrategyId { get; }

        void Generate(MainEditableNode mainNode, List<LodGenerationSettings> settings);
    }
}
