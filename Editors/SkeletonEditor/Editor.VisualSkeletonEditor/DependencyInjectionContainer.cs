using AnimationEditor.Common.BaseControl;
using Editor.VisualSkeletonEditor.SkeletonEditor;
using Microsoft.Extensions.DependencyInjection;
using Shared.Core.DependencyInjection;
using Shared.Core.DevConfig;
using Shared.Core.ToolCreation;

namespace Editor.VisualSkeletonEditor
{
    public class DependencyInjectionContainer : DependencyContainer
    {
        public override void Register(IServiceCollection serviceCollection)
        {
            serviceCollection.AddScoped<SkeletonEditorViewModel>();

            RegisterAllAsInterface<IDeveloperConfiguration>(serviceCollection, ServiceLifetime.Transient);
        }

        public override void RegisterTools(IEditorDatabase editorDatabase)
        {
            EditorInfoBuilder
                .Create<SkeletonEditorViewModel, EditorHostView>(EditorEnums.VisualSkeletonEditor)
                .AddExtention(".anim", EditorPriorites.High)
                .ValidForFoldersContaining(@"animations\skeletons")
                .ValidForFoldersContaining("tech")
                .AddToToolbar("Skeleton Tool", true)
                .Build(editorDatabase);
        }
    }
}
