using CommonControls.Common;
using KitbasherEditor.EventHandlers;
using KitbasherEditor.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System;
using View3D.Components;

namespace KitbasherEditor
{
    public class KitbasherScopeHelper : IScopeHelper<KitbasherViewModel>
    {
        public void ResolveGlobalServices(IServiceProvider serviceProvider)
        {
            serviceProvider.GetRequiredService<SceneInitializedHandler>();
            serviceProvider.GetRequiredService<SkeletonChangedHandler>();

            var inserter = serviceProvider.GetRequiredService<IComponentInserter>();
            inserter.Execute();
        }
    }
}
