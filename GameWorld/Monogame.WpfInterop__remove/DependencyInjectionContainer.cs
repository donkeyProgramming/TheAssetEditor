using GameWorld.WpfWindow.ResourceHandling;
using Microsoft.Extensions.DependencyInjection;
using Shared.Core.DependencyInjection;

namespace GameWorld.WpfWindow
{
    public class DependencyInjectionContainer : DependencyContainer
    {
        public override void Register(IServiceCollection serviceCollection)
        {
            // Graphics scene
            serviceCollection.AddScoped<IWpfGame, WpfGame>();
            serviceCollection.AddSingleton<ResourceLibrary>();
        }
    }
}
