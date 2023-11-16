using CommonControls.Services.ToolCreation;
using Microsoft.Extensions.DependencyInjection;

namespace CommonControls.Editors.AnimationBatchExporter
{
    public class AnimationBatchExporter_DependencyInjectionContainer
    {
        public static void Register(IServiceCollection serviceCollection)
        {
            serviceCollection.AddTransient<OpenAnimationBatchConverterCommand>();
            serviceCollection.AddTransient<AnimationBatchExportViewModel>();
            serviceCollection.AddTransient<AnimationBatchExportView>();
        }

        public static void RegisterTools(IToolFactory factory)
        {
        }
    }
}
