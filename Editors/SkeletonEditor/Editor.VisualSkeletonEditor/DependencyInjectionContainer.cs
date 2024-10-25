using AnimationEditor.Common.BaseControl;
using Editor.VisualSkeletonEditor.SkeletonEditor;
using Microsoft.Extensions.DependencyInjection;
using Shared.Core.DependencyInjection;
using Shared.Core.ToolCreation;

namespace Editor.VisualSkeletonEditor
{
    public class DependencyInjectionContainer : DependencyContainer
    {
        public override void Register(IServiceCollection serviceCollection)
        {
            serviceCollection.AddScoped<SkeletonEditorViewModel>();
        }

        public override void RegisterTools(IEditorDatabase factory)
        {
            factory.Register(EditorInfo.Create<SkeletonEditorViewModel, EditorHostView>(EditorEnums.Skeleton_Editor, new ExtensionToTool([".anim"])));
        }
    }
}
