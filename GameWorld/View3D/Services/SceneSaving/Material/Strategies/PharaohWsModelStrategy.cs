using GameWorld.Core.Rendering.Materials.Serialization;
using GameWorld.Core.SceneNodes;
using Shared.Core.Settings;

namespace GameWorld.Core.Services.SceneSaving.Material.Strategies
{
    internal class PharaohWsModelStrategy : IMaterialStrategy
    {
        private readonly WsModelGeneratorService _wsModelGeneratorService;
        private readonly MaterialToWsMaterialFactory _wsMaterialGeneratorFactory;

        public MaterialStrategy StrategyId => MaterialStrategy.WsModel_Pharaoh;
        public string Name => "Pharaoh";
        public string Description => "Generates the WsModel for Pharaoh Total War";

        public PharaohWsModelStrategy(WsModelGeneratorService wsModelGeneratorService, MaterialToWsMaterialFactory wsMaterialGeneratorFactory)
        {
            _wsModelGeneratorService = wsModelGeneratorService;
            _wsMaterialGeneratorFactory = wsMaterialGeneratorFactory;
        }

        public WsMaterialResult Generate(MainEditableNode mainNode, string outputPath, bool onlyVisibleNodes)
        {
            var input = WsModelGeneratorInputHelper.Create(mainNode, onlyVisibleNodes);
            var res = _wsModelGeneratorService.GenerateWsModel(_wsMaterialGeneratorFactory.CreateInstance(GameTypeEnum.Pharaoh), outputPath, input);
            return res;
        }
    }
}
