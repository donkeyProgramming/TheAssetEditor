using CommonControls.Common;
using Microsoft.Extensions.DependencyInjection;

namespace View3D
{
    public static class View3D_DependencyInjectionContainer
    {
        public static void Register(IServiceCollection serviceCollection)
        {
            //serviceCollection.AddTransient<SceneViewModel>();
            //serviceCollection.AddTransient<SceneView3D>();
        }

        public static void RegisterTools(IToolFactory factory)
        {
            //factory.RegisterTool<SceneViewModel, SceneView3D>();
        }
    }
}
