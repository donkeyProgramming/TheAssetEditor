using System.Linq;
using GameWorld.Core.Rendering.Shading.Shaders;
using GameWorld.Core.SceneNodes;
using Shared.Core.Services;
using Shared.GameFormats.RigidModel;

namespace GameWorld.Core.Services.SceneSaving.Material.Strategies
{
    internal class PharaohWsModelStrategy : IMaterialStrategy
    {
        private readonly WsModelGeneratorService _wsModelGeneratorService;

        public MaterialStrategy StrategyId => MaterialStrategy.WsModel_Pharaoh;
        public string Name => "Pharaoh";
        public string Description => "Generates the WsModel for Pharaoh Total War";
        public bool IsAvailable => true;

        public PharaohWsModelStrategy(WsModelGeneratorService wsModelGeneratorService)
        {
            _wsModelGeneratorService = wsModelGeneratorService;
        }

        public void Generate(MainEditableNode mainNode, string outputPath, bool onlyVisibleNodes)
        {
            var input = WsModelGeneratorInputHelper.Create(mainNode);
            _wsModelGeneratorService.GenerateWsModel(outputPath, input, CapabilityMaterialFactory.GetBuilder(GameTypeEnum.Pharaoh));
        }
    }


   
}
