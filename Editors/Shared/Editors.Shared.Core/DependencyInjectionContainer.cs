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
                .AddExtention(".variantmeshdefinition", EditorPriorites.High)
                .Build(factory);
        }
    }
}
