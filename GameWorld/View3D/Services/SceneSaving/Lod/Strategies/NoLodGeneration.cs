using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GameWorld.Core.SceneNodes;

namespace GameWorld.Core.Services.SceneSaving.Lod.Strategies
{

    public class NoLodGeneration : ILodGenerationStrategy
    {
        public LodStrategy StrategyId => LodStrategy.Lod0ForAll;
        public string Name => "No Lod generation";
        public string Description => "Leave lods as is - useful when only changing textures";
        public bool IsAvailable => true;

        public NoLodGeneration()
        {
        }

        public void Generate(MainEditableNode mainNode, List<LodGenerationSettings> settings)
        {
          
        }
    }
}
