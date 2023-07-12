using AnimationMeta.Presentation;
using AnimationMeta.Presentation.View;
using AnimationMeta.Visualisation;
using CommonControls.Common;
using Microsoft.Extensions.DependencyInjection;
using View3D;

namespace AnimationMeta
{
    public class AnimationMeta_DependencyInjectionContainer : DependencyContainer
    {
        public override void Register(IServiceCollection serviceCollection)
        {
            serviceCollection.AddTransient<MainEditorView>();
            serviceCollection.AddTransient<EditorViewModel>();


            serviceCollection.AddScoped<MetaDataFactory>(); // Needs heavy refactorying!
        }

        public override void RegisterTools(IToolFactory factory)
        {
            factory.RegisterTool<EditorViewModel, MainEditorView>(new ExtentionToTool(EditorEnums.Meta_Editor, new[] { ".anm.meta", ".meta", ".snd.meta" }));
        }
    }
}
