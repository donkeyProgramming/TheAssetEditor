using BaseTests.ToolCreation;
using CommonControls;
using CommonControls.Common;
using CommonControls.Services.ToolCreation;
using Microsoft.Extensions.DependencyInjection;

namespace TestCommon.Utility
{
    class Test_DependencyContainer : DependencyContainer
    {
        public override void Register(IServiceCollection serviceCollection)
        {
            serviceCollection.AddScoped<TestEditor>();
            serviceCollection.AddScoped<DummyView>();
        }

        public override void RegisterTools(IToolFactory factory)
        {
            factory.RegisterTool<TestEditor, DummyView>(new ExtensionToTool(EditorEnums.None, new[] { ".testformat" }));
        }
    }

}
