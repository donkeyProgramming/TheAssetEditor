using KitbasherEditor.EventHandlers;
using KitbasherEditor.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Shared.Core.DependencyInjection;
using System;
using View3D.Components;

namespace KitbasherEditor
{
    public class KitbasherScopeHelper : IScopeHelper<KitbasherViewModel>
    {
        public void ResolveGlobalServices(IServiceProvider serviceProvider)
        {
            // Force the services to initialize. This is needed to make them subscribe to events
            serviceProvider.GetRequiredService<SceneInitializedHandler>();
            serviceProvider.GetRequiredService<SkeletonChangedHandler>();

            var inserter = serviceProvider.GetRequiredService<IComponentInserter>();
            inserter.Execute();
        }
    }
}
