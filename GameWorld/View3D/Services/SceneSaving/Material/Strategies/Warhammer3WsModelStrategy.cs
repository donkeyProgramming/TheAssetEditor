﻿using GameWorld.Core.Rendering.Materials;
using GameWorld.Core.Rendering.Materials.Serialization;
using GameWorld.Core.SceneNodes;
using Shared.Core.Services;

namespace GameWorld.Core.Services.SceneSaving.Material.Strategies
{
    public class Warhammer3WsModelStrategy : IMaterialStrategy
    {
        private readonly WsModelGeneratorService _wsModelGeneratorService;
        private readonly MaterialToWsMaterialFactory _wsMaterialGeneratorFactory;

        public string Name => "Warhammer3";
        public string Description => "";

        public MaterialStrategy StrategyId => MaterialStrategy.WsModel_Warhammer3;

        public Warhammer3WsModelStrategy(WsModelGeneratorService wsModelGeneratorService, MaterialToWsMaterialFactory wsMaterialGeneratorFactory)
        {
            _wsModelGeneratorService = wsModelGeneratorService;
            _wsMaterialGeneratorFactory = wsMaterialGeneratorFactory;
        }

        public void Generate(MainEditableNode mainNode, string outputPath, bool onlyVisibleNodes)
        {
            var input = WsModelGeneratorInputHelper.Create(mainNode);
            _wsModelGeneratorService.GenerateWsModel(_wsMaterialGeneratorFactory.CreateInstance(GameTypeEnum.Warhammer3), outputPath, input);
        }
    }
}
