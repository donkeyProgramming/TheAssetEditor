using Microsoft.Extensions.DependencyInjection;
using Monogame.WpfInterop.ResourceHandling;
using Shared.Core.DependencyInjection;

namespace GameWorld.WpfWindow
{
    public class DependencyInjectionContainer : DependencyContainer
    {
        public override void Register(IServiceCollection serviceCollection)
        {
            // Graphics scene
            serviceCollection.AddScoped<WpfGame>();
            serviceCollection.AddSingleton<ResourceLibrary>();
        }
    }
}
