
using Microsoft.Extensions.DependencyInjection;
using Shared.Core.DependencyInjection;
using Shared.Core.ToolCreation;

namespace Editors.Reports
{
    public class DependencyInjectionContainer : DependencyContainer
    {
        public DependencyInjectionContainer(bool loadResource = true)
        {
        }

        public override void Register(IServiceCollection services)
        {
       
        }

        public override void RegisterTools(IToolFactory factory)
        {
     
        }
    }
}
