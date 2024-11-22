using Editors.Reports.DeepSearch;
using Microsoft.Extensions.DependencyInjection;
using Shared.Core.DependencyInjection;
using Shared.Core.Events;

namespace Editors.Reports
{
    public class DependencyInjectionContainer : DependencyContainer
    {
        public DependencyInjectionContainer()
        {
        }

        public override void Register(IServiceCollection serviceCollection)
        {
            serviceCollection.AddTransient<DeepSearchReport>();
            serviceCollection.AddTransient<DeepSearchCommand>();
        }
    }
}
