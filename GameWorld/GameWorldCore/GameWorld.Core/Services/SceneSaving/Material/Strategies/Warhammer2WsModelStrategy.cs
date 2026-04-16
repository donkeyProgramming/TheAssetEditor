using GameWorld.Core.Rendering.Materials.Serialization;
using GameWorld.Core.SceneNodes;
using Shared.Core.Settings;

namespace GameWorld.Core.Services.SceneSaving.Material.Strategies
{
    public class Warhammer2WsModelStrategy : IMaterialStrategy
    {
        private readonly WsModelGeneratorService _wsModelGeneratorService;
        private readonly MaterialToWsMaterialFactory _wsMaterialGeneratorFactory;

        public MaterialStrategy StrategyId => MaterialStrategy.WsModel_Warhammer2;
        public string Name => "Warhammer2";
        public string Description => "Can also be used for Troy";

        public Warhammer2WsModelStrategy(WsModelGeneratorService wsModelGeneratorService, MaterialToWsMaterialFactory wsMaterialGeneratorFactory)
        {
            _wsModelGeneratorService = wsModelGeneratorService;
            _wsMaterialGeneratorFactory = wsMaterialGeneratorFactory;
        }

        public WsMaterialResult Generate(MainEditableNode mainNode, string outputPath, bool onlyVisibleNodes)
        {
            var input = WsModelGeneratorInputHelper.Create(mainNode, onlyVisibleNodes);
            var res = _wsModelGeneratorService.GenerateWsModel(_wsMaterialGeneratorFactory.CreateInstance(GameTypeEnum.Warhammer2), outputPath, input);
            return res;
        }
    }
}

