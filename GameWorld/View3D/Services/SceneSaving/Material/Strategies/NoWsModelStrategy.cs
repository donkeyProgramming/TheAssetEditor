using GameWorld.Core.SceneNodes;
using GameWorld.Core.Services.SceneSaving.Material;

namespace GameWorld.Core.Services.SceneSaving.Material.Strategies
{
    public class NoWsModelStrategy : IMaterialStrategy
    {
        public MaterialStrategy StrategyId => MaterialStrategy.WsModel_Warhammer2;
        public string Name => "None";
        public string Description => "Dont generate a ws model";
        public bool IsAvailable => true;

        public NoWsModelStrategy()
        {

        }

        public void Generate(MainEditableNode mainNode, string outputPath, bool onlyVisibleNodes)
        {

        }
    }
}

