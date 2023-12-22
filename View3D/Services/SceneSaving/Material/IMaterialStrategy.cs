using View3D.SceneNodes;
using View3D.Services.SceneSaving.Lod;

namespace View3D.Services.SceneSaving.WsModel
{
    public enum MaterialStrategy
    {
        None,
        WsModel_Warhammer2,
        WsModel_Warhammer3
    }

    public interface IMaterialStrategy
    {
        public string Name { get; }
        public string Description { get; }
        public bool IsAvailable { get; }
        public MaterialStrategy StrategyId { get; }
        public void Generate(MainEditableNode mainNode, string outputPath, bool onlyVisibleNodes);
    }
}
