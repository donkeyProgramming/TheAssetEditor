using CommonControls.Services;
using View3D.SceneNodes;
using View3D.Services.SceneSaving.WsModel;

namespace View3D.Services.SceneSaving.Material.Strategies
{
    public class Warhammer3WsModelStrategy : IMaterialStrategy
    {
        private readonly WsModelGeneratorService _wsModelGeneratorService;
        public string Name => "Warhammer3";
        public string Description => "";
        public bool IsAvailable => true;

        public MaterialStrategy StrategyId => MaterialStrategy.WsModel_Warhammer3;

        public Warhammer3WsModelStrategy(WsModelGeneratorService wsModelGeneratorService)
        {
            _wsModelGeneratorService = wsModelGeneratorService;
        }

        public void Generate(MainEditableNode mainNode, string outputPath, bool onlyVisibleNodes)
        {
            _wsModelGeneratorService.GenerateWsModel(outputPath, mainNode, GameTypeEnum.Warhammer3);
        }
    }
}
