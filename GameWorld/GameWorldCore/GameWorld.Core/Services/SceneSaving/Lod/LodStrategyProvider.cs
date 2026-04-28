using System.Collections.Generic;
using System.Linq;

namespace GameWorld.Core.Services.SceneSaving.Lod
{
    public class LodStrategyProvider
    {
        private readonly IEnumerable<ILodGenerationStrategy> _lodGenerationStrategies;

        public LodStrategyProvider(IEnumerable<ILodGenerationStrategy> lodGenerationStrategies)
        {
            _lodGenerationStrategies = lodGenerationStrategies;
        }

        public List<LodStrategyInformation> GetStrategies()
        {
            var output = _lodGenerationStrategies
                .Select(x => new LodStrategyInformation(x.StrategyId, x.Description, x.Name, x.IsAvailable))
                .ToList();

            return output;
        }

        public ILodGenerationStrategy GetStrategy(LodStrategy strategy)
        {
            return _lodGenerationStrategies.First(x => x.StrategyId == strategy);
        }
    }

    public record LodStrategyInformation(LodStrategy StrategyId, string Description, string Name, bool IsAvailable);
}
