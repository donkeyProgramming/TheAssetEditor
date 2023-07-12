using AnimationMeta.FileTypes.Parsing;
using AnimationMeta.Presentation;
using AnimationMeta.Presentation.View;
using AnimationMeta.Visualisation;
using CommonControls;
using CommonControls.Common;
using CommonControls.Services.ToolCreation;
using Microsoft.Extensions.DependencyInjection;

namespace AnimationMeta
{
    public class AnimationMeta_DependencyInjectionContainer : DependencyContainer
    {
        public override void Register(IServiceCollection serviceCollection)
        {
            MetaDataTagDeSerializer.EnsureMappingTableCreated();

            serviceCollection.AddTransient<MainEditorView>();
            serviceCollection.AddTransient<EditorViewModel>();

            serviceCollection.AddScoped<MetaDataFactory>(); // Needs heavy refactorying!
        }

        public override void RegisterTools(IToolFactory factory)
        {
            factory.RegisterTool<EditorViewModel, MainEditorView>(new ExtensionToTool(EditorEnums.Meta_Editor, new[] { ".anm.meta", ".meta", ".snd.meta" }));
        }
    }
}
