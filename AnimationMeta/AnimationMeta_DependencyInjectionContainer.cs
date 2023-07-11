using AnimationMeta.Presentation;
using AnimationMeta.Presentation.View;
using CommonControls.Common;
using Microsoft.Extensions.DependencyInjection;

namespace AnimationMeta
{
    public class AnimationMeta_DependencyInjectionContainer
    {
        public static void Register(IServiceCollection serviceCollection)
        {
            serviceCollection.AddTransient<MainEditorView>();
            serviceCollection.AddTransient<EditorViewModel>();
        }

        public static void RegisterTools(IToolFactory factory)
        {
            factory.RegisterTool<EditorViewModel, MainEditorView>(new ExtentionToTool(EditorEnums.Meta_Editor, new[] { ".anm.meta", ".meta", ".snd.meta" }));
        }
    }
}
