using System.Collections.Generic;
using System.Linq;

namespace GameWorld.Core.Services.SceneSaving.Material
{
    public class MaterialStrategyProvider
    {
        private readonly IEnumerable<IMaterialStrategy> _materialStrategies;

        public MaterialStrategyProvider(IEnumerable<IMaterialStrategy> materialStrategies)
        {
            _materialStrategies = materialStrategies;
        }

        public List<MaterialStrategyInformation> GetStrategies()
        {
            var output = _materialStrategies
                .Select(x => new MaterialStrategyInformation(x.StrategyId, x.Description, x.Name))
                .ToList();

            return output;
        }

        public IMaterialStrategy GetStrategy(MaterialStrategy strategy)
        {
            return _materialStrategies.First(x => x.StrategyId == strategy);
        }
    }

    public record MaterialStrategyInformation(MaterialStrategy StrategyId, string Description, string Name);
}
