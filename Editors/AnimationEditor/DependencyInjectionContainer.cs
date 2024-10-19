using AnimationEditor.AnimationKeyframeEditor;
using AnimationEditor.AnimationTransferTool;
using AnimationEditor.CampaignAnimationCreator;
using AnimationEditor.Common.BaseControl;
using AnimationEditor.MountAnimationCreator;
using AnimationEditor.SkeletonEditor;
using Editors.Shared.Core.Common.BaseControl;
using Microsoft.Extensions.DependencyInjection;
using Shared.Core.DependencyInjection;
using Shared.Core.ToolCreation;

namespace Editors.AnimationVisualEditors
{
    public class DependencyInjectionContainer : DependencyContainer
    {
        public override void Register(IServiceCollection serviceCollection)
        {
            serviceCollection.AddScoped<EditorHost<SkeletonEditorViewModel>>();
            serviceCollection.AddScoped<SkeletonEditorViewModel>();

            serviceCollection.AddScoped<EditorHost<CampaignAnimationCreatorViewModel>>();
            serviceCollection.AddScoped<CampaignAnimationCreatorViewModel>();

            serviceCollection.AddScoped<EditorHost<AnimationTransferToolViewModel>>();
            serviceCollection.AddScoped<AnimationTransferToolViewModel>();

            serviceCollection.AddScoped<EditorHost<MountAnimationCreatorViewModel>>();
            serviceCollection.AddScoped<MountAnimationCreatorViewModel>();

            serviceCollection.AddScoped<EditorHost<AnimationKeyframeEditorViewModel>>();
            serviceCollection.AddScoped<AnimationKeyframeEditorViewModel>();
        }

        public override void RegisterTools(IToolFactory factory)
        {
            factory.Register(EditorInfo.Create<EditorHost<MountAnimationCreatorViewModel>, EditorHostView>(EditorEnums.MountTool_Editor, new NoExtention()));
            factory.Register(EditorInfo.Create<EditorHost<AnimationTransferToolViewModel>, EditorHostView>(EditorEnums.AnimationTransfer_Editor, new NoExtention()));
            factory.Register(EditorInfo.Create<EditorHost<SkeletonEditorViewModel>, EditorHostView>(EditorEnums.Skeleton_Editor, new NoExtention()));
            factory.Register(EditorInfo.Create<EditorHost<CampaignAnimationCreatorViewModel>, EditorHostView>(EditorEnums.CampaginAnimation_Editor, new NoExtention()));
            factory.Register(EditorInfo.Create<EditorHost<AnimationKeyframeEditorViewModel>, EditorHostView>(EditorEnums.AnimationKeyFrame_Editor, new NoExtention()));
        }
    }
}
