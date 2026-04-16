using System.Collections.Generic;
using System.Linq;

namespace GameWorld.Core.Services.SceneSaving.Geometry
{
    public class GeometryStrategyProvider
    {
        private readonly IEnumerable<IGeometryStrategy> _strategies;

        public GeometryStrategyProvider(IEnumerable<IGeometryStrategy> strategies)
        {
            _strategies = strategies;
        }

        public List<GeometryStrategyInformation> GetStrategies()
        {
            var output = _strategies
                .Select(x => new GeometryStrategyInformation(x.StrategyId, x.Description, x.Name, x.IsAvailable))
                .ToList();

            return output;
        }

        public IGeometryStrategy GetStrategy(GeometryStrategy strategy)
        {
            return _strategies.First(x => x.StrategyId == strategy);
        }
    }

    public record GeometryStrategyInformation(GeometryStrategy StrategyId, string Description, string Name, bool IsAvailable);
}
