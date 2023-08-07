using AnimationEditor.AnimationKeyframeEditor;
using AnimationEditor.AnimationTransferTool;
using AnimationEditor.CampaignAnimationCreator;
using AnimationEditor.Common.AnimationPlayer;
using AnimationEditor.Common.BaseControl;
using AnimationEditor.Common.ReferenceModel;
using AnimationEditor.MountAnimationCreator;
using AnimationEditor.PropCreator.ViewModels;
using AnimationEditor.SkeletonEditor;
using AnimationEditor.SuperView;
using CommonControls;
using CommonControls.Services.ToolCreation;
using Microsoft.Extensions.DependencyInjection;

namespace AnimationEditor
{
    public class AnimationEditors_DependencyInjectionContainer : DependencyContainer
    {
        public override void Register(IServiceCollection serviceCollection)
        {
            serviceCollection.AddScoped<SceneObjectBuilder>();
            serviceCollection.AddTransient<SceneObject>();
            serviceCollection.AddScoped<AnimationPlayerViewModel>();
            serviceCollection.AddScoped<SceneObjectViewModelBuilder>();
            serviceCollection.AddScoped<EditorHostView>();

            serviceCollection.AddScoped<EditorHost<SuperViewViewModel>>();
            serviceCollection.AddScoped<SuperViewViewModel>();

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
            factory.RegisterTool<EditorHost<SuperViewViewModel>, EditorHostView>();
            factory.RegisterTool<EditorHost<SkeletonEditorViewModel>, EditorHostView>();
            factory.RegisterTool<EditorHost<CampaignAnimationCreatorViewModel>, EditorHostView>();
            factory.RegisterTool<EditorHost<AnimationKeyframeEditorViewModel>, EditorHostView>();
        }
    }
}
