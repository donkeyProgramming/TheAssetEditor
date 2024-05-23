
using Microsoft.Extensions.DependencyInjection;
using SharedCore.Misc;
using SharedCore.ToolCreation;

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
