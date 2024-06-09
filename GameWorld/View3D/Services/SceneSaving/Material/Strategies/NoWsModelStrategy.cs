using View3D.SceneNodes;
using View3D.Services.SceneSaving.WsModel;

namespace View3D.Services.SceneSaving.Material.Strategies
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

