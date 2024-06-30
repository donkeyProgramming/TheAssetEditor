using GameWorld.Core.Components;
using KitbasherEditor.EventHandlers;
using KitbasherEditor.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Shared.Core.DependencyInjection;

namespace KitbasherEditor
{
    public class KitbasherScopeHelper : IScopeHelper<KitbasherViewModel>
    {
        public void ResolveGlobalServices(IServiceProvider serviceProvider)
        {
            // Force the services to initialize. This is needed to make them subscribe to events
            serviceProvider.GetRequiredService<SkeletonChangedHandler>();

            var inserter = serviceProvider.GetRequiredService<IComponentInserter>();
            inserter.Execute();
        }
    }
}
