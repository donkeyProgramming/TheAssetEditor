using AnimationEditor.Common.BaseControl;
using Editors.AnimationMeta.Presentation;
using Editors.AnimationMeta.Presentation.View;
using Editors.AnimationMeta.SuperView;
using Editors.AnimationMeta.Visualisation;
using Editors.Shared.Core.Common.BaseControl;
using Editors.Shared.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using Shared.Core.DependencyInjection;
using Shared.Core.ToolCreation;
using Shared.GameFormats.AnimationMeta.Parsing;

namespace Editors.AnimationMeta
{
    public class DependencyInjectionContainer : DependencyContainer
    {
        public override void Register(IServiceCollection serviceCollection)
        {
            MetaDataTagDeSerializer.EnsureMappingTableCreated();

            serviceCollection.AddTransient<MainEditorView>();
            serviceCollection.AddTransient<EditorViewModel>();

            serviceCollection.AddScoped<EditorHost<SuperViewViewModel>>();
            serviceCollection.AddScoped<SuperViewViewModel>();

            serviceCollection.AddScoped<IMetaDataFactory, MetaDataFactory>(); // Needs heavy refactorying!
        }

        public override void RegisterTools(IEditorDatabase factory)
        {
            factory.Register(EditorInfo.Create<EditorHost<SuperViewViewModel>, EditorHostView> (EditorEnums.SuperView_Editor, new NoExtention()));

            var editorInfo = EditorInfo.Create<EditorViewModel, MainEditorView>(EditorEnums.Meta_Editor, new ExtensionToTool([".anm.meta", ".meta", ".snd.meta"]));
            factory.Register(editorInfo);
        }
    }
}
