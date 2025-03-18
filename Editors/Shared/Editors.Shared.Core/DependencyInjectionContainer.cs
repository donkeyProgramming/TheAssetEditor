using AnimationEditor.Common.BaseControl;
using CommonControls.Editors.BoneMapping.View;
using CommonControls.Editors.TextEditor;
using Editors.Shared.Core.Common;
using Editors.Shared.Core.Common.AnimationPlayer;
using Editors.Shared.Core.Common.BaseControl;
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
            serviceCollection.AddScoped<AnimationPlayerViewModel>();
            serviceCollection.AddScoped<SceneObjectViewModelBuilder>();
            serviceCollection.AddScoped<EditorHostView>();

            serviceCollection.AddScoped<IEditorHostParameters, EditorHostParameters>();

        }

        public override void RegisterTools(IEditorDatabase factory)
        {
            EditorInfoBuilder
                .Create<TextEditorViewModel<VariantMeshToXmlConverter>, TextEditorView>(EditorEnums.XML_VariantMesh_Editor)
                .AddExtention(".variantmeshdefinition", EditorPriorites.High)
                .Build(factory);


            EditorInfoBuilder
                .Create<TextEditorViewModel<DefaultTextConverter>, TextEditorView>(EditorEnums.XML_Editor)
                .AddExtention(".json", EditorPriorites.Default)
                .AddExtention(".xml", EditorPriorites.Default)
                .AddExtention(".txt", EditorPriorites.Default)
                .AddExtention(".wsmodel", EditorPriorites.Default)
                .AddExtention(".xml.material", EditorPriorites.Default)
                .AddExtention(".anm.meta.xml", EditorPriorites.Default)
                .AddExtention(".bmd.xml", EditorPriorites.Default)
                .AddExtention(".csv", EditorPriorites.Default)
                .AddExtention(".bnk.xml", EditorPriorites.Default)
                .AddExtention(".aproj", EditorPriorites.Default)
                .AddExtention(".twui.xml", EditorPriorites.Default)
                .Build(factory);
        }
    }
}
