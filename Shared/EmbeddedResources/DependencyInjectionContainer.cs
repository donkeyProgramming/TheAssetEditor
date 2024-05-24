using Microsoft.Extensions.DependencyInjection;
using Shared.Core;
using Shared.Core.Misc;
using Shared.Core.ToolCreation;

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

        public override void RegisterTools(IToolFactory factory)
        {

        }
    }
}
