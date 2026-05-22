using Microsoft.Extensions.DependencyInjection;
using Shared.Core.DependencyInjection;
using Shared.GameFormats.AnimationMeta.Parsing;

namespace Shared.GameFormats
{
    public class DependencyInjectionContainer : DependencyContainer
    {
        public DependencyInjectionContainer()
        {
        }

        public override void Register(IServiceCollection services)
        {
            services.AddSingleton<IMetaDataDatabase, MetaDataDatabase>();
            services.AddTransient<MetaDataFileParser>();
        }
    }
}
