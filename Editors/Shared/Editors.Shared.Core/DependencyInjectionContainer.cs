using AnimationEditor.Common.BaseControl;
using CommonControls.Editors.BoneMapping.View;
using CommonControls.Editors.TextEditor;
using Editors.Shared.Core.Common;
using Editors.Shared.Core.Common.AnimationPlayer;
using Editors.Shared.Core.Common.BaseControl;
using Editors.Shared.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using Shared.Core.DependencyInjection;
using Shared.Core.ToolCreation;
using Shared.Ui.Editors.BoneMapping;
using Shared.Ui.Editors.TextEditor;
using Shared.Ui.Editors.VariantMeshDefinition;

namespace Editors.Shared.Core
{
    public class DependencyInjectionContainer : DependencyContainer
    {
        public override void Register(IServiceCollection serviceCollection)
        {

            serviceCollection.AddTransient<VariantMeshToXmlConverter>();
            serviceCollection.AddTransient<TextEditorViewModel<VariantMeshToXmlConverter>>();

            serviceCollection.AddTransient<TextEditorView>();
            serviceCollection.AddTransient<DefaultTextConverter>();
            serviceCollection.AddTransient<TextEditorViewModel<DefaultTextConverter>>();


            serviceCollection.AddScoped<BoneMappingView>();
            serviceCollection.AddScoped<BoneMappingViewModel>();


            serviceCollection.AddScoped<SceneObjectEditor>();
            serviceCollection.AddTransient<SceneObject>();
            serviceCollection.AddScoped<AnimationPlayerViewModel>();
            serviceCollection.AddScoped<SceneObjectViewModelBuilder>();
            serviceCollection.AddScoped<EditorHostView>();

            serviceCollection.AddScoped<IEditorHostParameters, EditorHostParameters>();


            serviceCollection.AddSingleton<SkeletonAnimationLookUpHelper>();;
        }

        public override void RegisterTools(IEditorDatabase factory)
        {
            EditorInfoBuilder
                .Create<TextEditorViewModel<VariantMeshToXmlConverter>, TextEditorView>(EditorEnums.XML_VariantMesh_Editor)
                .AddExtension(".variantmeshdefinition", EditorPriorites.High)
                .Build(factory);

            EditorInfoBuilder
                .Create<TextEditorViewModel<DefaultTextConverter>, TextEditorView>(EditorEnums.XML_Editor)
                .AddExtension(".json", EditorPriorites.Default)
                .AddExtension(".xml", EditorPriorites.Default)
                .AddExtension(".txt", EditorPriorites.Default)
                .AddExtension(".wsmodel", EditorPriorites.Default)
                .AddExtension(".xml.material", EditorPriorites.Default)
                .AddExtension(".anm.meta.xml", EditorPriorites.Default)
                .AddExtension(".bmd.xml", EditorPriorites.Default)
                .AddExtension(".csv", EditorPriorites.Default)
                .AddExtension(".bnk.xml", EditorPriorites.Default)
                .AddExtension(".aproj", EditorPriorites.Default)
                .Build(factory);
        }
    }
}
