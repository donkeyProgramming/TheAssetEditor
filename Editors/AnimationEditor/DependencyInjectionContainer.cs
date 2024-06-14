using AnimationEditor.AnimationKeyframeEditor;
using AnimationEditor.AnimationTransferTool;
using AnimationEditor.CampaignAnimationCreator;
using AnimationEditor.Common.BaseControl;
using AnimationEditor.MountAnimationCreator;
using AnimationEditor.PropCreator.ViewModels;
using AnimationEditor.SkeletonEditor;
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
            factory.RegisterTool<EditorHost<MountAnimationCreatorViewModel>, EditorHostView>();
            factory.RegisterTool<EditorHost<AnimationTransferToolViewModel>, EditorHostView>();
            factory.RegisterTool<EditorHost<SkeletonEditorViewModel>, EditorHostView>();
            factory.RegisterTool<EditorHost<CampaignAnimationCreatorViewModel>, EditorHostView>();
            factory.RegisterTool<EditorHost<AnimationKeyframeEditorViewModel>, EditorHostView>();
        }
    }
}
