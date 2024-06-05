using Microsoft.Extensions.DependencyInjection;
using Monogame.WpfInterop.ResourceHandling;
using MonoGame.Framework.WpfInterop;
using Shared.Core.DependencyInjection;

namespace Monogame.WpfInterop
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
