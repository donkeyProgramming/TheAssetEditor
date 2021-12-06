using CommonControls.Common;
using CommonControls.Editors.AnimMeta.View;
using Microsoft.Extensions.DependencyInjection;

namespace CommonControls.Editors.AnimMeta
{
    public class AnimMetaEditor_DependencyInjectionContainer
    {
        public static void Register(IServiceCollection serviceCollection)
        {
            serviceCollection.AddTransient<MainEditorView>();
            serviceCollection.AddTransient<EditorViewModel>();
        }

        public static void RegisterTools(IToolFactory factory)
        {
            factory.RegisterTool<EditorViewModel, MainEditorView>(new ExtentionToTool(".anm.meta", ".meta"));
        }
    }
}
