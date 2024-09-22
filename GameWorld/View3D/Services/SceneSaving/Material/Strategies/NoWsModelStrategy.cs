﻿using GameWorld.Core.SceneNodes;

namespace GameWorld.Core.Services.SceneSaving.Material.Strategies
{
    public class NoWsModelStrategy : IMaterialStrategy
    {
        public MaterialStrategy StrategyId => MaterialStrategy.None;
        public string Name => "None";
        public string Description => "Don't generate a ws model";

        public NoWsModelStrategy()
        {

        }

        public void Generate(MainEditableNode mainNode, string outputPath, bool onlyVisibleNodes)
        {
        }
    }
}

