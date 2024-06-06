using Editors.Shared.DevConfig.Base;
using Microsoft.Extensions.DependencyInjection;
using Shared.Core.DependencyInjection;

namespace Editors.Shared.DevConfig
{
    public class DependencyInjectionContainer : DependencyContainer
    {
        public DependencyInjectionContainer()
        {
        }

        public override void Register(IServiceCollection serviceCollection)
        {
            serviceCollection.AddTransient<DevelopmentConfigurationManager>();
            RegisterAllAsInterface<IDeveloperConfiguration>(serviceCollection, ServiceLifetime.Transient);
        }
    }
}
