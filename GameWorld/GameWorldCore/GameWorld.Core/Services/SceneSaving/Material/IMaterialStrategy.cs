using GameWorld.Core.SceneNodes;

namespace GameWorld.Core.Services.SceneSaving.Material
{
    public enum MaterialStrategy
    {
        None,
        WsModel_Pharaoh,
        WsModel_Warhammer2,
        WsModel_Warhammer3,
    }

    public interface IMaterialStrategy
    {
        public string Name { get; }
        public string Description { get; }
        public MaterialStrategy StrategyId { get; }
        public WsMaterialResult Generate(MainEditableNode mainNode, string outputPath, bool onlyVisibleNodes);
    }
}
