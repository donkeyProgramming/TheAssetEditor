using GameWorld.Core.SceneNodes;
using GameWorld.Core.Services.SceneSaving.Material;
using Shared.Core.Services;

namespace GameWorld.Core.Services.SceneSaving.Material.Strategies
{
    public class Warhammer2WsModelStrategy : IMaterialStrategy
    {
        private readonly WsModelGeneratorService _wsModelGeneratorService;

        public MaterialStrategy StrategyId => MaterialStrategy.WsModel_Warhammer2;
        public string Name => "Warhammer2";
        public string Description => "Can also be used for Troy";
        public bool IsAvailable => true;

        public Warhammer2WsModelStrategy(WsModelGeneratorService wsModelGeneratorService)
        {
            _wsModelGeneratorService = wsModelGeneratorService;
        }

        public void Generate(MainEditableNode mainNode, string outputPath, bool onlyVisibleNodes)
        {
            _wsModelGeneratorService.GenerateWsModel(outputPath, mainNode, GameTypeEnum.Warhammer2);
        }
    }
}

