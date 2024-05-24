using Microsoft.Extensions.DependencyInjection;
using Shared.Core.DependencyInjection;
using Shared.Core.Misc;

namespace Shared.EmbeddedResources
{
    public class DependencyInjectionContainer : DependencyContainer
    {
        private readonly bool _loadResource;

        public DependencyInjectionContainer(bool loadResource)
        {
            _loadResource = loadResource;
        }

        public override void Register(IServiceCollection services)
        {
            if (_loadResource)
            {
                IconLibrary.Load();
                DirectoryHelper.EnsureCreated();
            }
        }
    }
}
